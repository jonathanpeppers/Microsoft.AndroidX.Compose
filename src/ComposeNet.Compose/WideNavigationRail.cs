using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>WideNavigationRail</c> — adaptive vertical navigation
/// rail that shows item labels when expanded. Children are
/// <see cref="WideNavigationRailItem"/>s. Stripped from the binding
/// because the underlying composable is annotated
/// <c>@ExperimentalMaterial3ExpressiveApi</c>; reached via
/// <see cref="ComposeBridges"/>.
/// <code>
/// new WideNavigationRail
/// {
///     new WideNavigationRailItem(selected: tab == 0, onClick: () =&gt; tab.Value = 0)
///     {
///         Icon  = new Text("🏠"),
///         Label = new Text("Home"),
///     },
/// }
/// </code>
/// </summary>
public sealed class WideNavigationRail : ComposableContainer
{
    internal override void Render(IComposer composer)
    {
        // WideNavigationRailItem is a top-level static (not a scope
        // extension), so we don't need to publish a receiver scope —
        // children render directly.
        var content = ComposableLambdas.Wrap2(composer, c => RenderChildren(c));
        ComposeBridges.WideNavigationRail(BuildModifier(), content, composer);
    }
}
