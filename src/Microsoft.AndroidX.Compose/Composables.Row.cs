using AndroidX.Compose.Runtime;

namespace AndroidX.Compose;

public static partial class Composables
{
    /// <summary>
    /// Tier 2 entry point for <see cref="Row"/>. The wrapper skips
    /// when <paramref name="content"/> identity is unchanged from the
    /// previous composition.
    /// </summary>
    [Composable]
    public static void Row(IComposer composer, Action<IComposer> content)
    {
        ArgumentNullException.ThrowIfNull(content);

        new global::AndroidX.Compose.Row { new Tier2InlineContent(content) }.Render(composer);
    }
}
