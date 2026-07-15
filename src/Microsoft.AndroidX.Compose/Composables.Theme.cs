using AndroidX.Compose.Material3;

namespace AndroidX.Compose;

public static partial class Composables
{
    /// <summary>Reads the current Material color scheme.</summary>
    public static ColorScheme ColorScheme() =>
        ComposeExtensions.ColorScheme(ComposableContext.Current);

    /// <summary>Reads the current Material typography.</summary>
    public static Typography Typography() =>
        ComposeExtensions.Typography(ComposableContext.Current);

    /// <summary>Reads the current Material shapes.</summary>
    public static Shapes Shapes() =>
        ComposeExtensions.Shapes(ComposableContext.Current);
}
