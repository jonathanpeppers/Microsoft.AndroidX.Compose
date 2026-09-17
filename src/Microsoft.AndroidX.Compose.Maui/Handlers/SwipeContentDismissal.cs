namespace Microsoft.AndroidX.Compose.Maui.Handlers;

internal static class SwipeContentDismissal
{
    public static bool ShouldDismiss(
        bool isOpen,
        bool isSettledOpen,
        bool isDragging) =>
        isOpen && isSettledOpen && !isDragging;
}
