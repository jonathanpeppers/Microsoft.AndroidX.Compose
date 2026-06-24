using AndroidX.Compose.Runtime;

namespace AndroidX.Compose;

public static partial class Composables
{
    /// <summary>
    /// Tier 2 entry point for <see cref="Column"/>. The wrapper skips
    /// when <paramref name="content"/> identity is unchanged from the
    /// previous composition.
    /// </summary>
    [Composable]
    public static partial void Column(IComposer composer, Action<IComposer> content, int _changed = 0);

    static void ColumnImpl(IComposer composer, Action<IComposer> content, int _changed = 0)
    {
        ArgumentNullException.ThrowIfNull(content);

        new global::AndroidX.Compose.Column { new Tier2InlineContent(content) }.Render(composer);
    }
}
