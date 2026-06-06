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
    static System.IntPtr s_scrollStateScrollTo_class;
    static System.IntPtr s_scrollStateScrollTo_method;
    static System.IntPtr s_scrollStateAnimateScrollTo_class;
    static System.IntPtr s_scrollStateAnimateScrollTo_method;
    static System.IntPtr s_androidUiDispatcherMain_handle;

    // androidx.compose.foundation.ScrollState.scrollTo(int value, Continuation): Object
    //
    // The two-arg suspend overload is exposed in the binding (returns
    // Java.Lang.Object), but we call it via raw JNI here so the
    // returned handle stays as a plain IntPtr — SuspendBridge needs
    // raw-handle semantics to avoid the peer-cache pitfall around
    // COROUTINE_SUSPENDED.
    internal static unsafe System.IntPtr ScrollStateScrollTo(
        System.IntPtr state, int value, SuspendContinuation cont)
    {
        if (s_scrollStateScrollTo_method == System.IntPtr.Zero)
        {
            // JNIEnv.FindClass in Mono.Android already returns a stable,
            // globally-registered class ref — store it directly. Calling
            // NewGlobalRef + DeleteLocalRef on top would fire CheckJNI's
            // "expected Local but found Global" abort. Matches the
            // pattern emitted by ComposeBridgeGenerator.
            s_scrollStateScrollTo_class = JNIEnv.FindClass("androidx/compose/foundation/ScrollState");
            s_scrollStateScrollTo_method = JNIEnv.GetMethodID(
                s_scrollStateScrollTo_class,
                "scrollTo",
                "(ILkotlin/coroutines/Continuation;)Ljava/lang/Object;");
        }

        try
        {
            JValue* args = stackalloc JValue[2];
            args[0] = new JValue(value);
            args[1] = new JValue(cont.Handle);
            return JNIEnv.CallObjectMethod(state, s_scrollStateScrollTo_method, args);
        }
        finally
        {
            System.GC.KeepAlive(cont);
        }
    }

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
    //
    // Why raw JNI: animateScrollTo lives in the Kotlin extension class
    // `androidx/compose/foundation/gestures/AnimateScrollExtensionsKt`,
    // which is stripped wholesale from `Xamarin.AndroidX.Compose.Foundation.Android.dll`
    // (zero `*Animate*` types in that assembly). The cause is the
    // `AnimationSpec<Float>` parameter — Kotlin's generic + `@JvmInline`
    // mangling produces a JVM name (`animateScrollTo-XXXXXXXX`) the
    // generator-bindings parser drops, and the parameterless-default
    // synthetic gets dropped along with it. Same root cause as the
    // mangled Compose Material3 overloads tracked by
    // dotnet/java-interop#1440 — once that lands and the upstream
    // Compose binding is regenerated, this bridge can be replaced with
    // a clean call through the bound `AnimateScrollExtensionsKt`.
    internal static unsafe System.IntPtr ScrollStateAnimateScrollTo(
        System.IntPtr state, int value, SuspendContinuation cont)
    {
        if (s_scrollStateAnimateScrollTo_method == System.IntPtr.Zero)
        {
            // JNIEnv.FindClass in Mono.Android returns a stable, globally
            // registered class ref — store it directly. See
            // ScrollStateScrollTo for the rationale.
            s_scrollStateAnimateScrollTo_class = JNIEnv.FindClass("androidx/compose/foundation/ScrollState");
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
            return JNIEnv.CallStaticObjectMethod(
                s_scrollStateAnimateScrollTo_class,
                s_scrollStateAnimateScrollTo_method,
                args);
        }
        finally
        {
            System.GC.KeepAlive(cont);
        }
    }

    // androidx.compose.ui.platform.AndroidUiDispatcher.Companion.getMain(): kotlin.coroutines.CoroutineContext
    //
    // Returns the singleton CoroutineContext that combines a main-thread
    // dispatcher with a MonotonicFrameClock backed by Choreographer. Required
    // for any Compose suspend function that calls `withFrameNanos` —
    // `animateScrollTo`, `animateTo`, etc. — because those look up
    // `coroutineContext[MonotonicFrameClock]` and throw without it.
    //
    // The C# binding exposes AndroidUiDispatcher.Companion as a nested type
    // with an internal constructor and no static accessor on the parent —
    // so we resolve Main via raw JNI and cache the result as a global ref.
    // Must be initialised on a Looper thread the first time, which is
    // naturally satisfied because Compose suspend calls originate from
    // button onClicks running on the main thread.
    internal static System.IntPtr AndroidUiDispatcherMain()
    {
        if (s_androidUiDispatcherMain_handle != System.IntPtr.Zero)
            return s_androidUiDispatcherMain_handle;

        var dispatcherClass = JNIEnv.FindClass("androidx/compose/ui/platform/AndroidUiDispatcher");
        var companionFid = JNIEnv.GetStaticFieldID(
            dispatcherClass, "Companion",
            "Landroidx/compose/ui/platform/AndroidUiDispatcher$Companion;");
        var companionLocal = JNIEnv.GetStaticObjectField(dispatcherClass, companionFid);
        try
        {
            var companionClass = JNIEnv.FindClass("androidx/compose/ui/platform/AndroidUiDispatcher$Companion");
            var getMainMid = JNIEnv.GetMethodID(
                companionClass, "getMain", "()Lkotlin/coroutines/CoroutineContext;");
            var ctxLocal = JNIEnv.CallObjectMethod(companionLocal, getMainMid);
            try
            {
                s_androidUiDispatcherMain_handle = JNIEnv.NewGlobalRef(ctxLocal);
            }
            finally
            {
                JNIEnv.DeleteLocalRef(ctxLocal);
            }
        }
        finally
        {
            JNIEnv.DeleteLocalRef(companionLocal);
        }

        return s_androidUiDispatcherMain_handle;
    }
}
