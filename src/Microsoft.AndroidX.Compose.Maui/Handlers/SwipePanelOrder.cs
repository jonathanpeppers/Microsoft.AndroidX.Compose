namespace Microsoft.AndroidX.Compose.Maui.Handlers;

internal static class SwipePanelOrder
{
    public static bool ShouldReverse(int swipeDirection) => swipeDirection == 2;
}
