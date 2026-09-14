using Kotlin.Jvm.Functions;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Minimum merger for a bound custom alignment line in scope tests.</summary>
internal sealed class BaselineLineMerger : Java.Lang.Object, IFunction2
{
    public Java.Lang.Object Invoke(Java.Lang.Object? first, Java.Lang.Object? second)
    {
        if (first is not Java.Lang.Integer a || second is not Java.Lang.Integer b)
            throw new InvalidOperationException("Alignment-line merger requires two integers.");
        return Java.Lang.Integer.ValueOf(Math.Min(a.IntValue(), b.IntValue()));
    }
}
