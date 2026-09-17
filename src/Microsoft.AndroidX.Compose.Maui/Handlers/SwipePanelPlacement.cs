namespace Microsoft.AndroidX.Compose.Maui.Handlers;

internal static class SwipePanelPlacement
{
    public const int LeftItems = 0;
    public const int RightItems = 1;
    public const int TopItems = 2;
    public const int BottomItems = 3;
    public const int None = -1;

    public static int ActivePanelIndex(int swipeDirection) => swipeDirection switch
    {
        1 => LeftItems,
        2 => RightItems,
        8 => TopItems,
        4 => BottomItems,
        _ => None,
    };
}
