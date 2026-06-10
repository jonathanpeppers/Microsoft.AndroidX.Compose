using AndroidX.Compose.Runtime;

namespace AndroidX.Compose;

/// <summary>
/// Generic typed wrapper around <see cref="IMutableState"/>. Reads/writes
/// boxed values through the JVM state container so changes are observed
/// by the Compose runtime and trigger recomposition.
/// </summary>
public class MutableState<T> : IMutableStateWrapper, IState<T>
{
    internal IMutableState _state;

    public MutableState(T initial)
    {
        _state = SnapshotStateKt.MutableStateOf(
            ToJava(initial),
            SnapshotStateKt.StructuralEqualityPolicy());
    }

    // Subclass hook: lets MutableNumberState<int|long|float> inject a
    // primitive-specialized IMutableState (MutableIntState etc.) so its
    // overridden Value getter/setter can bypass the boxed slow path.
    internal MutableState(IMutableState state) => _state = state;

    /// <summary>
    /// Subclass hook for <see cref="IMutableStateWrapper.State"/>'s
    /// setter. <see cref="MutableNumberState{T}"/> overrides this to
    /// re-probe its specialized-state <c>_kind</c> when Compose hands
    /// back a (potentially boxed) restored state after a process-death
    /// round-trip through
    /// <see cref="ComposeExtensions.RememberSaveable{T}(Func{T}, int, string)"/>.
    /// </summary>
    internal virtual void SetUnderlyingState(IMutableState state) => _state = state;

    IMutableState IMutableStateWrapper.State
    {
        get => _state;
        set => SetUnderlyingState(value);
    }

    public virtual T Value
    {
        get => FromJava(_state.Value);
        set => _state.Value = ToJava(value);
    }

    /// <summary>
    /// Returns the underlying value's string representation so
    /// <c>$"...{state}..."</c> interpolation reads as Kotlin would
    /// (a null value renders as <c>"null"</c>).
    /// </summary>
    public override string ToString() => Value?.ToString() ?? "null";

    // Cached per-T: when T is Nullable<U> (e.g. long?, int?, bool?), this
    // is typeof(U) so the primitive dispatch in FromJava/ToJava treats
    // `long?` the same as `long`. For all other T, it's just typeof(T).
    static readonly Type s_effectiveType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

    internal static Java.Lang.Object? ToJava(T value)
    {
        if (value is null) return null;
        // Boxing a non-null Nullable<U> to object yields a boxed U, not a
        // boxed Nullable<U>. That lets one switch handle both T == long and
        // T == long? without a separate nullable branch.
        object boxed = value!;
        return boxed switch
        {
            Java.Lang.Object o    => o,
            string s              => new Java.Lang.String(s),
            bool b                => Java.Lang.Boolean.ValueOf(b),
            char c                => Java.Lang.Character.ValueOf(c),
            sbyte sb              => Java.Lang.Byte.ValueOf(sb),
            byte by               => Java.Lang.Short.ValueOf((short)by),                  // widen 8u -> 16s
            short sh              => Java.Lang.Short.ValueOf(sh),
            ushort us             => Java.Lang.Integer.ValueOf(us),                       // widen 16u -> 32s
            int i                 => Java.Lang.Integer.ValueOf(i),
            uint ui               => Java.Lang.Long.ValueOf(ui),                          // widen 32u -> 64s
            long l                => Java.Lang.Long.ValueOf(l),
            ulong ul              => Java.Lang.Long.ValueOf(unchecked((long)ul)),         // bit-exact round-trip
            float f               => Java.Lang.Float.ValueOf(f),
            double d              => Java.Lang.Double.ValueOf(d),
            _                     => throw new NotSupportedException(
                                        $"MutableState<{typeof(T).Name}>: T must be a Java.Lang.Object subclass, string, bool, char, or a built-in numeric primitive (sbyte/byte/short/ushort/int/uint/long/ulong/float/double), or a Nullable<T> of any of those primitives. Types like decimal, Half, BigInteger, and IntPtr have no clean Java box and are not supported.")
        };
    }

    internal static T FromJava(Java.Lang.Object? value)
    {
        // For T == Nullable<U> this returns the empty Nullable (HasValue=false);
        // for reference T it returns null; for non-nullable struct T it's
        // unreachable in practice because Compose wouldn't store a Java null
        // there, but `default!` is still the only sane fallback.
        if (value is null) return default!;
        if (value is T t) return t;
        // Use the underlying type when T is Nullable<U>, so e.g. `long?`
        // dispatches into the same branch as `long`. The unbox cast
        // `(T)(object)5L` works for T == long? because boxed-primitive
        // unboxes directly into Nullable<long>.
        var type = s_effectiveType;
        if (type == typeof(string))  return (T)(object)value.ToString()!;
        if (type == typeof(bool))    return (T)(object)((Java.Lang.Boolean)value).BooleanValue();
        if (type == typeof(char))    return (T)(object)((Java.Lang.Character)value).CharValue();
        if (type == typeof(sbyte))   return (T)(object)((Java.Lang.Byte)value).ByteValue();
        if (type == typeof(byte))    return (T)(object)(byte)((Java.Lang.Short)value).ShortValue();
        if (type == typeof(short))   return (T)(object)((Java.Lang.Short)value).ShortValue();
        if (type == typeof(ushort))  return (T)(object)(ushort)((Java.Lang.Integer)value).IntValue();
        if (type == typeof(int))     return (T)(object)((Java.Lang.Integer)value).IntValue();
        if (type == typeof(uint))    return (T)(object)(uint)((Java.Lang.Long)value).LongValue();
        if (type == typeof(long))    return (T)(object)((Java.Lang.Long)value).LongValue();
        if (type == typeof(ulong))   return (T)(object)unchecked((ulong)((Java.Lang.Long)value).LongValue());
        if (type == typeof(float))   return (T)(object)((Java.Lang.Float)value).FloatValue();
        if (type == typeof(double))  return (T)(object)((Java.Lang.Double)value).DoubleValue();
        throw new NotSupportedException(
            $"MutableState<{typeof(T).Name}>: don't know how to unwrap {value.GetType().Name}");
    }
}
