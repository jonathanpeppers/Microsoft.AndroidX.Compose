using AndroidX.Compose;
using AndroidX.Compose.Runtime;
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
/// </remarks>
public partial class ScrollViewHandler : ComposeElementHandler<IScrollView>
{
    /// <summary>
    /// Property mapper. <see cref="IScrollView.Orientation"/> picks
    /// the axis; nothing else needs eager mapping because the inner
    /// content is reached via the walker.
    /// </summary>
    public static IPropertyMapper<IScrollView, ScrollViewHandler> Mapper =
        new PropertyMapper<IScrollView, ScrollViewHandler>(ViewHandler.ViewMapper)
        {
            [nameof(IScrollView.Orientation)] = MapOrientation,
        };

    /// <summary>Command mapper (inherits view-level commands; no extras).</summary>
    public static CommandMapper<IScrollView, ScrollViewHandler> CommandMapper =
        new(ViewCommandMapper);

    readonly MutableState<int> _orientation = new((int)ScrollOrientation.Vertical);

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

        return new ScrollContainer(scroll, _orientation, context);
    }

    /// <summary>Map <see cref="IScrollView.Orientation"/> to the cached enum slot.</summary>
    public static void MapOrientation(ScrollViewHandler handler, IScrollView view) =>
        handler._orientation.Value = (int)view.Orientation;

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
        readonly IScrollView _scroll;
        readonly MutableState<int> _orientation;
        readonly IMauiContext _context;

        public ScrollContainer(IScrollView scroll, MutableState<int> orientation, IMauiContext context)
        {
            _scroll = scroll;
            _orientation = orientation;
            _context = context;
        }

        public override void Render(IComposer composer)
        {
            var orientation = (ScrollOrientation)_orientation.Value;
            var state = composer.Remember(() => new ScrollState());

            var fillMain = orientation switch
            {
                ScrollOrientation.Horizontal => Modifier.HorizontalScroll(state),
                _                            => Modifier.VerticalScroll(state),
            };

            // BuildModifier() is internal to Microsoft.AndroidX.Compose;
            // from this assembly we read the public Modifier property
            // (consumers don't call PrependModifier on internal nodes).
            var modifier = Modifier is null ? fillMain : Modifier.Then(fillMain);

            var box = new Box { Modifier = modifier };
            var content = _scroll.PresentedContent;
            if (content is not null)
                box.Add(ComposeWalker.Render(content, composer, _context));
            box.Render(composer);
        }
    }
}
