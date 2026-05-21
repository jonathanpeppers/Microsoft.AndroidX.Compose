using Androidx.Compose.Runtime;

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

    static Java.Lang.Object? ToJava(T value) => value switch
    {
        null                  => null,
        Java.Lang.Object o    => o,
        string s              => new Java.Lang.String(s),
        int i                 => Java.Lang.Integer.ValueOf(i),
        long l                => Java.Lang.Long.ValueOf(l),
        bool b                => Java.Lang.Boolean.ValueOf(b),
        double d              => Java.Lang.Double.ValueOf(d),
        float f               => Java.Lang.Float.ValueOf(f),
        _                     => throw new System.NotSupportedException(
                                    $"MutableState<{typeof(T).Name}>: provide a Java.Lang.Object-derived T or use a primitive overload.")
    };

    static T FromJava(Java.Lang.Object? value)
    {
        if (value is null) return default!;
        if (value is T t) return t;
        // common boxed-primitive unwraps
        if (typeof(T) == typeof(int))    return (T)(object)((Java.Lang.Integer)value).IntValue();
        if (typeof(T) == typeof(long))   return (T)(object)((Java.Lang.Long)value).LongValue();
        if (typeof(T) == typeof(bool))   return (T)(object)((Java.Lang.Boolean)value).BooleanValue();
        if (typeof(T) == typeof(double)) return (T)(object)((Java.Lang.Double)value).DoubleValue();
        if (typeof(T) == typeof(float))  return (T)(object)((Java.Lang.Float)value).FloatValue();
        if (typeof(T) == typeof(string)) return (T)(object)value.ToString()!;
        throw new System.NotSupportedException(
            $"MutableState<{typeof(T).Name}>: don't know how to unwrap {value.GetType().Name}");
    }
}

/// <summary>
/// Int-specialized mutable state. Adds <c>implicit operator int</c> and
/// <c>operator ++/--</c> so user code reads almost identically to Kotlin:
/// <code>
/// var count = Remember(() => new MutableIntState(0));
/// count++;                  // mutates
/// Text($"Count: {count}");  // implicitly converts to int
/// </code>
/// </summary>
public sealed class MutableIntState : MutableState<int>
{
    public MutableIntState(int initial) : base(initial) { }

    public static implicit operator int(MutableIntState s) => s.Value;

    public static MutableIntState operator ++(MutableIntState s) { s.Value++; return s; }
    public static MutableIntState operator --(MutableIntState s) { s.Value--; return s; }

    public override string ToString() => Value.ToString();
}
