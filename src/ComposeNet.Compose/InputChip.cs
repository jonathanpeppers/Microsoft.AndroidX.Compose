using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>InputChip</c>. Adds an <see cref="Avatar"/> slot in
/// addition to the leading/trailing slots common to the chip family.
/// </summary>
public sealed class InputChip : ComposableNode
{
    readonly bool _selected;
    readonly System.Action _onClick;

    public InputChip(bool selected, System.Action onClick)
    {
        _selected = selected;
        _onClick  = onClick;
    }

    /// <summary>Required: chip text.</summary>
    public ComposableNode? Label { get; set; }

    public ComposableNode? LeadingIcon  { get; set; }
    public ComposableNode? Avatar       { get; set; }
    public ComposableNode? TrailingIcon { get; set; }

    internal override void Render(IComposer composer)
    {
        if (Label is null)
            throw new System.InvalidOperationException(
                "InputChip.Label is required (the Kotlin parameter has no default).");

        var click = new ComposableLambda0(_onClick);
        var label = ComposableLambdas.Wrap2(composer, c => Label.Render(c));
        var leading  = LeadingIcon  is null ? null : ComposableLambdas.Wrap2(composer, c => LeadingIcon.Render(c));
        var avatar   = Avatar       is null ? null : ComposableLambdas.Wrap2(composer, c => Avatar.Render(c));
        var trailing = TrailingIcon is null ? null : ComposableLambdas.Wrap2(composer, c => TrailingIcon.Render(c));

        int defaults = (int)InputChipDefault.All;
        var modifier = BuildModifier();
        if (modifier is not null) defaults &= ~(int)InputChipDefault.Modifier;
        if (leading  is not null) defaults &= ~(int)InputChipDefault.LeadingIcon;
        if (avatar   is not null) defaults &= ~(int)InputChipDefault.Avatar;
        if (trailing is not null) defaults &= ~(int)InputChipDefault.TrailingIcon;

        ComposeBridges.InputChip(_selected, click, label, modifier, leading, avatar, trailing, defaults, composer);
    }
}
