using Android.Runtime;
using Kotlin.Jvm.Functions;

namespace AndroidX.Compose;

/// <summary>
/// Identity-stable variant of <see cref="ComposableLambda0"/> for use
/// with <see cref="ComposeExtensions.RememberAction(AndroidX.Compose.Runtime.IComposer, Action, int, string)"/>.
/// The wrapper is allocated once per call site and cached in the
/// composer's slot table; subsequent renders rebind <see cref="Target"/>
/// instead of allocating a fresh JCW. Keeps the
/// <c>Function0&lt;Unit&gt;</c> JNI peer reference-stable so
/// <c>$changed</c> can read <see cref="ChangedBits.Static"/> for
/// <c>onClick</c>-style ctor params and Kotlin's
/// <c>remember(key)</c>-keyed callbacks (sheet/drawer
/// <c>confirmStateChange</c>, etc.) don't drop their cached state.
/// </summary>
[Register("net/compose/MutableComposableLambda0")]
internal sealed class MutableComposableLambda0 : Java.Lang.Object, IFunction0
{
    /// <summary>
    /// Mutable target — caller rebinds it on every render. Read inside
    /// <see cref="Invoke"/> (not at construction) so updating it
    /// between renders takes effect immediately without re-creating
    /// the Java peer.
    /// </summary>
    public Action Target { get; set; }

    public MutableComposableLambda0(Action target)
    {
        Target = target ?? throw new ArgumentNullException(nameof(target));
    }

    // Kotlin Function0<Unit> contractually returns Unit.INSTANCE — see
    // ComposableLambda0 / issue #43.
    public Java.Lang.Object Invoke()
    {
        Target();
        return Kotlin.Unit.Instance!;
    }
}
