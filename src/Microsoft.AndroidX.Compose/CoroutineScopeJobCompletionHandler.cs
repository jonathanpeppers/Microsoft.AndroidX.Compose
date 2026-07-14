using System.Runtime.InteropServices;
using Android.Runtime;
using Kotlin.Jvm.Functions;

namespace AndroidX.Compose;

/// <summary>
/// Observes cancellation of the Kotlin child job returned by
/// <c>CoroutineScope.launch</c>.
/// </summary>
[Register("net/compose/CoroutineScopeJobCompletionHandler")]
internal sealed class CoroutineScopeJobCompletionHandler : Java.Lang.Object, IFunction1
{
    readonly CoroutineScopeLaunchBody _operation;
    GCHandle _selfPin;

    public CoroutineScopeJobCompletionHandler(CoroutineScopeLaunchBody operation)
    {
        _operation = operation;
        _selfPin = GCHandle.Alloc(this);
    }

    public Java.Lang.Object Invoke(Java.Lang.Object? p0)
    {
        try
        {
            if (p0 is not null)
                _operation.CancelFromScope();
            return Kotlin.Unit.Instance
                ?? throw new InvalidOperationException(
                    "Kotlin.Unit.Instance was not available.");
        }
        finally
        {
            Release();
        }
    }

    internal void Release()
    {
        if (_selfPin.IsAllocated)
            _selfPin.Free();
    }
}
