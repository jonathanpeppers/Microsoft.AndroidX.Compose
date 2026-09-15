using Android.Runtime;
using AndroidX.Compose.Runtime;
using AndroidX.Compose.UI;
using AndroidX.Compose.UI.Input.Pointer;
using Kotlin.Jvm.Functions;

namespace AndroidX.Compose;

// Unlike Unit-returning composable slots, Modifier.composed's factory returns the materialized modifier.
[Register("net/compose/PointerInputModifierFactory")]
internal sealed class PointerInputModifierFactory(LongPressDragCallbacks callbacks, object? key)
    : Java.Lang.Object, IFunction3
{
    public Java.Lang.Object Invoke(Java.Lang.Object? receiver, Java.Lang.Object? composer, Java.Lang.Object? changed)
    {
        var current = composer?.JavaCast<IComposer>()
            ?? throw new InvalidOperationException("PointerInputModifierFactory requires a composer.");
        var modifier = receiver?.JavaCast<IModifier>()
            ?? throw new InvalidOperationException("PointerInputModifierFactory requires a Modifier receiver.");
        var block = current.Remember(() => new LongPressDragGestureBlock(callbacks), key);
        // A failed/speculative composition must not overwrite an installed handler's delegates.
        current.SideEffect(() => block.Callbacks = callbacks);
        var nativeKey = current.Remember(() => ModifierExtensions.BoxPointerInputKey(key), key);
        return (Java.Lang.Object)SuspendingPointerInputFilterKt.PointerInput(modifier, nativeKey, block);
    }
}
