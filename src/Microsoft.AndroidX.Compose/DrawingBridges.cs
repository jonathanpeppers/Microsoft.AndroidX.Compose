using Kotlin.Jvm.Functions;

namespace AndroidX.Compose;

internal static partial class ComposeBridges
{
    [ComposeBridge(
        Instance = true,
        Class = "androidx/compose/ui/draw/CacheDrawScope",
        JvmName = "getSize-NH-jbRc",
        Signature = "()J")]
    internal static partial long CacheDrawScopeSize(IntPtr scope);

    [ComposeBridge(
        Instance = true,
        Class = "androidx/compose/ui/draw/CacheDrawScope",
        JvmName = "onDrawBehind",
        Signature = "(Lkotlin/jvm/functions/Function1;)Landroidx/compose/ui/draw/DrawResult;")]
    internal static partial IntPtr CacheDrawScopeOnDrawBehind(
        IntPtr scope,
        IFunction1 draw);

    [ComposeBridge(
        Instance = true,
        Class = "androidx/compose/ui/draw/CacheDrawScope",
        JvmName = "onDrawWithContent",
        Signature = "(Lkotlin/jvm/functions/Function1;)Landroidx/compose/ui/draw/DrawResult;")]
    internal static partial IntPtr CacheDrawScopeOnDrawWithContent(
        IntPtr scope,
        IFunction1 draw);
}
