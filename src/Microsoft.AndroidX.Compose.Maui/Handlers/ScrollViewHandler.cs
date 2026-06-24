using AndroidX.Compose;
using AndroidX.Compose.Runtime;
using Microsoft.AndroidX.Compose.Maui.Platform;
using Microsoft.Maui.Handlers;

namespace Microsoft.AndroidX.Compose.Maui.Handlers;

/// <summary>
/// MAUI <see cref="Microsoft.Maui.Controls.ScrollView"/> handler that
/// renders through a Compose <see cref="Box"/> wrapped in
/// <c>Modifier.verticalScroll</c> or <c>Modifier.horizontalScroll</c>.
/// Folds into the page's single <c>ComposeView</c> via
/// <see cref="IComposeHandler"/>; replaces MAUI's stock
/// <c>MauiScrollView</c>-based handler when
/// <see cref="Hosting.AppHostBuilderExtensions.UseAndroidXCompose"/>
/// is called.
/// </summary>
/// <remarks>
/// <para>Scroll position is <em>not</em> currently exposed via
/// <see cref="MutableState{T}"/> back to MAUI — that lands when
/// <c>IScrollView.ScrollToRequested</c> is wired up in a follow-up
/// (the bridge type, <see cref="ScrollState"/>, already supports
/// <c>ScrollToAsync</c> / <c>AnimateScrollToAsync</c>).</para>
///
/// <para>The
/// <see cref="IScrollView.HorizontalScrollBarVisibility"/> /
/// <see cref="IScrollView.VerticalScrollBarVisibility"/> mappers
/// route to a hand-drawn overlay thumb because Compose Foundation 1.11
/// ships no public <c>Scrollbar</c> / <c>Modifier.scrollbar</c> API on
/// Android (the multiplatform <c>androidx.compose.foundation.v2</c>
/// extension is desktop-only). See
/// <see cref="ScrollbarOverlayDrawCallback"/> for the limitations of
/// that overlay (no auto-hide, thumb-only, fixed neutral tint) — in
/// particular, <see cref="Microsoft.Maui.ScrollBarVisibility.Default"/>
/// behaves the same as
/// <see cref="Microsoft.Maui.ScrollBarVisibility.Always"/>; only
/// <see cref="Microsoft.Maui.ScrollBarVisibility.Never"/> suppresses
/// the overlay.</para>
/// </remarks>
public partial class ScrollViewHandler : ComposeElementHandler<IScrollView>
{
    /// <summary>
    /// Property mapper. <see cref="IScrollView.Orientation"/> picks
    /// the axis; the two visibility properties wire to the overlay
    /// scrollbar (see remarks on the type for why we hand-draw).
    /// Inner content is reached via the walker, so it doesn't need
    /// an eager mapper.
    /// </summary>
    public static IPropertyMapper<IScrollView, ScrollViewHandler> Mapper =
        new PropertyMapper<IScrollView, ScrollViewHandler>(ViewHandler.ViewMapper)
        {
            [nameof(IScrollView.Orientation)]                  = MapOrientation,
            [nameof(IScrollView.Content)]                      = MapContent,
            [nameof(IScrollView.HorizontalScrollBarVisibility)] = MapHorizontalScrollBarVisibility,
            [nameof(IScrollView.VerticalScrollBarVisibility)]   = MapVerticalScrollBarVisibility,
        };

    /// <summary>Command mapper (inherits view-level commands; no extras).</summary>
    public static CommandMapper<IScrollView, ScrollViewHandler> CommandMapper =
        new(ViewCommandMapper);

    readonly MutableState<int> _orientation = new((int)ScrollOrientation.Vertical);
    // Content swaps don't trigger a property changed on Orientation —
    // bumping a version slot recomposes BuildNode which re-walks the
    // new PresentedContent.
    readonly MutableState<int> _contentVersion = new(0);
    // Visibility is stored as int because MutableState<T> only supports
    // a fixed set of T (primitives / Java.Lang.Object / string). Cast
    // back to ScrollBarVisibility on read.
    readonly MutableState<int> _horizontalScrollBarVisibility = new((int)ScrollBarVisibility.Default);
    readonly MutableState<int> _verticalScrollBarVisibility   = new((int)ScrollBarVisibility.Default);

    // Allocated once per handler instance so the JNI peer (and the
    // backing Paint) survives every recomposition. Render reconfigures
    // State / Vertical / Density before each draw — see
    // BorderHandler._strokeDrawCallback for the canonical pattern.
    readonly ScrollbarOverlayDrawCallback _scrollbarCallback = new();

    /// <summary>Construct a handler with the default mappers.</summary>
    public ScrollViewHandler() : base(Mapper, CommandMapper) { }

    /// <summary>Construct a handler with custom mappers.</summary>
    public ScrollViewHandler(IPropertyMapper? mapper, CommandMapper? commandMapper = null)
        : base(mapper ?? Mapper, commandMapper ?? CommandMapper) { }

    /// <inheritdoc/>
    public override ComposableNode BuildNode(IComposer composer)
    {
        var scroll = VirtualView
            ?? throw new InvalidOperationException("VirtualView not set on ScrollViewHandler.");
        var context = MauiContext
            ?? throw new InvalidOperationException("MauiContext not set on ScrollViewHandler.");

        return new ScrollContainer(
            this,
            scroll,
            _orientation,
            _contentVersion,
            _horizontalScrollBarVisibility,
            _verticalScrollBarVisibility,
            _scrollbarCallback,
            context);
    }

    /// <summary>Map <see cref="IScrollView.Orientation"/> to the cached enum slot.</summary>
    public static void MapOrientation(ScrollViewHandler handler, IScrollView view) =>
        handler._orientation.Value = (int)view.Orientation;

    /// <summary>
    /// Bump the content version slot when
    /// <see cref="IScrollView.Content"/> swaps so the child walker
    /// re-renders against the new tree. The new
    /// <see cref="IScrollView.PresentedContent"/> is read live inside
    /// <c>ScrollContainer.Render</c>.
    /// </summary>
    public static void MapContent(ScrollViewHandler handler, IScrollView _) =>
        handler._contentVersion.Value++;

    /// <summary>
    /// Map <see cref="IScrollView.HorizontalScrollBarVisibility"/> to
    /// the cached enum slot. <see cref="ScrollBarVisibility.Default"/>
    /// and <see cref="ScrollBarVisibility.Always"/> both render the
    /// overlay (no auto-hide animation in our pinned Compose
    /// Foundation); only <see cref="ScrollBarVisibility.Never"/>
    /// suppresses it.
    /// </summary>
    public static void MapHorizontalScrollBarVisibility(ScrollViewHandler handler, IScrollView view) =>
        handler._horizontalScrollBarVisibility.Value = (int)view.HorizontalScrollBarVisibility;

    /// <summary>
    /// Map <see cref="IScrollView.VerticalScrollBarVisibility"/> to the
    /// cached enum slot. See
    /// <see cref="MapHorizontalScrollBarVisibility"/> for visibility
    /// semantics.
    /// </summary>
    public static void MapVerticalScrollBarVisibility(ScrollViewHandler handler, IScrollView view) =>
        handler._verticalScrollBarVisibility.Value = (int)view.VerticalScrollBarVisibility;

    /// <summary>
    /// <see cref="ComposableNode"/> implementing the
    /// scroll-wrapped <see cref="Box"/>. Pulled out so the
    /// <see cref="ScrollState"/> can be <c>Remember</c>ed inside its
    /// <see cref="Render(IComposer)"/> (which runs on the composition
    /// thread) rather than allocated eagerly during
    /// <see cref="BuildNode(IComposer)"/>.
    /// </summary>
    sealed class ScrollContainer : ComposableNode
    {
        readonly ScrollViewHandler _owner;
        readonly IScrollView _scroll;
        readonly MutableState<int> _orientation;
        readonly MutableState<int> _contentVersion;
        readonly MutableState<int> _hVisibility;
        readonly MutableState<int> _vVisibility;
        readonly ScrollbarOverlayDrawCallback _scrollbarCallback;
        readonly IMauiContext _context;

        public ScrollContainer(
            ScrollViewHandler owner,
            IScrollView scroll,
            MutableState<int> orientation,
            MutableState<int> contentVersion,
            MutableState<int> horizontalScrollBarVisibility,
            MutableState<int> verticalScrollBarVisibility,
            ScrollbarOverlayDrawCallback scrollbarCallback,
            IMauiContext context)
        {
            _owner = owner;
            _scroll = scroll;
            _orientation = orientation;
            _contentVersion = contentVersion;
            _hVisibility = horizontalScrollBarVisibility;
            _vVisibility = verticalScrollBarVisibility;
            _scrollbarCallback = scrollbarCallback;
            _context = context;
        }

        public override void Render(IComposer composer)
        {
            // Subscribe inside Render so the deferred composition
            // scope (the actual scope that builds the modifier chain)
            // observes view-property bumps, not the parent-only scope
            // that called BuildNode.
            _owner.SubscribeToViewProperties();

            var orientation = (ScrollOrientation)_orientation.Value;
            // Read the content version so swapping ScrollView.Content
            // re-runs the walker. Live PresentedContent is read below.
            _ = _contentVersion.Value;
            // Read both visibility slots so a runtime change
            // recomposes the overlay layer toggle.
            bool vertical   = orientation != ScrollOrientation.Horizontal;
            var visibility  = (ScrollBarVisibility)(vertical ? _vVisibility.Value : _hVisibility.Value);
            bool drawScrollbar = visibility != ScrollBarVisibility.Never;

            var state = composer.Remember(() => new ScrollState());

            var scrollMod = vertical
                ? Modifier.VerticalScroll(state)
                : Modifier.HorizontalScroll(state);

            // BuildModifier() is internal to Microsoft.AndroidX.Compose;
            // from this assembly we read the public Modifier property
            // (consumers don't call PrependModifier on internal nodes).
            // Cross-cutting view properties go on the OUTER box so
            // opacity / clip / shadow trace the ScrollView's bounding
            // box, not the inner scrolling surface — matches MAUI's
            // stock semantics where Opacity fades the entire scroll
            // region as one. The scroll modifier moves to an inner Box
            // so the overlay scrollbar can sit as a sibling drawn over
            // the scrolling content.
            var outer = Modifier.Companion
                .ApplyViewProperties(_scroll)
                .ApplyGestures(_scroll, _context)
                .ApplySemantics(_scroll);
            var outerModifier = Modifier is null ? outer : Modifier.Then(outer);

            var outerBox = new Box { Modifier = outerModifier };

            var innerBox = new Box { Modifier = scrollMod };
            var content = _scroll.PresentedContent;
            if (content is not null)
                innerBox.Add(ComposeWalker.Render(content, composer, _context));
            outerBox.Add(innerBox);

            if (drawScrollbar)
            {
                _scrollbarCallback.State    = state;
                _scrollbarCallback.Vertical = vertical;
                var metrics = global::Android.Content.Res.Resources.System?.DisplayMetrics
                    ?? throw new InvalidOperationException("Resources.System.DisplayMetrics not available.");
                _scrollbarCallback.Density = metrics.Density;

                var thickness = new Dp(ScrollbarOverlayDrawCallback.ThicknessDip);
                var overlayMod = vertical
                    ? Modifier.Companion
                        .Align(Alignment.CenterEnd)
                        .FillMaxHeight()
                        .Width(thickness)
                        .DrawBehind(_scrollbarCallback)
                    : Modifier.Companion
                        .Align(Alignment.BottomCenter)
                        .FillMaxWidth()
                        .Height(thickness)
                        .DrawBehind(_scrollbarCallback);

                outerBox.Add(new Box { Modifier = overlayMod });
            }

            outerBox.Render(composer);
        }
    }
}
