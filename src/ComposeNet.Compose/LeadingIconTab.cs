using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>LeadingIconTab</c>. Like <see cref="Tab"/> but the
/// icon and text are required and laid out side-by-side (icon first)
/// instead of stacked vertically.
/// </summary>
public sealed class LeadingIconTab : ComposableNode
{
    readonly bool _selected;
    readonly System.Action _onClick;

    public LeadingIconTab(bool selected, System.Action onClick)
    {
        _selected = selected;
        _onClick  = onClick;
    }

    /// <summary>Required: tab label slot.</summary>
    public ComposableNode? Text { get; set; }

    /// <summary>Required: tab icon slot, rendered before the text.</summary>
    public ComposableNode? Icon { get; set; }

    internal override void Render(IComposer composer)
    {
        if (Text is null || Icon is null)
            throw new System.InvalidOperationException(
                "LeadingIconTab.Text and Icon are both required (the Kotlin parameters have no defaults).");

        var click = new ComposableLambda0(_onClick);
        var text  = new ComposableLambda2(c => Text.Render(c));
        var icon  = new ComposableLambda2(c => Icon.Render(c));

        ComposeBridges.LeadingIconTab(_selected, click, text, icon, BuildModifier(), composer);
    }
}
