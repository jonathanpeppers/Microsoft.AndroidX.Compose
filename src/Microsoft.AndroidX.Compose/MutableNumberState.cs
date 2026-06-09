using System.Numerics;
using System.Runtime.CompilerServices;
using AndroidX.Compose.Runtime;

namespace AndroidX.Compose;

/// <summary>
/// Numeric-specialized mutable state. Adds <c>operator ++/--</c> so user
/// code reads almost identically to Kotlin:
/// <code>
/// var count = Remember(() => new MutableNumberState&lt;int&gt;(0));
/// count++;                  // mutates the underlying value
/// Text($"Count: {count}");  // ToString() forwards to Value
/// </code>
///
/// For <c>int</c>, <c>long</c>, and <c>float</c>, the underlying slot is
/// a primitive Compose state (<c>MutableIntState</c>, <c>MutableLongState</c>,
/// <c>MutableFloatState</c>), so reads/writes avoid the
/// <c>Java.Lang.Integer</c>/<c>Long</c>/<c>Float</c> allocation that the
/// generic <see cref="MutableState{T}"/> path pays on every access. All
/// other <see cref="INumber{TSelf}"/> primitives that <see cref="MutableState{T}"/>
/// can box (<c>sbyte</c>, <c>byte</c>, <c>short</c>, <c>ushort</c>,
/// <c>uint</c>, <c>ulong</c>, <c>double</c>) still work — they fall through
/// to the boxed path. Types that <see cref="MutableState{T}"/> can't box
/// (<c>decimal</c>, <c>Half</c>, <c>BigInteger</c>, <c>nint</c>,
/// <c>nuint</c>) compile but throw at construction.
/// </summary>
/// <remarks>
/// <para>
/// The primitive-specialized fast path requires the underlying
/// <see cref="IMutableState"/> to actually be a primitive subtype
/// (<see cref="IMutableIntState"/>, <see cref="IMutableLongState"/>,
/// <see cref="IMutableFloatState"/>). That's true when constructed
/// via <c>new MutableNumberState&lt;int&gt;(0)</c> — <c>CreateState</c>
/// picks the right Kotlin factory. It's <i>not</i> guaranteed when the
/// wrapper is built around a state produced elsewhere, such as the
/// boxed <c>mutableStateOf(restoredValue, policy)</c> that
/// <see cref="ComposeExtensions.RememberSaveable{T}(Func{T}, int, string)"/> returns after a process
/// death restore. The instance <c>_kind</c> field is probed from the
/// supplied state once at construction so the <see cref="Value"/>
/// getter/setter never attempts an invalid cast.
/// </para>
/// </remarks>
public class MutableNumberState<T> : MutableState<T> where T : INumber<T>
{
    enum Kind { Other, Int, Long, Float }

    Kind _kind;

    public MutableNumberState(T initial) : base(CreateState(initial))
    {
        _kind = ProbeKind(_state);
    }

    static IMutableState CreateState(T initial)
    {
        if (typeof(T) == typeof(int))
            return SnapshotIntStateKt.MutableIntStateOf(Unsafe.As<T, int>(ref initial));
        if (typeof(T) == typeof(long))
            return SnapshotLongStateKt.MutableLongStateOf(Unsafe.As<T, long>(ref initial));
        if (typeof(T) == typeof(float))
            return PrimitiveSnapshotStateKt.MutableFloatStateOf(Unsafe.As<T, float>(ref initial));
        return SnapshotStateKt.MutableStateOf(
            MutableState<T>.ToJava(initial),
            SnapshotStateKt.StructuralEqualityPolicy());
    }

    static Kind ProbeKind(IMutableState state) => state switch
    {
        IMutableIntState   => Kind.Int,
        IMutableLongState  => Kind.Long,
        IMutableFloatState => Kind.Float,
        _                  => Kind.Other,
    };

    internal override void SetUnderlyingState(IMutableState state)
    {
        base.SetUnderlyingState(state);
        _kind = ProbeKind(state);
    }

    public override T Value
    {
        get
        {
            switch (_kind)
            {
                case Kind.Int:
                    int i = ((IMutableIntState)_state).IntValue;
                    return Unsafe.As<int, T>(ref i);
                case Kind.Long:
                    long l = ((IMutableLongState)_state).LongValue;
                    return Unsafe.As<long, T>(ref l);
                case Kind.Float:
                    float f = ((IMutableFloatState)_state).FloatValue;
                    return Unsafe.As<float, T>(ref f);
                default:
                    return base.Value;
            }
        }
        set
        {
            switch (_kind)
            {
                case Kind.Int:
                    ((IMutableIntState)_state).IntValue = Unsafe.As<T, int>(ref value);
                    break;
                case Kind.Long:
                    ((IMutableLongState)_state).LongValue = Unsafe.As<T, long>(ref value);
                    break;
                case Kind.Float:
                    ((IMutableFloatState)_state).FloatValue = Unsafe.As<T, float>(ref value);
                    break;
                default:
                    base.Value = value;
                    break;
            }
        }
    }

    public static MutableNumberState<T> operator ++(MutableNumberState<T> s) { s.Value++; return s; }
    public static MutableNumberState<T> operator --(MutableNumberState<T> s) { s.Value--; return s; }
}
