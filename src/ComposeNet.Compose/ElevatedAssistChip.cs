using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>ElevatedAssistChip</c> — same API surface as
/// <see cref="AssistChip"/> but uses shadow elevation for emphasis.
/// </summary>
public sealed class ElevatedAssistChip : ComposableNode
{
    readonly System.Action _onClick;
    public ElevatedAssistChip(System.Action onClick) => _onClick = onClick;

    /// <summary>Required: chip text.</summary>
    public ComposableNode? Label { get; set; }

    public ComposableNode? LeadingIcon  { get; set; }
    public ComposableNode? TrailingIcon { get; set; }

    internal override void Render(IComposer composer)
    {
        if (Label is null)
            throw new System.InvalidOperationException(
                "ElevatedAssistChip.Label is required (the Kotlin parameter has no default).");

        var click = new ComposableLambda0(_onClick);
        var label = new ComposableLambda2(c => Label.Render(c));
        ComposableLambda2? leading  = LeadingIcon  is null ? null : new ComposableLambda2(c => LeadingIcon.Render(c));
        ComposableLambda2? trailing = TrailingIcon is null ? null : new ComposableLambda2(c => TrailingIcon.Render(c));

        // ElevatedAssistChip's parameter order matches AssistChip exactly,
        // so we reuse AssistChipDefault for the $default bitmask.
        int defaults = (int)AssistChipDefault.All;
        if (leading  is not null) defaults &= ~(int)AssistChipDefault.LeadingIcon;
        if (trailing is not null) defaults &= ~(int)AssistChipDefault.TrailingIcon;

        ComposeBridges.ElevatedAssistChip(click, label, leading, trailing, defaults, composer);
    }
}
