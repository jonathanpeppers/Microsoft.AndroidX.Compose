using AndroidX.Compose.Material3;
using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>RadioButton</c>. Typically used inside a
/// <see cref="Row"/> alongside a <see cref="Text"/> label, with the
/// parent state tracking which option is selected:
/// <code>
/// new RadioButton(selected: pick.Value == "A", onClick: () => pick.Value = "A")
/// </code>
/// </summary>
public sealed class RadioButton : ComposableNode
{
    readonly bool _selected;
    readonly System.Action _onClick;

    public RadioButton(bool selected, System.Action onClick)
    {
        _selected = selected;
        _onClick = onClick;
    }

    internal override void Render(IComposer composer)
    {
        var onClick = new ComposableLambda0(_onClick);

        var modifier = BuildModifier();
        int defaults = (int)RadioButtonDefault.All;
        if (modifier is not null) defaults &= ~(int)RadioButtonDefault.Modifier;

        RadioButtonKt.RadioButton(
            selected:          _selected,
            onClick:           onClick,
            modifier:          modifier,
            enabled:           true,
            colors:            null,
            interactionSource: null,
            _composer:         composer,
            p7:                0,
            _changed:          defaults);
    }
}
