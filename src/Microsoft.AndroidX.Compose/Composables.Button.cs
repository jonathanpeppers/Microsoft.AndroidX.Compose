using AndroidX.Compose.Runtime;

namespace AndroidX.Compose;

public static partial class Composables
{
    /// <summary>
    /// Tier 2 entry point for <see cref="Button"/>. The wrapper skips
    /// when both <paramref name="onClick"/> identity and
    /// <paramref name="content"/> identity are unchanged from the
    /// previous composition.
    /// </summary>
    [Composable]
    public static partial void Button(IComposer composer, Action onClick, Action<IComposer> content, int _changed = 0);

    static void ButtonImpl(IComposer composer, Action onClick, Action<IComposer> content, int _changed = 0)
    {
        ArgumentNullException.ThrowIfNull(onClick);
        ArgumentNullException.ThrowIfNull(content);

        new global::AndroidX.Compose.Button(onClick)
        {
            new Tier2InlineContent(content),
        }.Render(composer);
    }
}
