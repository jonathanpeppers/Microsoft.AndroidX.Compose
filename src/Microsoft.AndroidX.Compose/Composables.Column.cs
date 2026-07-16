using AndroidX.Compose.Runtime;

namespace AndroidX.Compose;

public static partial class Composables
{
    /// <summary>
    /// Implicit-composer entry point for
    /// <see cref="global::AndroidX.Compose.Column"/>.
    /// </summary>
    [Composable]
    public static void Column([ComposableContent] Action content)
    {
        ArgumentNullException.ThrowIfNull(content);

        new global::AndroidX.Compose.Column
        {
            new ComposableContentNode(_ => content()),
        }.Render();
    }

    /// <summary>
    /// Explicit-composer entry point for
    /// <see cref="global::AndroidX.Compose.Column"/>. The wrapper skips
    /// when <paramref name="content"/> identity is unchanged from the
    /// previous composition.
    /// </summary>
    [Composable]
    internal static void Column(
        IComposer composer,
        [ComposableContent] Action<IComposer> content)
    {
        ArgumentNullException.ThrowIfNull(content);

        new global::AndroidX.Compose.Column { new ComposableContentNode(content) }.Render(composer);
    }
}
