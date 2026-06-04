using AndroidX.Compose.Material3;
using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>Switch</c>:
/// <code>
/// new Switch(@checked: state.Value, onCheckedChange: v => state.Value = v)
/// </code>
/// </summary>
public sealed class Switch : ComposableNode
{
    readonly bool _checked;
    readonly System.Action<bool> _onCheckedChange;

    public Switch(bool @checked, System.Action<bool> onCheckedChange)
    {
        _checked = @checked;
        _onCheckedChange = onCheckedChange;
    }

    internal override void Render(IComposer composer)
    {
        var onChange = new ComposableLambda1(v =>
            _onCheckedChange(v is Java.Lang.Boolean b && b.BooleanValue()));

        var modifier = BuildModifier();
        int defaults = (int)SwitchDefault.All;
        if (modifier is not null) defaults &= ~(int)SwitchDefault.Modifier;

        SwitchKt.Switch(
            @checked:          _checked,
            onCheckedChange:   onChange,
            modifier:          modifier,
            thumbContent:      null,
            enabled:           true,
            colors:            null,
            interactionSource: null,
            _composer:         composer,
            p8:                0,
            _changed:          defaults);
    }
}
