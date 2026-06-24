namespace AndroidX.Compose;

/// <summary>
/// Per-parameter contribution to Kotlin Compose's <c>$changed</c>
/// bitmask. Each user parameter occupies 3 bits in <c>$changed</c>:
/// see <see cref="DiffSlotShift"/> for the bit layout. The values of
/// this enum are the un-shifted 3-bit codes the Kotlin compose-compiler
/// emits — shift them into position via
/// <c>(int)bits &lt;&lt; <see cref="DiffSlotShift"/>(paramIndex)</c>
/// (or use the shifted overloads on
/// <see cref="ComposeExtensions"/>).
/// </summary>
/// <remarks>
/// Bit 0 of <c>$changed</c> is reserved for a "force" flag the runtime
/// toggles to skip skipping; the C# facade always leaves it 0. Per-param
/// codes start at bit 1 so param 0 occupies bits 1-3, param 1 bits 4-6,
/// etc. — 10 user params per <c>$changed</c> int. The compose-compiler
/// emits additional <c>$changed</c> ints when a function has &gt; 10
/// defaultable params; the facade generator falls back to <c>0</c>
/// (Uncertain) for any overflow ints.
/// </remarks>
public enum ChangedBits
{
    /// <summary>
    /// 0b000 — caller doesn't know whether the param changed; the
    /// runtime must compare it itself. Equivalent to the legacy
    /// <c>JValue(0)</c> behaviour.
    /// </summary>
    Uncertain = 0,

    /// <summary>
    /// 0b001 — caller has compared this value against the previous
    /// composition's input and confirmed it's the same.
    /// </summary>
    Same = 1,

    /// <summary>
    /// 0b010 — caller knows the value is different from the previous
    /// composition's input.
    /// </summary>
    Different = 2,

    /// <summary>
    /// 0b100 — caller knows this param can never change (compile-time
    /// constant or guaranteed identity-stable across recompositions).
    /// </summary>
    Static = 4,
}
