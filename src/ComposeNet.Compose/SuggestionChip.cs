using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>SuggestionChip</c>. Single-icon variant — only an
/// <see cref="Icon"/> slot is exposed (vs. AssistChip's leading + trailing).
/// </summary>
public sealed class SuggestionChip : ComposableNode
{
    readonly System.Action _onClick;
    public SuggestionChip(System.Action onClick) => _onClick = onClick;

    /// <summary>Required: chip text.</summary>
    public ComposableNode? Label { get; set; }

    /// <summary>Optional: leading slot.</summary>
    public ComposableNode? Icon { get; set; }

    internal override void Render(IComposer composer)
    {
        if (Label is null)
            throw new System.InvalidOperationException(
                "SuggestionChip.Label is required (the Kotlin parameter has no default).");

        var click = new ComposableLambda0(_onClick);
        var label = ComposableLambdas.Wrap2(composer, c => Label.Render(c));
        var icon = Icon is null ? null : ComposableLambdas.Wrap2(composer, c => Icon.Render(c));

        int defaults = (int)SuggestionChipDefault.All;
        var modifier = BuildModifier();
        if (modifier is not null) defaults &= ~(int)SuggestionChipDefault.Modifier;
        if (icon     is not null) defaults &= ~(int)SuggestionChipDefault.Icon;

        ComposeBridges.SuggestionChip(click, label, modifier, icon, defaults, composer);
    }
}
