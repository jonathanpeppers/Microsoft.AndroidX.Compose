using Android.Runtime;

namespace AndroidX.Compose;

/// <summary>
/// Single-field <see cref="Java.Lang.Object"/> wrapper that lets
/// <see cref="ComposeExtensions.DiffSlot{T}"/>
/// stash a previous-render value (boxed when <c>T</c> is a value type)
/// in Compose's slot table, which only holds
/// <see cref="Java.Lang.Object"/> references. Registered with the
/// .NET-for-Android peer cache so the runtime can rehydrate the same
/// managed peer across recompositions and the <see cref="Value"/>
/// field stays attached to it.
///
/// <para>Tracks <see cref="HasValue"/> separately so a slot that goes
/// from <c>null</c> to <c>null</c> reads as <see cref="ChangedBits.Same"/>
/// (default-boxed value types like <c>0</c> would otherwise be
/// indistinguishable from "no prior render" — same boxed identity but
/// semantically different).</para>
/// </summary>
[Register("net/compose/DiffSlotHolder")]
internal sealed class DiffSlotHolder : Java.Lang.Object
{
    /// <summary>Last-render value at this slot (boxed if a value type).</summary>
    public object? Value;

    /// <summary>
    /// Whether <see cref="Value"/> represents a non-<c>null</c> caller
    /// value (vs the slot's never-written initial state, where
    /// <c>Value</c> is the <c>default</c> box). Lets DiffSlot detect
    /// "first render" without a separate sentinel object.
    /// </summary>
    public bool HasValue;

    public DiffSlotHolder() { }

    public DiffSlotHolder(object? value, bool hasValue)
    {
        Value = value;
        HasValue = hasValue;
    }

    internal DiffSlotHolder(IntPtr handle, JniHandleOwnership transfer)
        : base(handle, transfer) { }
}
