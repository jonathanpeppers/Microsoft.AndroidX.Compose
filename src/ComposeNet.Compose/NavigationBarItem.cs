using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>NavigationBarItem</c>. Must be a child of
/// <see cref="NavigationBar"/> — the Kotlin static method takes a
/// <c>RowScope</c> extension receiver, which the parent
/// <see cref="NavigationBar"/> publishes via <c>RenderContext</c>.
/// <see cref="Icon"/> is required; <see cref="Label"/> is optional.
/// </summary>
public sealed class NavigationBarItem : ComposableNode
{
    readonly bool _selected;
    readonly System.Action _onClick;

    public NavigationBarItem(bool selected, System.Action onClick)
    {
        _selected = selected;
        _onClick  = onClick;
    }

    /// <summary>Required: item icon.</summary>
    public ComposableNode? Icon { get; set; }

    /// <summary>Optional: item label.</summary>
    public ComposableNode? Label { get; set; }

    internal override void Render(IComposer composer)
    {
        if (Icon is null)
            throw new System.InvalidOperationException(
                "NavigationBarItem.Icon is required (the Kotlin parameter has no default).");

        var click = new ComposableLambda0(_onClick);
        var icon  = new ComposableLambda2(c => Icon.Render(c));
        ComposableLambda2? label = Label is null ? null : new ComposableLambda2(c => Label.Render(c));

        int defaults = (int)NavigationBarItemDefault.All;
        var modifier = BuildModifier();
        if (modifier is not null) defaults &= ~(int)NavigationBarItemDefault.Modifier;
        if (label    is not null) defaults &= ~(int)NavigationBarItemDefault.Label;

        ComposeBridges.NavigationBarItem(
            rowScope: RenderContext.CurrentScope,
            selected: _selected,
            onClick:  click,
            icon:     icon,
            modifier: modifier,
            label:    label,
            defaults: defaults,
            composer: composer);
    }
}
