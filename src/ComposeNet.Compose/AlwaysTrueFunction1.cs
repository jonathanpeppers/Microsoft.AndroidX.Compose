using Android.Runtime;
using Kotlin.Jvm.Functions;

namespace ComposeNet;

/// <summary>
/// <c>Function1</c> stub that ignores its argument and returns
/// <see cref="Java.Lang.Boolean.True"/>. Used as a default
/// <c>confirmStateChange</c> callback when constructing
/// <c>DrawerState</c> / similar Compose state objects from C# —
/// passing a real lambda avoids relying on Kotlin's <c>$default</c>
/// substitution for the lambda parameter, which doesn't always work
/// through the .NET binding.
/// </summary>
[Register("composenet/compose/AlwaysTrueFunction1")]
internal sealed class AlwaysTrueFunction1 : Java.Lang.Object, IFunction1
{
    public static readonly AlwaysTrueFunction1 Instance = new();

    public Java.Lang.Object? Invoke(Java.Lang.Object? p0) => Java.Lang.Boolean.True;
}
