using AndroidX.Compose.Material3;
using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>DismissibleDrawerSheet</c> — the panel shown by a
/// dismissible-style navigation drawer.
/// </summary>
public sealed class DismissibleDrawerSheet : ComposableContainer
{
    /// <summary>
    /// Optional container color. <c>0L</c> (the default) uses the
    /// active theme's <c>secondaryContainer</c>.
    /// </summary>
    public long ContainerColor { get; set; }

    internal override void Render(IComposer composer)
    {
        var content = new ComposableLambda3(c => RenderChildren(c));
        var color = ContainerColor != 0L
            ? ContainerColor
            : AndroidX.Compose.Material3.MaterialTheme.Instance.GetColorScheme(composer, 0).SecondaryContainer;
        ComposeBridges.DismissibleDrawerSheet(content, color, composer);
    }
}
