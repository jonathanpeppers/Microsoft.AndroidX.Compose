using Android.Runtime;
using Kotlin.Jvm.Functions;

namespace AndroidX.Compose;

[Register("net/compose/DraggableStoppedCallback")]
internal sealed class DraggableStoppedCallback : Java.Lang.Object, IFunction3
{
    readonly Action _callback;

    public DraggableStoppedCallback(Action callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        _callback = callback;
    }

    public Java.Lang.Object? Invoke(
        Java.Lang.Object? coroutineScope,
        Java.Lang.Object? velocity,
        Java.Lang.Object? continuation)
    {
        try
        {
            _callback();
            return Kotlin.Unit.Instance
                ?? throw new InvalidOperationException("Kotlin Unit singleton was unavailable.");
        }
        finally
        {
            GC.KeepAlive(coroutineScope);
            GC.KeepAlive(velocity);
            GC.KeepAlive(continuation);
        }
    }
}
