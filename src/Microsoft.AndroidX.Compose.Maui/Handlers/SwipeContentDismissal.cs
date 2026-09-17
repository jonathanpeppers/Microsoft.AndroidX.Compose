namespace Microsoft.AndroidX.Compose.Maui.Handlers;

internal static class SwipeContentDismissal
{
    public static bool ShouldDismiss(
        bool ownerEnabled,
        bool isOpen,
        bool isSettledOpen,
        bool isDragging) =>
        ownerEnabled && isOpen && isSettledOpen && !isDragging;
}
