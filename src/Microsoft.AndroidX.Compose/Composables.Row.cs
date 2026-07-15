using AndroidX.Compose.Runtime;

namespace AndroidX.Compose;

public static partial class Composables
{
    /// <summary>
    /// Implicit-composer Tier 2 entry point for
    /// <see cref="global::AndroidX.Compose.Row"/>.
    /// </summary>
    [Composable]
    public static void Row([ComposableContent] Action content)
    {
        ArgumentNullException.ThrowIfNull(content);

        new global::AndroidX.Compose.Row
        {
            new Tier2InlineContent(_ => content()),
        }.Render(ComposableContext.Current);
    }

    /// <summary>
    /// Tier 2 entry point for
    /// <see cref="global::AndroidX.Compose.Row"/>. The wrapper skips
    /// when <paramref name="content"/> identity is unchanged from the
    /// previous composition.
    /// </summary>
    [Composable]
    public static void Row(
        IComposer composer,
        [ComposableContent] Action<IComposer> content)
    {
        ArgumentNullException.ThrowIfNull(content);

        new global::AndroidX.Compose.Row { new Tier2InlineContent(content) }.Render(composer);
    }
}
