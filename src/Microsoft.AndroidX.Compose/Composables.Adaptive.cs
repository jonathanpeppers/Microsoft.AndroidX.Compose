using AndroidX.Compose.Material3.Adaptive;
using AndroidX.Window.Layout;

namespace AndroidX.Compose;

public static partial class Composables
{
    /// <summary>
    /// Renders a fold-aware Material 3 list-detail scaffold whose Back
    /// behavior is owned by an outer navigation host.
    /// </summary>
    [Composable]
    public static void ListDetailPaneScaffold<T>(
        ListDetailPaneScaffoldNavigator<T> navigator,
        [ComposableContent] Action listPane,
        [ComposableContent] Action detailPane,
        Modifier? modifier = null,
        [ComposableContent] Action? extraPane = null)
    {
        ArgumentNullException.ThrowIfNull(navigator);
        ArgumentNullException.ThrowIfNull(listPane);
        ArgumentNullException.ThrowIfNull(detailPane);

        ListDetailPaneScaffold(
            ComposableContext.Current,
            navigator,
            _ => listPane(),
            _ => detailPane(),
            modifier,
            extraPane is null ? null : _ => extraPane());
    }

    /// <summary>
    /// Renders a fold-aware Material 3 list-detail scaffold with built-in
    /// system and predictive Back handling.
    /// </summary>
    [Composable]
    public static void NavigableListDetailPaneScaffold<T>(
        ListDetailPaneScaffoldNavigator<T> navigator,
        [ComposableContent] Action listPane,
        [ComposableContent] Action detailPane,
        Modifier? modifier = null,
        [ComposableContent] Action? extraPane = null,
        PaneBackNavigationBehavior? defaultBackBehavior = null)
    {
        ArgumentNullException.ThrowIfNull(navigator);
        ArgumentNullException.ThrowIfNull(listPane);
        ArgumentNullException.ThrowIfNull(detailPane);

        NavigableListDetailPaneScaffold(
            ComposableContext.Current,
            navigator,
            _ => listPane(),
            _ => detailPane(),
            modifier,
            extraPane is null ? null : _ => extraPane(),
            defaultBackBehavior);
    }

    /// <summary>Reads the adaptive information for the current host window.</summary>
    public static WindowAdaptiveInfo CurrentWindowAdaptiveInfo(
        bool supportLargeAndXLargeWidth = false) =>
        ComposeExtensions.CurrentWindowAdaptiveInfo(
            ComposableContext.Current, supportLargeAndXLargeWidth);

    /// <summary>
    /// Remembers a fold-aware navigator for a Material 3 list-detail scaffold.
    /// </summary>
    public static ListDetailPaneScaffoldNavigator<T>
        RememberListDetailPaneScaffoldNavigator<T>(
            AdaptiveHingePolicy verticalHingePolicy =
                AdaptiveHingePolicy.AvoidSeparating,
            bool twoPanesOnMediumWidth = false,
            bool destinationHistoryAware = true,
            WindowAdaptiveInfo? windowAdaptiveInfo = null) =>
        ComposeExtensions.RememberListDetailPaneScaffoldNavigator<T>(
            ComposableContext.Current,
            verticalHingePolicy,
            twoPanesOnMediumWidth,
            destinationHistoryAware,
            windowAdaptiveInfo);

    /// <summary>
    /// Collects the current activity's raw Jetpack WindowManager layout info.
    /// </summary>
    public static CollectedState<WindowLayoutInfo?> CollectWindowLayoutInfo(
        global::Android.App.Activity activity) =>
        ComposeExtensions.CollectWindowLayoutInfo(
            ComposableContext.Current,
            activity);
}
