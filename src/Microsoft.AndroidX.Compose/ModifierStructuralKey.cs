namespace AndroidX.Compose;

/// <summary>
/// Structural fingerprint of an entire <see cref="Modifier"/> chain —
/// the array of <see cref="ModifierOpKey"/>s for each op in chain
/// order. Wraps the array so
/// <see cref="EqualityComparer{T}.Default"/> compares chains
/// element-wise (the default <c>object.Equals</c> on a raw array would
/// fall back to reference equality and miss every structurally-equal
/// pair built across two render passes).
/// </summary>
internal readonly struct ModifierStructuralKey : IEquatable<ModifierStructuralKey>
{
    readonly ModifierOpKey[] _keys;

    internal ModifierStructuralKey(ModifierOpKey[] keys)
    {
        _keys = keys ?? [];
    }

    /// <summary>Number of ops in the chain.</summary>
    public int Count => _keys.Length;

    /// <summary>Read-only access to a single op's key.</summary>
    public ModifierOpKey this[int index] => _keys[index];

    /// <inheritdoc/>
    public bool Equals(ModifierStructuralKey other)
    {
        if (_keys.Length != other._keys.Length) return false;
        for (int i = 0; i < _keys.Length; i++)
        {
            if (!_keys[i].Equals(other._keys[i])) return false;
        }
        return true;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is ModifierStructuralKey k && Equals(k);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        for (int i = 0; i < _keys.Length; i++)
            hash.Add(_keys[i]);
        return hash.ToHashCode();
    }

    /// <inheritdoc/>
    public static bool operator ==(ModifierStructuralKey a, ModifierStructuralKey b) => a.Equals(b);

    /// <inheritdoc/>
    public static bool operator !=(ModifierStructuralKey a, ModifierStructuralKey b) => !a.Equals(b);
}
