using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>NavigationRail</c>. Vertical analog of
/// <see cref="NavigationBar"/>. Children are <see cref="NavigationRailItem"/>s:
/// <code>
/// new NavigationRail
/// {
///     new NavigationRailItem(selected: tab == 0, onClick: ...) { Icon = ..., Label = ... },
///     new NavigationRailItem(selected: tab == 1, onClick: ...) { Icon = ..., Label = ... },
/// }
/// </code>
/// </summary>
public sealed class NavigationRail : ComposableContainer
{
    internal override void Render(IComposer composer)
    {
        // NavigationRailItem (unlike NavigationBarItem) is a top-level
        // static, not a ColumnScope extension — so we don't need to
        // publish the scope. Children can render directly.
        var content = new ComposableLambda3(c => RenderChildren(c));
        ComposeBridges.NavigationRail(content, composer);
    }
}
