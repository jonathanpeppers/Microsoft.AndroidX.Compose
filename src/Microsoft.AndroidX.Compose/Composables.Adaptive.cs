using AndroidX.Compose.Material3.Adaptive;

namespace AndroidX.Compose;

public static partial class Composables
{
    /// <summary>Reads the adaptive information for the current host window.</summary>
    public static WindowAdaptiveInfo CurrentWindowAdaptiveInfo(
        bool supportLargeAndXLargeWidth = false) =>
        ComposeExtensions.CurrentWindowAdaptiveInfo(
            ComposableContext.Current, supportLargeAndXLargeWidth);
}
