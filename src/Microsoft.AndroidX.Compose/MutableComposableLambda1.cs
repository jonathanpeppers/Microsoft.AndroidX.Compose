using Android.Runtime;
using Kotlin.Jvm.Functions;

namespace AndroidX.Compose;

/// <summary>
/// Identity-stable variant of <see cref="ComposableLambda1"/> for use
/// with <see cref="ComposeExtensions.RememberAction(AndroidX.Compose.Runtime.IComposer, Action{Java.Lang.Object?}, int, string)"/>.
/// The wrapper is allocated once per call site and cached in the
/// composer's slot table; subsequent renders rebind <see cref="Target"/>
/// instead of allocating a fresh JCW. Keeps the
/// <c>Function1&lt;T, Unit&gt;</c> JNI peer reference-stable so
/// <c>$changed</c> can read <see cref="ChangedBits.Static"/> for
/// <c>onValueChange</c>/<c>onCheckedChange</c>-style ctor params.
/// </summary>
[Register("net/compose/MutableComposableLambda1")]
internal sealed class MutableComposableLambda1 : Java.Lang.Object, IFunction1
{
    /// <summary>
    /// Mutable target — caller rebinds it on every render. Read inside
    /// <see cref="Invoke"/> (not at construction) so updating it
    /// between renders takes effect immediately without re-creating
    /// the Java peer.
    /// </summary>
    public Action<Java.Lang.Object?> Target { get; set; }

    public MutableComposableLambda1(Action<Java.Lang.Object?> target)
    {
        Target = target ?? throw new ArgumentNullException(nameof(target));
    }

    // Kotlin Function1<T, Unit> contractually returns Unit.INSTANCE — see
    // ComposableLambda1 / issue #43.
    public Java.Lang.Object Invoke(Java.Lang.Object? p0)
    {
        Target(p0);
        return Kotlin.Unit.Instance!;
    }
}
