using AndroidX.Compose.Material3;
using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>Slider</c>:
/// <code>
/// new Slider(value: pos.Value, onValueChange: v => pos.Value = v)
/// </code>
/// Calls the simple <c>(Float, (Float) -&gt; Unit)</c> overload — the
/// richer overloads (with custom <c>thumb</c> / <c>track</c> slots, or
/// a <c>SliderState</c> first param) aren't exposed in this facade.
/// </summary>
public sealed class Slider : ComposableNode
{
    readonly float _value;
    readonly System.Action<float> _onValueChange;

    public Slider(float value, System.Action<float> onValueChange)
    {
        _value = value;
        _onValueChange = onValueChange;
    }

    internal override void Render(IComposer composer)
    {
        var onChange = new ComposableLambda1(v =>
            _onValueChange(v is Java.Lang.Float f ? f.FloatValue() : 0f));

        var modifier = BuildModifier();
        int defaults = (int)SliderDefault.All;
        if (modifier is not null) defaults &= ~(int)SliderDefault.Modifier;

        SliderKt.Slider(
            value:                  _value,
            onValueChange:          onChange,
            modifier:               modifier,
            enabled:                true,
            valueRange:             null,
            p5:                     0,
            onValueChangeFinished:  null,
            colors:                 null,
            interactionSource:      null,
            _composer:              composer,
            steps:                  0,
            _changed:               defaults);
    }
}
