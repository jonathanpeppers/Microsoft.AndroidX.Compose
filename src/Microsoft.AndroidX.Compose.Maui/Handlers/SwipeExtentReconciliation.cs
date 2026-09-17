namespace Microsoft.AndroidX.Compose.Maui.Handlers;

internal enum SwipeExtentReconciliation
{
    None,
    Close,
    SnapOpen,
}

internal static class SwipeExtentReconciliationPolicy
{
    public static SwipeExtentReconciliation Resolve(
        bool isOpen,
        bool isSettledOpen,
        bool isDragging,
        bool isAnimating,
        float extent)
    {
        if (!isOpen)
            return SwipeExtentReconciliation.None;
        if (extent <= 0f)
            return SwipeExtentReconciliation.Close;
        return isSettledOpen && !isDragging && !isAnimating
            ? SwipeExtentReconciliation.SnapOpen
            : SwipeExtentReconciliation.None;
    }
}
