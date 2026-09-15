using Android.Runtime;
using AndroidX.Compose.UI.Input.Pointer;
using Kotlin.Coroutines;
using Kotlin.Coroutines.Intrinsics;

namespace AndroidX.Compose;

// The native pointer-input job owns cancellation; forward its continuation without a managed Task.
[Register("net/compose/LongPressDragGestureBlock")]
internal sealed class LongPressDragGestureBlock : Java.Lang.Object, IPointerInputEventHandler
{
    internal LongPressDragCallbacks Callbacks { get; set; }
    internal OffsetCallback Start { get; }
    internal UnitCallback End { get; }
    internal UnitCallback Cancel { get; }
    internal DragCallback Drag { get; }

    internal LongPressDragGestureBlock(LongPressDragCallbacks callbacks)
    {
        Callbacks = callbacks;
        Start = new OffsetCallback(position => Callbacks.OnDragStart?.Invoke(position));
        End = new UnitCallback(() => Callbacks.OnDragEnd?.Invoke());
        Cancel = new UnitCallback(() => Callbacks.OnDragCancel?.Invoke());
        Drag = new DragCallback(delta => Callbacks.OnDrag(delta));
    }

    public Java.Lang.Object? Invoke(IPointerInputScope scope, IContinuation continuation)
    {
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(continuation);
        var result = IntPtr.Zero;
        try
        {
            result = ComposeBridges.DetectDragGesturesAfterLongPress(
                ((Java.Lang.Object)scope).Handle, Start, End, Cancel, Drag, continuation);
            if (SuspendBridge.IsCoroutineSuspended(result))
                return IntrinsicsKt.COROUTINE_SUSPENDED;
            return Java.Lang.Object.GetObject<Java.Lang.Object>(result, JniHandleOwnership.DoNotTransfer);
        }
        finally
        {
            if (result != IntPtr.Zero)
                JNIEnv.DeleteLocalRef(result);
            GC.KeepAlive(scope);
        }
    }
}
