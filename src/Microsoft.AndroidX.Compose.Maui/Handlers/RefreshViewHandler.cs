using AndroidX.Compose;
using AndroidX.Compose.Material3.PullToRefresh;
using AndroidX.Compose.Runtime;
using AndroidX.Compose.UI.Platform;
using Kotlin.Jvm.Functions;
using Microsoft.AndroidX.Compose.Maui.Platform;
using Microsoft.Maui.Handlers;
using ComposePullToRefreshBox = AndroidX.Compose.PullToRefreshBox;
using MauiRefreshView         = Microsoft.Maui.Controls.RefreshView;

namespace Microsoft.AndroidX.Compose.Maui.Handlers;

/// <summary>
/// MAUI <see cref="MauiRefreshView"/> handler that renders through
/// Jetpack Compose's Material 3 <see cref="ComposePullToRefreshBox"/>.
/// Replaces MAUI's stock <c>SwipeRefreshLayout</c>-backed handler when
/// the consumer calls
/// <see cref="Hosting.AppHostBuilderExtensions.UseAndroidXCompose"/>.
/// </summary>
/// <remarks>
/// <para><see cref="IRefreshView.IsRefreshing"/> is two-way:
/// MAUI → Compose flows through <see cref="MapIsRefreshing"/>, and the
/// pull gesture flows back through the <c>onRefresh</c> callback —
/// which writes <see cref="IRefreshView.IsRefreshing"/> = <c>true</c>
/// and lets MAUI's <c>OnIsRefreshingPropertyChanged</c> pipeline raise
/// the <c>Refreshing</c> event and invoke
/// <see cref="IRefreshView.Command"/>. The mapper re-fires with the
/// same value, but the <see cref="MutableState{T}"/> equality
/// short-circuit breaks the loop without needing a
/// <c>_suppressMauiWrite</c> flag (mirrors
/// <see cref="EntryHandler.OnValueChanged"/>).</para>
///
/// <para>The consumer is responsible for clearing
/// <see cref="IRefreshView.IsRefreshing"/> back to <c>false</c> when
/// their async reload completes — same contract as MAUI's stock
/// handler. Wiring an automatic timer here would race the consumer's
/// own state machine.</para>
///
/// <para><see cref="IRefreshView.Content"/> is walked through the same
/// <see cref="ComposeWalker"/> recursion used by
/// <see cref="LayoutHandler"/> / <see cref="ContentViewHandler"/>, so
/// nested Compose-backed views fold into the parent composition.</para>
///
/// <para><see cref="IRefreshView.RefreshColor"/> maps to the spinner
/// glyph color via <see cref="PullToRefreshDefaults"/>'s
/// <c>Indicator(state, isRefreshing, modifier, containerColor, color,
/// maxDistance, ...)</c> helper. When unset the spinner falls back to
/// Material 3's default tint and we render the plain
/// <see cref="ComposePullToRefreshBox"/> facade; when set we render
/// directly through the bound <c>PullToRefreshKt.PullToRefreshBox</c>
/// with a custom indicator <c>IFunction3</c> slot that supplies the
/// packed color.</para>
/// </remarks>
public partial class RefreshViewHandler : ComposeElementHandler<IRefreshView>
{
    /// <summary>
    /// Property mapper that forwards MAUI <see cref="IRefreshView"/>
    /// property changes to the Compose-backed <see cref="ComposeView"/>.
    /// </summary>
    public static IPropertyMapper<IRefreshView, RefreshViewHandler> Mapper =
        new PropertyMapper<IRefreshView, RefreshViewHandler>(ViewHandler.ViewMapper)
        {
            [nameof(IRefreshView.IsRefreshing)]      = MapIsRefreshing,
            [nameof(IRefreshView.IsRefreshEnabled)]  = MapIsRefreshEnabled,
            [nameof(IRefreshView.RefreshColor)]      = MapRefreshColor,
            ["Content"]                              = MapContent,
        };

    /// <summary>Command mapper (inherits view-level commands; no extras).</summary>
    public static CommandMapper<IRefreshView, RefreshViewHandler> CommandMapper =
        new(ViewCommandMapper);

    readonly MutableState<bool>  _isRefreshing   = new(false);
    readonly MutableState<bool>  _isEnabled      = new(true);
    readonly MutableState<long?> _refreshColor   = new((long?)null);
    // Bumped whenever Content swaps so BuildNode reads the live
    // PresentedContent reference (IView itself doesn't fit in
    // MutableState<T>; same trick as ContentViewHandler).
    readonly MutableState<int>   _contentVersion = new(0);

    /// <summary>Construct a handler with the default mappers.</summary>
    public RefreshViewHandler() : base(Mapper, CommandMapper) { }

    /// <summary>Construct a handler with custom mappers.</summary>
    public RefreshViewHandler(IPropertyMapper? mapper, CommandMapper? commandMapper = null)
        : base(mapper ?? Mapper, commandMapper ?? CommandMapper) { }

    /// <inheritdoc/>
    public override ComposableNode BuildNode(IComposer composer)
    {
        // Read the version slot so recompositions trigger when Content
        // swaps (Content as a property doesn't live in MutableState<T>).
        _ = _contentVersion.Value;

        var view    = VirtualView
            ?? throw new InvalidOperationException("VirtualView not set on RefreshViewHandler.");
        var context = MauiContext
            ?? throw new InvalidOperationException("MauiContext not set on RefreshViewHandler.");

        bool isEnabled = _isEnabled.Value;

        // When RefreshColor is set we drop into the
        // PullToRefreshBox-with-custom-indicator path so the spinner
        // glyph picks up the caller-supplied tint. The default path
        // keeps using the existing facade, which lets Material 3's
        // theme drive the indicator colors.
        if (_refreshColor.Value is long packedColor)
        {
            return new ColoredIndicatorBox(
                this, view, context, isEnabled, packedColor);
        }

        var box = new ComposePullToRefreshBox(
            isRefreshing: _isRefreshing.Value,
            onRefresh:    () =>
            {
                // Mirror MAUI's stock Android RefreshView handler: only
                // write IsRefreshing = true and let MAUI's
                // OnIsRefreshingPropertyChanged pipeline raise the
                // Refreshing event AND invoke Command.Execute /
                // RefreshCommand.Execute. Doing it ourselves here would
                // double-fire the Command for any Command-bound caller.
                // The mapper re-fires with the same value and the
                // MutableState<bool> equality short-circuit breaks the
                // loop.
                //
                // When IsRefreshEnabled = false we swallow the pull-down
                // gesture entirely — stock MAUI sets SwipeRefreshLayout.Enabled
                // = false which suppresses both the spinner and the
                // event, matching that behaviour here without needing
                // the lower-level enabled-flag API on PullToRefreshBox.
                if (!isEnabled) return;
                _isRefreshing.Value = true;
                view.IsRefreshing   = true;
            });

        // FillMaxSize so the gesture region matches MAUI's full-bleed
        // RefreshView semantics, plus ApplyViewProperties for Opacity /
        // Translation / Scale / Rotation / IsVisible / Clip / Shadow.
        box.PrependModifier(Modifier.FillMaxSize().ApplyViewProperties(view).ApplySemantics(view));

        if (view.Content is { } content)
            box.Add(c => ComposeWalker.Render(content, c, context));

        return box;
    }

    /// <summary>
    /// Map <see cref="IRefreshView.IsRefreshing"/> to the Compose busy
    /// slot. The <see cref="MutableState{T}"/> equality short-circuit
    /// breaks the two-way feedback loop when the pull gesture writes
    /// the same value back through <see cref="IRefreshView"/>.
    /// </summary>
    public static void MapIsRefreshing(RefreshViewHandler handler, IRefreshView view) =>
        handler._isRefreshing.Value = view.IsRefreshing;

    /// <summary>Bump the content version slot so <see cref="BuildNode"/> re-walks.</summary>
    public static void MapContent(RefreshViewHandler handler, IRefreshView _) =>
        handler._contentVersion.Value++;

    /// <summary>
    /// Map <see cref="IRefreshView.IsRefreshEnabled"/>. When
    /// <see langword="false"/> the pull-to-refresh gesture is swallowed
    /// — the spinner never appears and <c>IsRefreshing = true</c> is
    /// not written. Matches MAUI's stock Android behaviour of disabling
    /// the underlying <c>SwipeRefreshLayout</c>.
    /// </summary>
    public static void MapIsRefreshEnabled(RefreshViewHandler handler, IRefreshView view) =>
        handler._isEnabled.Value = view.IsRefreshEnabled;

    /// <summary>
    /// Map <see cref="IRefreshView.RefreshColor"/> to the spinner
    /// glyph color. <c>null</c> (or a non-<see cref="SolidPaint"/>
    /// brush) falls back to Material 3's default theme tint and the
    /// handler renders the plain <see cref="ComposePullToRefreshBox"/>
    /// facade; a non-null solid color switches us to the
    /// custom-indicator render path.
    /// </summary>
    /// <remarks>
    /// <see cref="IRefreshView.RefreshColor"/> is a
    /// <see cref="Paint"/> on the interface so consumers can in theory
    /// bind a gradient brush — but MAUI's stock platform handlers all
    /// flatten that to a single tint, so we do the same. Gradient
    /// paints are silently treated as "no color set".
    /// </remarks>
    public static void MapRefreshColor(RefreshViewHandler handler, IRefreshView view) =>
        handler._refreshColor.Value = ColorMapping.ToPackedLong((view.RefreshColor as SolidPaint)?.Color);

    /// <summary>
    /// Hand-written <see cref="ComposableNode"/> that drives the
    /// <c>PullToRefreshKt.PullToRefreshBox</c> binding directly so the
    /// indicator slot can pipe a caller-supplied
    /// <see cref="IRefreshView.RefreshColor"/> through to
    /// <see cref="PullToRefreshDefaults.Instance"/>'s
    /// <c>Indicator(...)</c> helper. Lives here (not as a generated
    /// facade) because no current <c>[ComposeFacade]</c> shape models
    /// "rebuild PullToRefreshBox with a custom indicator slot whose
    /// only deviation from Material 3's default is the spinner color".
    /// </summary>
    sealed class ColoredIndicatorBox : ComposableNode
    {
        // PullToRefreshBox $default mask bits — order mirrors
        // PullToRefreshBoxDefault in ComposeDefaults.cs.
        const int BitContentAlignment = 1 << 4;

        // PullToRefreshDefaults.Indicator $default mask bits — order
        // matches the bound JNI signature
        // (state, isRefreshing, modifier, containerColor, color,
        // maxDistance). Bits 0/1 are always supplied; bit 4 is
        // cleared when we pass our own color.
        const int IndicatorBitModifier       = 1 << 2;
        const int IndicatorBitContainerColor = 1 << 3;
        const int IndicatorBitMaxDistance    = 1 << 5;

        readonly RefreshViewHandler _owner;
        readonly IRefreshView       _view;
        readonly IMauiContext       _context;
        readonly bool               _isEnabled;
        readonly long               _packedColor;

        public ColoredIndicatorBox(
            RefreshViewHandler owner,
            IRefreshView       view,
            IMauiContext       context,
            bool               isEnabled,
            long               packedColor)
        {
            _owner       = owner;
            _view        = view;
            _context     = context;
            _isEnabled   = isEnabled;
            _packedColor = packedColor;
        }

        public override void Render(IComposer composer)
        {
            // Mirror RefreshViewHandler.BuildNode's IsRefreshEnabled
            // gesture-swallow guard (see comments there).
            var onRefresh = new ComposableLambda0(() =>
            {
                if (!_isEnabled) return;
                _owner._isRefreshing.Value = true;
                _view.IsRefreshing         = true;
            });

            // Remember the JVM PullToRefreshState. RememberPullToRefreshState
            // is itself a @Composable that internally uses `remember`,
            // so calling it directly during Render returns the same
            // instance across recompositions (Kotlin slot-table identity).
            // No outer `composer.Remember(...)` wrapper needed.
            var state = PullToRefreshKt.RememberPullToRefreshState(composer, 0);

            bool isRefreshing = _owner._isRefreshing.Value;

            // Custom indicator slot — calls the bound
            // PullToRefreshDefaults.Indicator(...) with our packed
            // color.
            IFunction3 indicator = ComposableLambdas.Wrap3(
                composer,
                c =>
                {
                    int indicatorMask =
                        IndicatorBitModifier |
                        IndicatorBitContainerColor |
                        IndicatorBitMaxDistance;
                    PullToRefreshDefaults.Instance.Indicator(
                        state:           state,
                        isRefreshing:    isRefreshing,
                        modifier:        null,
                        containerColor:  0L,
                        color:           _packedColor,
                        maxDistance:     0f,
                        _composer:       c,
                        p7:              indicatorMask,
                        _changed:        0);
                });

            // Content lambda — same walk as the default path.
            IFunction3 content = ComposableLambdas.Wrap3(
                composer,
                c =>
                {
                    if (_view.Content is { } child)
                        ComposeWalker.Render(child, c, _context);
                });

            var modifier = Modifier.FillMaxSize()
                .ApplyViewProperties(_view)
                .ApplySemantics(_view);

            // PullToRefreshBox $default mask. Bits we always pass —
            // isRefreshing(0), onRefresh(1), modifier(2), state(3),
            // indicator(5), content(6) — stay cleared. Bit 4
            // (contentAlignment) is set so Kotlin uses its default
            // (Alignment.TopCenter for the indicator placement).
            int boxMask = BitContentAlignment;

            PullToRefreshKt.PullToRefreshBox(
                isRefreshing:     isRefreshing,
                onRefresh:        onRefresh,
                modifier:         modifier.Build(),
                state:            state,
                contentAlignment: null,
                indicator:        indicator,
                content:          content,
                _composer:        composer,
                p8:               boxMask,
                _changed:         0);
        }
    }
}
