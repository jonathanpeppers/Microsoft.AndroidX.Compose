namespace Microsoft.AndroidX.Compose.Maui.Handlers;

internal static class SwipeThresholdPolicy
{
    const float DefaultOpenFraction = 0.6f;

    public static float ResolveOpenDistancePixels(
        double thresholdDp,
        float density,
        float panelExtentPixels)
    {
        if (panelExtentPixels <= 0f)
            return 0f;
        if (thresholdDp <= 0d)
            return panelExtentPixels * DefaultOpenFraction;
        return System.Math.Min(
            (float)thresholdDp * density,
            panelExtentPixels);
    }
}
