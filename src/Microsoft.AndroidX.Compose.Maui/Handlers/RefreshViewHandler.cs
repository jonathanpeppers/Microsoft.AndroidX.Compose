using AndroidX.Compose;
using AndroidX.Compose.Runtime;
using AndroidX.Compose.UI.Platform;
using Microsoft.AndroidX.Compose.Maui.Platform;
using Microsoft.Maui.Handlers;
using ComposeColor = AndroidX.Compose.Color;
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
/// glyph color via the public
/// <see cref="PullToRefreshIndicator"/> facade, which wraps
/// <c>PullToRefreshDefaults.Instance.Indicator(...)</c>. When unset
/// the spinner falls back to Material 3's default tint.</para>
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

        bool isEnabled    = _isEnabled.Value;
        bool isRefreshing = _isRefreshing.Value;

        // Share one PullToRefreshState wrapper between the box and
        // the optional Indicator override. The box's Render populates
        // state.Jvm during the first composition; the indicator
        // lambda runs inside that same Render afterwards, so it sees
        // the populated handle.
        var state = new PullToRefreshState();

        var box = new ComposePullToRefreshBox(
            isRefreshing: isRefreshing,
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
            },
            state: state);

        // RefreshColor → spinner glyph tint. null/non-SolidPaint
        // falls through to Material 3's default; non-null swaps in
        // the public PullToRefreshIndicator facade.
        if (_refreshColor.Value is { } packedColor)
        {
            box.Indicator = new PullToRefreshIndicator(state, isRefreshing)
            {
                Color = ComposeColor.FromPacked(packedColor),
            };
        }

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
    /// brush) falls back to Material 3's default theme tint; a
    /// non-null solid color swaps in a
    /// <see cref="PullToRefreshIndicator"/> with the typed color.
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
}
