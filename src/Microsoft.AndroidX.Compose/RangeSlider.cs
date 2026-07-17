using AndroidX.Compose.Material3;
using AndroidX.Compose.Runtime;

namespace AndroidX.Compose;

/// <summary>
/// Material 3 <c>RangeSlider</c> using <see cref="FloatRange"/> for both
/// the current selection and change callbacks:
/// <code>
/// new RangeSlider(
///     value: range.Value,
///     onValueChange: r => range.Value = r)
/// </code>
/// Set <see cref="ValueRange"/> to change the overall selectable range;
/// leaving it <see langword="null"/> preserves Material 3's default
/// <c>0f..1f</c> range.
/// </summary>
public sealed class RangeSlider : ComposableNode
{
    readonly FloatRange _value;
    readonly Action<FloatRange> _onValueChange;

    /// <summary>Creates a two-thumb slider for the current selected range.</summary>
    public RangeSlider(
        FloatRange value,
        Action<FloatRange> onValueChange)
    {
        ArgumentNullException.ThrowIfNull(onValueChange);
        _value = value;
        _onValueChange = onValueChange;
    }

    /// <summary>
    /// Optional overall selectable range. <see langword="null"/> uses
    /// Material 3's default <c>0f..1f</c> range.
    /// </summary>
    public FloatRange? ValueRange { get; set; }

    /// <inheritdoc/>
    public override void Render(IComposer composer)
    {
        using var range = _value.ToKotlin();
        using var valueRange = ValueRange is { } overallRange
            ? overallRange.ToKotlin()
            : null;
        var onChange = FloatRangeInterop.WrapCallback(_onValueChange);

        var modifier = BuildModifier();
        int defaults = (int)RangeSliderDefault.All;
        if (modifier is not null) defaults &= ~(int)RangeSliderDefault.Modifier;
        if (valueRange is not null) defaults &= ~(int)RangeSliderDefault.ValueRange;

        SliderKt.RangeSlider(
            value:                  range,
            onValueChange:          onChange,
            modifier:               modifier,
            enabled:                true,
            valueRange:             valueRange,
            p5:                     0,
            onValueChangeFinished:  null,
            colors:                 null,
            _composer:              composer,
            steps:                  0,
            _changed:               defaults);
    }
}
