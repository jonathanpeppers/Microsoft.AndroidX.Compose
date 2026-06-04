using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>NavigationBar</c>. Container for
/// <see cref="NavigationBarItem"/> children laid out horizontally:
/// <code>
/// new NavigationBar
/// {
///     new NavigationBarItem(selected: tab == 0, onClick: () =&gt; tab.Value = 0)
///     {
///         Icon = new Text("🏠"), Label = new Text("Home"),
///     },
///     new NavigationBarItem(selected: tab == 1, onClick: () =&gt; tab.Value = 1)
///     {
///         Icon = new Text("⚙"), Label = new Text("Settings"),
///     },
/// }
/// </code>
/// </summary>
public sealed class NavigationBar : ComposableContainer
{
    internal override void Render(IComposer composer)
    {
        // Capture the RowScope receiver (p0 of the Function3) and publish
        // it so child NavigationBarItems can pass it to their underlying
        // RowScope-extension static.
        var content = ComposableLambdas.Wrap3(composer, (scope, c) =>
        {
            using var _ = RenderContext.PushScope(scope, ScopeKind.Row);
            RenderChildren(c);
        });
        ComposeBridges.NavigationBar(BuildModifier(), content, composer);
    }
}
