using AndroidX.Compose.Runtime;

namespace AndroidX.Compose;

public static partial class Composables
{
    /// <summary>
    /// Tier 2 entry point for <see cref="Text"/>. The generator-emitted
    /// wrapper skips its body when <paramref name="text"/> is unchanged
    /// from the previous composition.
    /// </summary>
    [Composable]
    public static void Text(IComposer composer, string text) =>
        new global::AndroidX.Compose.Text(text).Render(composer);
}
