using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>NavigationRailItem</c>. Used inside
/// <see cref="NavigationRail"/>.
/// </summary>
public sealed class NavigationRailItem : ComposableNode
{
    readonly bool _selected;
    readonly System.Action _onClick;

    public NavigationRailItem(bool selected, System.Action onClick)
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
                "NavigationRailItem.Icon is required (the Kotlin parameter has no default).");

        var click = new ComposableLambda0(_onClick);
        var icon  = new ComposableLambda2(c => Icon.Render(c));
        ComposableLambda2? label = Label is null ? null : new ComposableLambda2(c => Label.Render(c));

        int defaults = (int)NavigationRailItemDefault.All;
        if (label is not null) defaults &= ~(int)NavigationRailItemDefault.Label;

        ComposeBridges.NavigationRailItem(_selected, click, icon, label, defaults, composer);
    }
}
