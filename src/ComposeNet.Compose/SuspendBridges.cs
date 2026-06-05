using Android.Runtime;

namespace ComposeNet;

// Hand-written JNI bridges for Kotlin `suspend` functions that the
// `[ComposeBridge]` source generator doesn't yet model. v1 ships just
// the ScrollState entries (`scrollTo` is already exposed via the
// regular C# binding, so only `animateScrollTo` needs a JNI plumb).
//
// When this file grows past ~3 entries, formalise into a
// `ComposeBridgeAttribute(Suspend = true)` generator path — see issue
// #96 design notes.
internal static partial class ComposeBridges
{
    static System.IntPtr s_scrollStateAnimateScrollTo_class;
    static System.IntPtr s_scrollStateAnimateScrollTo_method;

    // androidx.compose.foundation.ScrollState.animateScrollTo(
    //     int value, AnimationSpec<Float> = SpringSpec(), Continuation): Object
    //
    // The two-arg overload (no `animationSpec`) is stripped from the
    // binding, so we invoke the synthetic `$default` overload directly:
    //
    //   animateScrollTo$default(
    //       ScrollState this, int value,
    //       AnimationSpec spec, Continuation cont,
    //       int $default, Object marker): Object
    //
    // Mask = 0b010 (bit 1) → "use Kotlin default for animationSpec".
    // Marker is always null at every call site (synthetic-overload
    // requirement). Returns kotlin/Unit on success or COROUTINE_SUSPENDED.
    internal static unsafe Java.Lang.Object? ScrollStateAnimateScrollTo(
        System.IntPtr state, int value, SuspendContinuation cont)
    {
        if (s_scrollStateAnimateScrollTo_method == System.IntPtr.Zero)
        {
            // FindClass returns a *local* JNI ref; promote to a global
            // ref before caching so the cached handle stays valid past
            // the current JNI frame. Matches the ModifierCompanionInstance
            // pattern in ComposeBridges.cs.
            var local = JNIEnv.FindClass("androidx/compose/foundation/ScrollState");
            s_scrollStateAnimateScrollTo_class = JNIEnv.NewGlobalRef(local);
            JNIEnv.DeleteLocalRef(local);
            s_scrollStateAnimateScrollTo_method = JNIEnv.GetStaticMethodID(
                s_scrollStateAnimateScrollTo_class,
                "animateScrollTo$default",
                "(Landroidx/compose/foundation/ScrollState;I" +
                "Landroidx/compose/animation/core/AnimationSpec;" +
                "Lkotlin/coroutines/Continuation;ILjava/lang/Object;)Ljava/lang/Object;");
        }

        try
        {
            JValue* args = stackalloc JValue[6];
            args[0] = new JValue(state);
            args[1] = new JValue(value);
            args[2] = new JValue(System.IntPtr.Zero);   // AnimationSpec — defaulted
            args[3] = new JValue(cont.Handle);
            args[4] = new JValue(0b010);                // $default mask: bit 1 = animationSpec
            args[5] = new JValue(System.IntPtr.Zero);   // synthetic marker — always null
            var handle = JNIEnv.CallStaticObjectMethod(
                s_scrollStateAnimateScrollTo_class,
                s_scrollStateAnimateScrollTo_method,
                args);
            return handle == System.IntPtr.Zero
                ? null
                : Java.Lang.Object.GetObject<Java.Lang.Object>(handle, JniHandleOwnership.TransferLocalRef);
        }
        finally
        {
            System.GC.KeepAlive(cont);
        }
    }
}
