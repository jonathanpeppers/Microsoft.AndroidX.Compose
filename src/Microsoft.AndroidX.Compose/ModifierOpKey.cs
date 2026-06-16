namespace AndroidX.Compose;

/// <summary>
/// Structural identity of one <see cref="Modifier"/> op. Two ops with
/// the same <see cref="OpName"/> and structurally equal
/// <see cref="Args"/> compare equal, so a chain of keys can be
/// element-wise diffed to detect "modifier didn't change" across
/// recompositions and let Kotlin's <c>$changed</c> bitmask take the
/// skip path.
/// </summary>
/// <remarks>
/// <para>The <see cref="Opaque"/> sentinel is used by
/// <see cref="Modifier.Append(System.Func{System.IntPtr, System.IntPtr})"/>
/// (the unkeyed overload). Two opaque keys are <i>never</i> equal —
/// every fresh allocation gets a unique <c>object</c> identity — so
/// any chain containing one will read as
/// <see cref="ChangedBits.Different"/> on diff. That's the conservative
/// fallback for ops whose closures capture state we can't trivially
/// structure-compare (raw delegates, gesture callbacks, etc.).</para>
///
/// <para><see cref="Args"/> can be any value-equatable shape — typed
/// <c>ValueTuple</c>s for fixed-arity arg lists are the common case
/// (e.g. <c>("Padding", (16f, 16f))</c>). For ops carrying a single
/// reference-typed arg whose own equality is reference-only (delegates,
/// JNI handles, etc.), the conservative behaviour is exactly what we
/// want: equal iff the same instance.</para>
/// </remarks>
internal readonly struct ModifierOpKey : IEquatable<ModifierOpKey>
{
    /// <summary>
    /// Sentinel "I have no structural key" entry — caller used the
    /// unkeyed <see cref="Modifier.Append(System.Func{System.IntPtr, System.IntPtr})"/>
    /// overload. Compares unequal to every other key, including other
    /// <see cref="Opaque"/> instances.
    /// </summary>
    public static ModifierOpKey Opaque => new ModifierOpKey(opaqueMarker: new object());

    /// <summary>
    /// Op name (the <c>Modifier.X(...)</c> factory's PascalCased
    /// suffix, e.g. <c>"Padding"</c>, <c>"FillMaxWidth"</c>). Opaque
    /// keys carry <c>null</c> here.
    /// </summary>
    public string? OpName { get; }

    /// <summary>
    /// Structurally-equatable args bundle (typically a
    /// <see cref="System.ValueTuple"/>) or <c>null</c> for parameterless
    /// ops. Opaque keys carry a unique <see cref="object"/> here so the
    /// equality check below ALWAYS fails.
    /// </summary>
    public object? Args { get; }

    /// <summary>Construct a structural key for a named op.</summary>
    public ModifierOpKey(string opName, object? args)
    {
        OpName = opName;
        Args = args;
    }

    /// <summary>Construct an opaque key (no <see cref="OpName"/>; unique <see cref="Args"/>).</summary>
    ModifierOpKey(object opaqueMarker)
    {
        OpName = null;
        Args = opaqueMarker;
    }

    /// <inheritdoc/>
    public bool Equals(ModifierOpKey other)
    {
        // Opaque keys: OpName is null on both sides AND Args is a
        // unique object — fail by reference inequality.
        if (OpName is null || other.OpName is null)
        {
            if (OpName is null && other.OpName is null)
                return ReferenceEquals(Args, other.Args);
            return false;
        }
        if (!string.Equals(OpName, other.OpName, StringComparison.Ordinal))
            return false;
        // Args == null on both sides means parameterless op; equal.
        if (Args is null && other.Args is null) return true;
        if (Args is null || other.Args is null) return false;
        return EqualityComparer<object>.Default.Equals(Args, other.Args);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is ModifierOpKey k && Equals(k);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        if (OpName is null)
            return Args is null ? 0 : System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(Args);
        return HashCode.Combine(OpName, Args);
    }

    /// <inheritdoc/>
    public static bool operator ==(ModifierOpKey a, ModifierOpKey b) => a.Equals(b);

    /// <inheritdoc/>
    public static bool operator !=(ModifierOpKey a, ModifierOpKey b) => !a.Equals(b);
}
