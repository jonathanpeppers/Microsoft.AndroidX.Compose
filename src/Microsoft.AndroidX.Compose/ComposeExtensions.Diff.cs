using System.Runtime.CompilerServices;
using AndroidX.Compose.Runtime;

namespace AndroidX.Compose;

public static partial class ComposeExtensions
{
    /// <summary>
    /// Bit position within Kotlin Compose's <c>$changed</c> int for the
    /// 3-bit code of param <paramref name="paramIndex"/>. Bit 0 is the
    /// "force" flag; param 0 occupies bits 1-3, param 1 bits 4-6, etc.
    /// The compose-compiler packs 10 params per int — overflow params
    /// land in the next int, which the facade generator always emits as
    /// <c>0</c> (Uncertain).
    /// </summary>
    public static int DiffSlotShift(int paramIndex) => 1 + paramIndex * 3;

    /// <summary>
    /// Compose's per-param <c>$changed</c> contribution backed by the
    /// active composer's slot table: stash the previous render's value
    /// at this call site, then on next render compare via
    /// <see cref="EqualityComparer{T}.Default"/> and return
    /// <see cref="ChangedBits.Same"/> /
    /// <see cref="ChangedBits.Different"/> shifted into the slot for
    /// the bit position. First call at a given site returns
    /// <see cref="ChangedBits.Different"/> (no prior value to compare
    /// against — the runtime treats that as "must consider").
    /// </summary>
    /// <typeparam name="T">
    /// Slot type — typically the structural key of a facade param
    /// (primitive, packed value-type long, JNI handle, modifier
    /// structural-key hash, etc.). <c>null</c> is a legal value;
    /// transitions between <c>null</c> and non-<c>null</c> count as
    /// "different".
    /// </typeparam>
    /// <param name="composer">Active composer.</param>
    /// <param name="value">This composition's value for the param.</param>
    /// <param name="bitOffset">
    /// The bit position within <c>$changed</c> for this param's 3-bit
    /// slot. Bit 0 is the "force" flag, so param 0 gets bit 1, param 1
    /// gets bit 4, etc. — matches the value
    /// <see cref="DiffSlotShift"/> returns for that param's index.
    /// Caller pre-computes via <c>DiffSlotShift(paramIndex)</c> or
    /// hardcodes the literal bit position.
    /// </param>
    /// <param name="line">Filled in by the compiler.</param>
    /// <param name="file">Filled in by the compiler.</param>
    /// <returns>
    /// <c>(int)<see cref="ChangedBits.Same"/> &lt;&lt; bitOffset</c>
    /// when the value matches the previous render, otherwise
    /// <c>(int)<see cref="ChangedBits.Different"/> &lt;&lt; …</c>.
    /// Caller ORs it into the <c>$changed</c> bitmask; mismatch with
    /// the runtime's own diff is harmless (Kotlin treats the result
    /// as a hint).
    /// </returns>
    public static int DiffSlot<T>(
        this IComposer composer,
        T? value,
        int bitOffset,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
    {
        ArgumentNullException.ThrowIfNull(composer);

        composer.StartReplaceableGroup(SourceLocationKey.Compute(line, file));
        try
        {
            // Box value types into the holder's `object? Value` field.
            // Reference types skip the box. The previous render's
            // unboxed value comes back through EqualityComparer<T?>
            // because we cast holder.Value back to T? before compare.
            object? boxed = value;
            if (composer.RememberedValue() is DiffSlotHolder existing)
            {
                T? prev = existing.Value is T t ? t : default;
                bool prevPresent = existing.HasValue;
                bool sameNullness = prevPresent
                    ? value is not null
                    : value is null;
                if (sameNullness && EqualityComparer<T?>.Default.Equals(prev, value))
                    return (int)ChangedBits.Same << bitOffset;
                existing.Value = boxed;
                existing.HasValue = value is not null;
                return (int)ChangedBits.Different << bitOffset;
            }
            composer.UpdateRememberedValue(new DiffSlotHolder(boxed, value is not null));
            return (int)ChangedBits.Different << bitOffset;
        }
        finally
        {
            composer.EndReplaceableGroup();
        }
    }
}

