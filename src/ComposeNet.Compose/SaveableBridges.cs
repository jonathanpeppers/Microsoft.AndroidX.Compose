using Android.Runtime;

namespace ComposeNet;

// Hand-written raw-JNI bridges for the
// `androidx.compose.runtime.saveable` package. The Xamarin
// NuGet (`Xamarin.AndroidX.Compose.Runtime.Saveable.Android`) ships
// the Java library but does not generate C# bindings, so every
// Saveable entry point we use here goes through `JNIEnv.*` calls.
// Same model as `SuspendBridges.cs`.
//
// When/if `dotnet/android-libraries` adds bindings for this package,
// these methods can be replaced with direct binding calls and this
// file can shrink to whatever is still mangled or unbound.
internal static partial class ComposeBridges
{
    static System.IntPtr s_autoSaver_class;
    static System.IntPtr s_autoSaver_method;
    static System.IntPtr s_autoSaver_handle;

    static System.IntPtr s_rememberSaveableSimple_class;
    static System.IntPtr s_rememberSaveableSimple_method;

    static System.IntPtr s_rememberSaveableState_class;
    static System.IntPtr s_rememberSaveableState_method;

    static System.IntPtr s_emptyObjectArray;

    // androidx.compose.runtime.saveable.SaverKt.autoSaver(): Saver<T, Object>
    //
    // The default `Saver` Compose uses for any `Bundle`-saveable value
    // (primitives, strings, parcelables, …). Cached as a global ref —
    // returns the same singleton on every call from Kotlin, and we
    // need it as the `stateSaver` argument for the MutableState-overload
    // of `rememberSaveable`.
    internal static System.IntPtr SaverAutoSaver()
    {
        if (s_autoSaver_handle != System.IntPtr.Zero)
            return s_autoSaver_handle;

        if (s_autoSaver_method == System.IntPtr.Zero)
        {
            s_autoSaver_class = JNIEnv.FindClass("androidx/compose/runtime/saveable/SaverKt");
            s_autoSaver_method = JNIEnv.GetStaticMethodID(
                s_autoSaver_class,
                "autoSaver",
                "()Landroidx/compose/runtime/saveable/Saver;");
        }

        var local = JNIEnv.CallStaticObjectMethod(s_autoSaver_class, s_autoSaver_method);
        try
        {
            s_autoSaver_handle = JNIEnv.NewGlobalRef(local);
        }
        finally
        {
            JNIEnv.DeleteLocalRef(local);
        }
        return s_autoSaver_handle;
    }

    // Lazily-cached empty `Object[]` for the `vararg inputs` parameter
    // of `rememberSaveable`. Compose treats inputs as "dependency
    // tracking" — if any element changes, the cached factory result
    // is discarded. We always pass an empty array (= "never invalidate
    // on input change"); future overloads with `params object?[]`
    // can build the array on demand.
    internal static System.IntPtr EmptyObjectArray()
    {
        if (s_emptyObjectArray != System.IntPtr.Zero)
            return s_emptyObjectArray;

        var objectClass = Java.Lang.Class.FromType(typeof(Java.Lang.Object));
        var local = JNIEnv.NewObjectArray(0, objectClass.Handle, System.IntPtr.Zero);
        try
        {
            s_emptyObjectArray = JNIEnv.NewGlobalRef(local);
        }
        finally
        {
            JNIEnv.DeleteLocalRef(local);
        }
        return s_emptyObjectArray;
    }

    // androidx.compose.runtime.saveable.RememberSaveableKt.rememberSaveable(
    //     Object[] inputs,
    //     Function0<? extends T> init,
    //     Composer composer,
    //     int $changed): Object
    //
    // The no-saver overload — Compose internally uses `autoSaver()`,
    // which handles every Bundle-saveable value (primitives, strings,
    // parcelables). Returns a JNI local ref to the boxed result.
    internal static unsafe System.IntPtr RememberSaveableSimple(
        System.IntPtr inputs,
        ObjectFunction0 init,
        System.IntPtr composer,
        int changed)
    {
        if (s_rememberSaveableSimple_method == System.IntPtr.Zero)
        {
            s_rememberSaveableSimple_class = JNIEnv.FindClass(
                "androidx/compose/runtime/saveable/RememberSaveableKt");
            s_rememberSaveableSimple_method = JNIEnv.GetStaticMethodID(
                s_rememberSaveableSimple_class,
                "rememberSaveable",
                "([Ljava/lang/Object;Lkotlin/jvm/functions/Function0;Landroidx/compose/runtime/Composer;I)Ljava/lang/Object;");
        }

        try
        {
            JValue* args = stackalloc JValue[4];
            args[0] = new JValue(inputs);
            args[1] = new JValue(init.Handle);
            args[2] = new JValue(composer);
            args[3] = new JValue(changed);
            return JNIEnv.CallStaticObjectMethod(
                s_rememberSaveableSimple_class,
                s_rememberSaveableSimple_method,
                args);
        }
        finally
        {
            System.GC.KeepAlive(init);
        }
    }

    // androidx.compose.runtime.saveable.RememberSaveableKt.rememberSaveable(
    //     Object[] inputs,
    //     Saver<T, ?> stateSaver,
    //     Function0<? extends MutableState<T>> init,
    //     Composer composer,
    //     int $changed): MutableState<T>
    //
    // The MutableState overload — Compose internally wraps
    // `stateSaver` with `mutableStateSaver(stateSaver)` and persists
    // only the inner value. On restore Compose calls
    // `mutableStateOf(restoredValue, policy)` to rebuild the state
    // wrapper, so the returned `IMutableState` is *not* guaranteed
    // to be a primitive-specialized `IMutableIntState`/etc. — it's
    // the boxed default unless the caller supplied a custom saver
    // that re-creates the specialized type.
    internal static unsafe System.IntPtr RememberSaveableMutableState(
        System.IntPtr inputs,
        System.IntPtr stateSaver,
        ObjectFunction0 init,
        System.IntPtr composer,
        int changed)
    {
        if (s_rememberSaveableState_method == System.IntPtr.Zero)
        {
            s_rememberSaveableState_class = JNIEnv.FindClass(
                "androidx/compose/runtime/saveable/RememberSaveableKt");
            s_rememberSaveableState_method = JNIEnv.GetStaticMethodID(
                s_rememberSaveableState_class,
                "rememberSaveable",
                "([Ljava/lang/Object;Landroidx/compose/runtime/saveable/Saver;Lkotlin/jvm/functions/Function0;Landroidx/compose/runtime/Composer;I)Landroidx/compose/runtime/MutableState;");
        }

        try
        {
            JValue* args = stackalloc JValue[5];
            args[0] = new JValue(inputs);
            args[1] = new JValue(stateSaver);
            args[2] = new JValue(init.Handle);
            args[3] = new JValue(composer);
            args[4] = new JValue(changed);
            return JNIEnv.CallStaticObjectMethod(
                s_rememberSaveableState_class,
                s_rememberSaveableState_method,
                args);
        }
        finally
        {
            System.GC.KeepAlive(init);
        }
    }

    // Builds a Kotlin-compatible `Object[]` from a managed `object?[]`
    // for the `inputs` (vararg) parameter of `rememberSaveable`. Each
    // element is boxed via the same primitive→Java.Lang.Object switch
    // MutableState<T> uses, so primitives compare structurally rather
    // than by reference. Returns a JNI local ref the caller MUST free
    // (call DeleteLocalRef inside a finally). Also creates local refs
    // for each boxed element; we delete each one immediately after
    // setting it into the array (the array owns its own ref).
    //
    // Null/empty keys array → `EmptyObjectArray()`, a cached global
    // ref — `ownsHandle` is false, signalling the caller MUST NOT
    // delete the returned handle.
    internal static System.IntPtr BuildKeysArray(object?[]? keys, out bool ownsHandle)
    {
        if (keys is null || keys.Length == 0)
        {
            ownsHandle = false;
            return EmptyObjectArray();
        }

        ownsHandle = true;
        var objectClass = Java.Lang.Class.FromType(typeof(Java.Lang.Object));
        var array = JNIEnv.NewObjectArray(keys.Length, objectClass.Handle, System.IntPtr.Zero);
        for (int i = 0; i < keys.Length; i++)
        {
            var boxed = BoxKey(keys[i]);
            if (boxed is null)
            {
                JNIEnv.SetObjectArrayElement(array, i, System.IntPtr.Zero);
                continue;
            }
            JNIEnv.SetObjectArrayElement(array, i, boxed.Handle);
            // Disposing releases the local ref the boxing constructor
            // created; the array now holds its own ref to the element.
            boxed.Dispose();
        }
        return array;
    }

    static Java.Lang.Object? BoxKey(object? key) => key switch
    {
        null               => null,
        Java.Lang.Object o => o,
        string s           => new Java.Lang.String(s),
        bool b             => Java.Lang.Boolean.ValueOf(b),
        char c             => Java.Lang.Character.ValueOf(c),
        sbyte sb           => Java.Lang.Byte.ValueOf(sb),
        byte by            => Java.Lang.Short.ValueOf((short)by),
        short sh           => Java.Lang.Short.ValueOf(sh),
        ushort us          => Java.Lang.Integer.ValueOf(us),
        int i              => Java.Lang.Integer.ValueOf(i),
        uint ui            => Java.Lang.Long.ValueOf(ui),
        long l             => Java.Lang.Long.ValueOf(l),
        ulong ul           => Java.Lang.Long.ValueOf(unchecked((long)ul)),
        float f            => Java.Lang.Float.ValueOf(f),
        double d           => Java.Lang.Double.ValueOf(d),
        _                  => new Java.Lang.String(key.ToString() ?? string.Empty),
    };
}
