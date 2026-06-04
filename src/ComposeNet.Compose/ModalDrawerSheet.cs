using AndroidX.Compose.Material3;
using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>ModalDrawerSheet</c> — the panel shown by a
/// modal-style navigation drawer. Lays out children as a Column.
/// Typically holds nav items; in this facade any
/// <see cref="ComposableNode"/> works.
/// </summary>
public sealed class ModalDrawerSheet : ComposableContainer
{
    /// <summary>
    /// Optional container color (Compose <c>Color</c> as a packed
    /// <c>long</c>). <c>0L</c> (the default) uses the active
    /// <c>MaterialTheme.colorScheme.secondaryContainer</c>, which is
    /// visibly distinct from <c>surface</c>; pass any other value to
    /// override.
    /// </summary>
    public long ContainerColor { get; set; }

    internal override void Render(IComposer composer)
    {
        var content = ComposableLambdas.Wrap3(composer, c => RenderChildren(c));
        var color = ContainerColor != 0L
            ? ContainerColor
            : AndroidX.Compose.Material3.MaterialTheme.Instance.GetColorScheme(composer, 0).SecondaryContainer;
        ComposeBridges.ModalDrawerSheet(content, color, composer);
    }
}
