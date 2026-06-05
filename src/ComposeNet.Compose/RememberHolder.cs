using Android.Runtime;

namespace ComposeNet;

/// <summary>
/// Single-field <see cref="Java.Lang.Object"/> wrapper that lets
/// <see cref="Compose.Remember{T}"/> store an arbitrary managed
/// <c>T</c> (often a <see cref="MutableState{T}"/>) in Compose's slot
/// table, which only holds <see cref="Java.Lang.Object"/> references.
///
/// Registered with the .NET-for-Android peer cache (<c>[Register]</c>
/// + the IntPtr/JniHandleOwnership ctor) so that when Compose hands
/// the slot value back through <see cref="AndroidX.Compose.Runtime.IComposer.RememberedValue"/>
/// on a recomposition, the runtime rehydrates the same managed
/// <c>RememberHolder</c> peer rather than a plain
/// <see cref="Java.Lang.Object"/> shell — which would lose the
/// <see cref="Value"/> field and force the factory to re-run.
/// </summary>
[Register("composenet/compose/RememberHolder")]
internal sealed class RememberHolder : Java.Lang.Object
{
    /// <summary>The remembered managed value (boxed if a value type).</summary>
    public object? Value;

    public RememberHolder() { }

    public RememberHolder(object? value) => Value = value;

    // Peer-rehydration ctor: called by the .NET-for-Android runtime when
    // a JNI handle for our [Register]'d class crosses back into managed
    // code and its managed peer needs to be (re)created. We do NOT touch
    // Value here — the original instance's Value field stays attached to
    // whichever peer the runtime returns from its peer cache.
    internal RememberHolder(IntPtr handle, JniHandleOwnership transfer)
        : base(handle, transfer) { }
}
