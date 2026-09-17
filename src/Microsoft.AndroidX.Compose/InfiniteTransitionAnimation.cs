using AndroidX.Compose.Runtime;

namespace AndroidX.Compose;

internal sealed class InfiniteTransitionAnimation(IState jvm) : IState<float>
{
    internal IState Jvm { get; } = jvm;

    public float Value => Jvm.Value is Java.Lang.Float number
        ? number.FloatValue()
        : throw new InvalidCastException("Infinite float animation did not return java.lang.Float.");
}
