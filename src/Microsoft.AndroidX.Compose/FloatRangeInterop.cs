using Android.Runtime;
using Kotlin.Ranges;

namespace AndroidX.Compose;

internal static class FloatRangeInterop
{
    internal static IClosedFloatingPointRange ToKotlin(this FloatRange range) =>
        RangesKt.RangeTo(range.Start, range.End);

    internal static FloatRange FromKotlin(IClosedFloatingPointRange range)
    {
        ArgumentNullException.ThrowIfNull(range);

        var start = range.Start as Java.Lang.Float
            ?? throw new InvalidOperationException("Kotlin slider range did not expose a Float start endpoint.");
        var end = range.EndInclusive as Java.Lang.Float
            ?? throw new InvalidOperationException("Kotlin slider range did not expose a Float end endpoint.");
        return new FloatRange(start.FloatValue(), end.FloatValue());
    }

    internal static ComposableLambda1 WrapCallback(Action<FloatRange> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        return new ComposableLambda1(value =>
        {
            var range = value?.JavaCast<IClosedFloatingPointRange>()
                ?? throw new InvalidOperationException("RangeSlider callback did not provide a floating-point range.");
            callback(FromKotlin(range));
        });
    }
}
