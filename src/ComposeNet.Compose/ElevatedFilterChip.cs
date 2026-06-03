using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>ElevatedFilterChip</c> — same API surface as
/// <see cref="FilterChip"/> but uses shadow elevation for emphasis.
/// </summary>
public sealed class ElevatedFilterChip : ComposableNode
{
    readonly bool _selected;
    readonly System.Action _onClick;

    public ElevatedFilterChip(bool selected, System.Action onClick)
    {
        _selected = selected;
        _onClick  = onClick;
    }

    /// <summary>Required: chip text.</summary>
    public ComposableNode? Label { get; set; }

    /// <summary>Optional: leading slot (e.g. icon).</summary>
    public ComposableNode? LeadingIcon  { get; set; }

    /// <summary>Optional: trailing slot.</summary>
    public ComposableNode? TrailingIcon { get; set; }

    internal override void Render(IComposer composer)
    {
        if (Label is null)
            throw new System.InvalidOperationException(
                "ElevatedFilterChip.Label is required (the Kotlin parameter has no default).");

        var click = new ComposableLambda0(_onClick);
        var label = new ComposableLambda2(c => Label.Render(c));
        ComposableLambda2? leading  = LeadingIcon  is null ? null : new ComposableLambda2(c => LeadingIcon.Render(c));
        ComposableLambda2? trailing = TrailingIcon is null ? null : new ComposableLambda2(c => TrailingIcon.Render(c));

        int defaults = (int)FilterChipDefault.All;
        if (leading  is not null) defaults &= ~(int)FilterChipDefault.LeadingIcon;
        if (trailing is not null) defaults &= ~(int)FilterChipDefault.TrailingIcon;

        ComposeBridges.ElevatedFilterChip(_selected, click, label, leading, trailing, defaults, composer);
    }
}
