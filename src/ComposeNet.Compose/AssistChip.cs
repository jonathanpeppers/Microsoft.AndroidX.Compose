using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>AssistChip</c>. <see cref="Label"/> is required;
/// <see cref="LeadingIcon"/> and <see cref="TrailingIcon"/> are optional
/// slots:
/// <code>
/// new AssistChip(onClick: ...) { Label = new Text("Filter") }
/// </code>
/// </summary>
public sealed class AssistChip : ComposableNode
{
    readonly System.Action _onClick;
    public AssistChip(System.Action onClick) => _onClick = onClick;

    /// <summary>Required: chip text.</summary>
    public ComposableNode? Label { get; set; }

    /// <summary>Optional: leading slot (e.g. icon).</summary>
    public ComposableNode? LeadingIcon { get; set; }

    /// <summary>Optional: trailing slot.</summary>
    public ComposableNode? TrailingIcon { get; set; }

    internal override void Render(IComposer composer)
    {
        if (Label is null)
            throw new System.InvalidOperationException(
                "AssistChip.Label is required (the Kotlin parameter has no default).");

        var click = new ComposableLambda0(_onClick);
        var label = new ComposableLambda2(c => Label.Render(c));
        ComposableLambda2? leading  = LeadingIcon  is null ? null : new ComposableLambda2(c => LeadingIcon.Render(c));
        ComposableLambda2? trailing = TrailingIcon is null ? null : new ComposableLambda2(c => TrailingIcon.Render(c));

        int defaults = (int)AssistChipDefault.All;
        if (leading  is not null) defaults &= ~(int)AssistChipDefault.LeadingIcon;
        if (trailing is not null) defaults &= ~(int)AssistChipDefault.TrailingIcon;

        ComposeBridges.AssistChip(click, label, leading, trailing, defaults, composer);
    }
}
