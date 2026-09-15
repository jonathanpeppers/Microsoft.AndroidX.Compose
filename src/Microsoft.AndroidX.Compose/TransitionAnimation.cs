using AndroidX.Compose.Runtime;
using Kotlin.Jvm.Functions;

namespace AndroidX.Compose;

internal sealed class TransitionAnimation<T>(
    IState jvm,
    Func<Java.Lang.Object?, T> unbox,
    IFunction3 targetCallback,
    IFunction3 specCallback) : IState<T>
{
    internal IState Jvm { get; } = jvm;
    internal IFunction3 TargetCallback { get; } = targetCallback;
    internal IFunction3 SpecCallback { get; } = specCallback;

    public T Value => unbox(Jvm.Value);
}
