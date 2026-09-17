namespace Microsoft.AndroidX.Compose.Maui.Handlers;

internal static class SwipeInvocationPolicy
{
    public static bool ShouldRemainOpen(
        global::Microsoft.Maui.SwipeMode mode,
        global::Microsoft.Maui.SwipeBehaviorOnInvoked behavior) =>
        behavior == global::Microsoft.Maui.SwipeBehaviorOnInvoked.RemainOpen ||
        behavior == global::Microsoft.Maui.SwipeBehaviorOnInvoked.Auto &&
        mode == global::Microsoft.Maui.SwipeMode.Execute;
}
