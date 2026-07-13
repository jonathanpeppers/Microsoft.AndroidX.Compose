using Android.Runtime;
using AndroidX.Compose.UI.Draw;
using Kotlin.Jvm.Functions;

namespace AndroidX.Compose;

internal static partial class ComposeBridges
{
    static IntPtr s_cacheDrawScopeClass;
    static IntPtr s_cacheDrawScopeSize;
    static IntPtr s_cacheDrawScopeOnDrawBehind;
    static IntPtr s_cacheDrawScopeOnDrawWithContent;

    // Why raw JNI: CacheDrawScope is omitted from the runtime binding. These
    // are instance methods, a non-suspend shape the bridge generator does not
    // currently model; DrawModifierKt entrypoints remain generated bridges.
    internal static long CacheDrawScopeSize(IntPtr scope)
    {
        ResolveCacheDrawScope();
        return JNIEnv.CallLongMethod(scope, s_cacheDrawScopeSize);
    }

    internal static unsafe DrawResult CacheDrawScopeOnDrawBehind(
        IntPtr scope,
        IFunction1 draw)
    {
        ResolveCacheDrawScope();
        return CallCacheDrawScope(scope, s_cacheDrawScopeOnDrawBehind, draw);
    }

    internal static unsafe DrawResult CacheDrawScopeOnDrawWithContent(
        IntPtr scope,
        IFunction1 draw)
    {
        ResolveCacheDrawScope();
        return CallCacheDrawScope(scope, s_cacheDrawScopeOnDrawWithContent, draw);
    }

    static void ResolveCacheDrawScope()
    {
        if (s_cacheDrawScopeClass != IntPtr.Zero)
            return;

        s_cacheDrawScopeClass = JNIEnv.FindClass("androidx/compose/ui/draw/CacheDrawScope");
        s_cacheDrawScopeSize = JNIEnv.GetMethodID(
            s_cacheDrawScopeClass, "getSize-NH-jbRc", "()J");
        s_cacheDrawScopeOnDrawBehind = JNIEnv.GetMethodID(
            s_cacheDrawScopeClass, "onDrawBehind",
            "(Lkotlin/jvm/functions/Function1;)Landroidx/compose/ui/draw/DrawResult;");
        s_cacheDrawScopeOnDrawWithContent = JNIEnv.GetMethodID(
            s_cacheDrawScopeClass, "onDrawWithContent",
            "(Lkotlin/jvm/functions/Function1;)Landroidx/compose/ui/draw/DrawResult;");
    }

    static unsafe DrawResult CallCacheDrawScope(
        IntPtr scope,
        IntPtr method,
        IFunction1 draw)
    {
        IntPtr local = IntPtr.Zero;
        try
        {
            var args = stackalloc JValue[1];
            args[0] = new JValue(((Java.Lang.Object)draw).Handle);
            local = JNIEnv.CallObjectMethod(scope, method, args);
            var result = Java.Lang.Object.GetObject<DrawResult>(
                local, JniHandleOwnership.TransferLocalRef);
            local = IntPtr.Zero;
            return result ?? throw new InvalidOperationException(
                "CacheDrawScope returned a null DrawResult.");
        }
        finally
        {
            if (local != IntPtr.Zero)
                JNIEnv.DeleteLocalRef(local);
            GC.KeepAlive(draw);
        }
    }
}
