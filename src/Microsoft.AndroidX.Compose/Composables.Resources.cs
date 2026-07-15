using AndroidX.Compose.UI.Graphics.Painter;

namespace AndroidX.Compose;

public static partial class Composables
{
    /// <summary>Loads a string resource.</summary>
    public static string StringResource(int id) =>
        ComposeExtensions.StringResource(ComposableContext.Current, id);

    /// <summary>Loads and formats a string resource.</summary>
    public static string StringResource(int id, params object?[] formatArgs) =>
        ComposeExtensions.StringResource(
            ComposableContext.Current, id, formatArgs);

    /// <summary>Loads a string-array resource.</summary>
    public static string[] StringArrayResource(int id) =>
        ComposeExtensions.StringArrayResource(ComposableContext.Current, id);

    /// <summary>Loads a plural string resource.</summary>
    public static string PluralStringResource(int id, int count) =>
        ComposeExtensions.PluralStringResource(
            ComposableContext.Current, id, count);

    /// <summary>Loads and formats a plural string resource.</summary>
    public static string PluralStringResource(
        int id,
        int count,
        params object?[] formatArgs) =>
        ComposeExtensions.PluralStringResource(
            ComposableContext.Current, id, count, formatArgs);

    /// <summary>Loads an integer resource.</summary>
    public static int IntegerResource(int id) =>
        ComposeExtensions.IntegerResource(ComposableContext.Current, id);

    /// <summary>Loads an integer-array resource.</summary>
    public static int[] IntegerArrayResource(int id) =>
        ComposeExtensions.IntegerArrayResource(ComposableContext.Current, id);

    /// <summary>Loads a Boolean resource.</summary>
    public static bool BooleanResource(int id) =>
        ComposeExtensions.BooleanResource(ComposableContext.Current, id);

    /// <summary>Loads a dimension resource.</summary>
    public static Dp DimensionResource(int id) =>
        ComposeExtensions.DimensionResource(ComposableContext.Current, id);

    /// <summary>Loads a color resource.</summary>
    public static long ColorResource(int id) =>
        ComposeExtensions.ColorResource(ComposableContext.Current, id);

    /// <summary>Loads a painter resource.</summary>
    public static Painter PainterResource(int id) =>
        ComposeExtensions.PainterResource(ComposableContext.Current, id);
}
