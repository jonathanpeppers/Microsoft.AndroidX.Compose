using AndroidX.Compose.Material3;
using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>Checkbox</c>:
/// <code>
/// new Checkbox(checked: state.Value, onCheckedChange: v => state.Value = v)
/// </code>
/// </summary>
public sealed class Checkbox : ComposableNode
{
    readonly bool _checked;
    readonly System.Action<bool> _onCheckedChange;

    public Checkbox(bool @checked, System.Action<bool> onCheckedChange)
    {
        _checked = @checked;
        _onCheckedChange = onCheckedChange;
    }

    internal override void Render(IComposer composer)
    {
        var onChange = new ComposableLambda1(v =>
            _onCheckedChange(v is Java.Lang.Boolean b && b.BooleanValue()));

        var modifier = BuildModifier();
        int defaults = (int)CheckboxDefault.All;
        if (modifier is not null) defaults &= ~(int)CheckboxDefault.Modifier;

        CheckboxKt.Checkbox(
            @checked:          _checked,
            onCheckedChange:   onChange,
            modifier:          modifier,
            enabled:           true,
            colors:            null,
            interactionSource: null,
            _composer:         composer,
            p7:                0,
            _changed:          defaults);
    }
}
