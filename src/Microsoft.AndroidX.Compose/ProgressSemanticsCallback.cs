using Android.Runtime;
using Kotlin.Jvm.Functions;

namespace AndroidX.Compose;

[Register("net/compose/ProgressSemanticsCallback")]
internal sealed class ProgressSemanticsCallback : Java.Lang.Object, IFunction1
{
    readonly Func<float, bool> _callback;

    public ProgressSemanticsCallback(Func<float, bool> callback) =>
        _callback = callback ?? throw new ArgumentNullException(nameof(callback));

    public Java.Lang.Object Invoke(Java.Lang.Object? value)
    {
        var number = value as Java.Lang.Float
            ?? throw new InvalidOperationException("Progress accessibility action did not provide a Float.");
        return _callback(number.FloatValue()) ? Java.Lang.Boolean.True! : Java.Lang.Boolean.False!;
    }
}
