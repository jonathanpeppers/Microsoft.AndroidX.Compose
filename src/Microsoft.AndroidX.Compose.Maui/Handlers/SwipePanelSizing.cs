namespace Microsoft.AndroidX.Compose.Maui.Handlers;

internal static class SwipePanelSizing
{
    public static bool UsesIntrinsicVerticalSize(
        bool horizontal,
        bool hasCustomItem) =>
        !horizontal && hasCustomItem;

    public static float? RequestedCustomHeight(double heightRequest) =>
        heightRequest >= 0d ? (float)heightRequest : null;
}
