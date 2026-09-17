using Microsoft.Maui;

namespace Microsoft.AndroidX.Compose.Maui.Handlers;

internal static class SwipeInvocationPolicy
{
    public static bool ShouldRemainOpen(
        SwipeMode mode,
        SwipeBehaviorOnInvoked behavior) =>
        behavior == SwipeBehaviorOnInvoked.RemainOpen ||
        behavior == SwipeBehaviorOnInvoked.Auto &&
        mode == SwipeMode.Execute;
}
