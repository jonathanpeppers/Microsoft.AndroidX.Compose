using AndroidX.Compose.Material3;
using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>WideNavigationRailItem</c>. Used inside
/// <see cref="WideNavigationRail"/>. <see cref="Icon"/> is required;
/// <see cref="Label"/> is optional.
/// </summary>
public sealed class WideNavigationRailItem : ComposableNode
{
    readonly bool _selected;
    readonly System.Action _onClick;

    public WideNavigationRailItem(bool selected, System.Action onClick)
    {
        _selected = selected;
        _onClick  = onClick;
    }

    /// <summary>Required: item icon.</summary>
    public ComposableNode? Icon { get; set; }

    /// <summary>Optional: item label (only shown when the rail is expanded).</summary>
    public ComposableNode? Label { get; set; }

    internal override void Render(IComposer composer)
    {
        if (Icon is null)
            throw new System.InvalidOperationException(
                "WideNavigationRailItem.Icon is required (the Kotlin parameter has no default).");

        var click = new ComposableLambda0(_onClick);
        var icon  = new ComposableLambda2(c => Icon.Render(c));
        ComposableLambda2? label = Label is null ? null : new ComposableLambda2(c => Label.Render(c));

        int defaults = (int)WideNavigationRailItemDefault.All;
        var modifier = BuildModifier();
        if (modifier is not null) defaults &= ~(int)WideNavigationRailItemDefault.Modifier;
        if (label    is not null) defaults &= ~(int)WideNavigationRailItemDefault.Label;

        // Bound C# wrapper has misnamed trailing params: `iconPosition`
        // is actually $changed, `_changed` is actually $default. The
        // mid-list `int p7` is the real iconPosition (we don't expose it).
        WideNavigationRailKt.WideNavigationRailItem(
            selected:          _selected,
            onClick:           click,
            icon:              icon,
            label:             label,
            railExpanded:      false,
            modifier:          modifier,
            enabled:           true,
            p7:                0,
            colors:            null,
            interactionSource: null,
            _composer:         composer,
            iconPosition:      0,
            _changed:          defaults);
    }
}
