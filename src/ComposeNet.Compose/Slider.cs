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
public sealed partial class Slider;
