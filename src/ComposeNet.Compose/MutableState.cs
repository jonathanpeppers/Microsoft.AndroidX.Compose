using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Generic typed wrapper around <see cref="IMutableState"/>. Reads/writes
/// boxed values through the JVM state container so changes are observed
/// by the Compose runtime and trigger recomposition.
/// </summary>
public class MutableState<T>
{
    internal readonly IMutableState _state;

    public MutableState(T initial)
    {
        _state = SnapshotStateKt.MutableStateOf(
            ToJava(initial),
            SnapshotStateKt.StructuralEqualityPolicy());
    }

    public T Value
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

    static Java.Lang.Object? ToJava(T value) => value switch
    {
        null                  => null,
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
        _                     => throw new System.NotSupportedException(
                                    $"MutableState<{typeof(T).Name}>: T must be a Java.Lang.Object subclass, string, bool, char, or a built-in numeric primitive (sbyte/byte/short/ushort/int/uint/long/ulong/float/double). Types like decimal, Half, BigInteger, and IntPtr have no clean Java box and are not supported.")
    };

    static T FromJava(Java.Lang.Object? value)
    {
        if (value is null) return default!;
        if (value is T t) return t;
        var type = typeof(T);
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
        throw new System.NotSupportedException(
            $"MutableState<{typeof(T).Name}>: don't know how to unwrap {value.GetType().Name}");
    }
}
