using System.Numerics;
using System.Runtime.CompilerServices;
using Android.Runtime;
using AndroidX.Compose.Runtime;

namespace ComposeNet;

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
public class MutableNumberState<T> : MutableState<T> where T : INumber<T>
{
    enum Kind { Other, Int, Long, Float }

    // Per-closed-generic constant: each MutableNumberState<int>,
    // MutableNumberState<long>, MutableNumberState<float>, ... gets its
    // own s_kind set once at type init. The Value get/set switch on it
    // collapses to the live branch only on the primitive paths, with no
    // typeof(T) check per access.
    static readonly Kind s_kind = ResolveKind();

    static Kind ResolveKind()
    {
        if (typeof(T) == typeof(int))   return Kind.Int;
        if (typeof(T) == typeof(long))  return Kind.Long;
        if (typeof(T) == typeof(float)) return Kind.Float;
        return Kind.Other;
    }

    public MutableNumberState(T initial) : base(CreateState(initial)) { }

    static IMutableState CreateState(T initial)
    {
        switch (s_kind)
        {
            case Kind.Int:
                return Java.Lang.Object.GetObject<IMutableState>(
                    ComposeBridges.MutableIntStateOf(Unsafe.As<T, int>(ref initial)),
                    JniHandleOwnership.TransferLocalRef)!;
            case Kind.Long:
                return Java.Lang.Object.GetObject<IMutableState>(
                    ComposeBridges.MutableLongStateOf(Unsafe.As<T, long>(ref initial)),
                    JniHandleOwnership.TransferLocalRef)!;
            case Kind.Float:
                return Java.Lang.Object.GetObject<IMutableState>(
                    ComposeBridges.MutableFloatStateOf(Unsafe.As<T, float>(ref initial)),
                    JniHandleOwnership.TransferLocalRef)!;
            default:
                return SnapshotStateKt.MutableStateOf(
                    MutableState<T>.ToJava(initial),
                    SnapshotStateKt.StructuralEqualityPolicy());
        }
    }

    public override T Value
    {
        get
        {
            switch (s_kind)
            {
                case Kind.Int:
                    int i = ComposeBridges.MutableIntStateGetIntValue(((Java.Lang.Object)_state).Handle);
                    return Unsafe.As<int, T>(ref i);
                case Kind.Long:
                    long l = ComposeBridges.MutableLongStateGetLongValue(((Java.Lang.Object)_state).Handle);
                    return Unsafe.As<long, T>(ref l);
                case Kind.Float:
                    float f = ComposeBridges.MutableFloatStateGetFloatValue(((Java.Lang.Object)_state).Handle);
                    return Unsafe.As<float, T>(ref f);
                default:
                    return base.Value;
            }
        }
        set
        {
            switch (s_kind)
            {
                case Kind.Int:
                    ComposeBridges.MutableIntStateSetIntValue(
                        ((Java.Lang.Object)_state).Handle,
                        Unsafe.As<T, int>(ref value));
                    break;
                case Kind.Long:
                    ComposeBridges.MutableLongStateSetLongValue(
                        ((Java.Lang.Object)_state).Handle,
                        Unsafe.As<T, long>(ref value));
                    break;
                case Kind.Float:
                    ComposeBridges.MutableFloatStateSetFloatValue(
                        ((Java.Lang.Object)_state).Handle,
                        Unsafe.As<T, float>(ref value));
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
