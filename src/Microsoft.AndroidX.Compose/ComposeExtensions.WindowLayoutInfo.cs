using AndroidX.Compose.Runtime;
using AndroidX.Window.Layout;

namespace AndroidX.Compose;

public static partial class ComposeExtensions
{
    /// <summary>
    /// Collects Jetpack WindowManager's raw layout information for an activity
    /// while its Compose lifecycle is active.
    /// </summary>
    /// <remarks>
    /// Prefer <see cref="CurrentWindowAdaptiveInfo(IComposer, bool)"/> for
    /// adaptive pane decisions. This lower-level API exposes
    /// <see cref="WindowLayoutInfo.DisplayFeatures"/>, including bound
    /// <see cref="IFoldingFeature"/> instances and their exact bounds,
    /// orientation, posture, occlusion, and separating state.
    /// </remarks>
    public static CollectedState<WindowLayoutInfo?> CollectWindowLayoutInfo(
        this IComposer composer,
        global::Android.App.Activity activity)
    {
        ArgumentNullException.ThrowIfNull(composer);
        ArgumentNullException.ThrowIfNull(activity);

        var flow = composer.Remember(
            () => WindowInfoTracker.GetOrCreate(activity)
                .WindowLayoutInfo(activity),
            activity);
        return flow.CollectAsStateWithLifecycle<WindowLayoutInfo?>(
            initialValue: null,
            composer);
    }
}
