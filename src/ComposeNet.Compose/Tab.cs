using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>Tab</c>. Place inside a <see cref="TabRow"/> /
/// <see cref="PrimaryTabRow"/> / <see cref="SecondaryTabRow"/> /
/// <see cref="ScrollableTabRow"/>. <see cref="Text"/> and
/// <see cref="Icon"/> are both optional but at least one should be
/// supplied for the tab to be visible.
/// </summary>
public sealed class Tab : ComposableNode
{
    readonly bool _selected;
    readonly System.Action _onClick;

    public Tab(bool selected, System.Action onClick)
    {
        _selected = selected;
        _onClick  = onClick;
    }

    /// <summary>Optional: tab label slot.</summary>
    public ComposableNode? Text { get; set; }

    /// <summary>Optional: tab icon slot.</summary>
    public ComposableNode? Icon { get; set; }

    internal override void Render(IComposer composer)
    {
        var click = new ComposableLambda0(_onClick);
        var text = Text is null ? null : ComposableLambdas.Wrap2(composer, c => Text.Render(c));
        var icon = Icon is null ? null : ComposableLambdas.Wrap2(composer, c => Icon.Render(c));

        int defaults = (int)TabDefault.All;
        var modifier = BuildModifier();
        if (modifier is not null) defaults &= ~(int)TabDefault.Modifier;
        if (text     is not null) defaults &= ~(int)TabDefault.Text;
        if (icon     is not null) defaults &= ~(int)TabDefault.Icon;

        ComposeBridges.Tab(_selected, click, modifier, text, icon, defaults, composer);
    }
}
