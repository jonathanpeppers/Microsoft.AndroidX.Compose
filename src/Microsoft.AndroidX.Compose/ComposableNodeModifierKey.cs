namespace AndroidX.Compose;

/// <summary>
/// Composite structural fingerprint of one
/// <see cref="ComposableNode"/>'s modifier slot — folds the four
/// contributions consumed by
/// <see cref="ComposableNode.BuildModifier"/> (the runtime content-
/// padding handle, the prepended/appended side-channels, and the
/// node's own <see cref="ComposableNode.Modifier"/>) into a single
/// value-equatable key. Wraps the four parts so
/// <see cref="EqualityComparer{T}.Default"/> compares them
/// element-wise rather than falling back to reference equality on
/// the raw <see cref="ModifierStructuralKey"/> arrays.
/// </summary>
/// <remarks>
/// Order mirrors <see cref="ComposableNode.BuildModifier"/>:
/// <c>contentPadding → prepended → modifier → appended</c>.
/// When all four components are absent (no padding, no prepend, no
/// modifier, no append) two keys with that shape compare equal —
/// the common case for thousands of leaf facades, and the whole
/// reason the modifier slot can read <see cref="ChangedBits.Same"/>
/// after the first render.
/// </remarks>
internal readonly struct ComposableNodeModifierKey : IEquatable<ComposableNodeModifierKey>
{
    readonly IntPtr _contentPadding;
    readonly ModifierStructuralKey _prepended;
    readonly ModifierStructuralKey _modifier;
    readonly ModifierStructuralKey _appended;
    readonly bool _hasPrepended;
    readonly bool _hasModifier;
    readonly bool _hasAppended;

    internal ComposableNodeModifierKey(
        IntPtr contentPadding,
        Modifier? prepended,
        Modifier? modifier,
        Modifier? appended)
    {
        _contentPadding = contentPadding;
        _hasPrepended = prepended is not null;
        _hasModifier  = modifier  is not null;
        _hasAppended  = appended  is not null;
        _prepended = prepended is null ? default : prepended.StructuralKey;
        _modifier  = modifier  is null ? default : modifier.StructuralKey;
        _appended  = appended  is null ? default : appended.StructuralKey;
    }

    /// <inheritdoc/>
    public bool Equals(ComposableNodeModifierKey other)
    {
        if (_contentPadding != other._contentPadding) return false;
        if (_hasPrepended != other._hasPrepended) return false;
        if (_hasModifier  != other._hasModifier)  return false;
        if (_hasAppended  != other._hasAppended)  return false;
        if (_hasPrepended && !_prepended.Equals(other._prepended)) return false;
        if (_hasModifier  && !_modifier.Equals(other._modifier))   return false;
        if (_hasAppended  && !_appended.Equals(other._appended))   return false;
        return true;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is ComposableNodeModifierKey k && Equals(k);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(_contentPadding);
        hash.Add(_hasPrepended);
        hash.Add(_hasModifier);
        hash.Add(_hasAppended);
        if (_hasPrepended) hash.Add(_prepended);
        if (_hasModifier)  hash.Add(_modifier);
        if (_hasAppended)  hash.Add(_appended);
        return hash.ToHashCode();
    }

    /// <inheritdoc/>
    public static bool operator ==(ComposableNodeModifierKey a, ComposableNodeModifierKey b) => a.Equals(b);

    /// <inheritdoc/>
    public static bool operator !=(ComposableNodeModifierKey a, ComposableNodeModifierKey b) => !a.Equals(b);
}
