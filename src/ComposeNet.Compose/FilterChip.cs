using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>FilterChip</c>. Renders as either selected or unselected;
/// the <c>onClick</c> handler typically toggles the bound boolean state.
/// </summary>
public sealed class FilterChip : ComposableNode
{
    readonly bool _selected;
    readonly System.Action _onClick;

    public FilterChip(bool selected, System.Action onClick)
    {
        _selected = selected;
        _onClick  = onClick;
    }

    /// <summary>Required: chip text.</summary>
    public ComposableNode? Label { get; set; }

    /// <summary>Optional: leading slot (typically the check / unselected icon).</summary>
    public ComposableNode? LeadingIcon { get; set; }

    /// <summary>Optional: trailing slot.</summary>
    public ComposableNode? TrailingIcon { get; set; }

    internal override void Render(IComposer composer)
    {
        if (Label is null)
            throw new System.InvalidOperationException(
                "FilterChip.Label is required (the Kotlin parameter has no default).");

        var click = new ComposableLambda0(_onClick);
        var label = new ComposableLambda2(c => Label.Render(c));
        ComposableLambda2? leading  = LeadingIcon  is null ? null : new ComposableLambda2(c => LeadingIcon.Render(c));
        ComposableLambda2? trailing = TrailingIcon is null ? null : new ComposableLambda2(c => TrailingIcon.Render(c));

        int defaults = (int)FilterChipDefault.All;
        if (leading  is not null) defaults &= ~(int)FilterChipDefault.LeadingIcon;
        if (trailing is not null) defaults &= ~(int)FilterChipDefault.TrailingIcon;

        ComposeBridges.FilterChip(_selected, click, label, leading, trailing, defaults, composer);
    }
}
