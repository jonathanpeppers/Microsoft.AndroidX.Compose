namespace Microsoft.AndroidX.Compose.Maui.Handlers;

internal static class SwipePanelSizing
{
    public static bool UsesIntrinsicVerticalSize(
        bool horizontal,
        bool hasCustomItem) =>
        !horizontal && hasCustomItem;

    public static float? RequestedCustomHeight(double heightRequest) =>
        heightRequest >= 0d ? (float)heightRequest : null;

    public static float CustomWidth(double widthRequest, float minimum) =>
        widthRequest >= 0d
            ? System.Math.Max((float)widthRequest, minimum)
            : minimum;

    public static bool UsesExecutionWidth(
        bool horizontal,
        bool executeMode,
        bool hasCustomItem) =>
        horizontal && executeMode && !hasCustomItem;
}
