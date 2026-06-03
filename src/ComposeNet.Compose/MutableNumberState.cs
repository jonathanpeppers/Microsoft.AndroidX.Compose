using System.Numerics;

namespace ComposeNet;

/// <summary>
/// Numeric-specialized mutable state. Adds <c>operator ++/--</c> so user
/// code reads almost identically to Kotlin:
/// <code>
/// var count = Remember(() => new MutableNumberState&lt;int&gt;(0));
/// count++;                  // mutates the underlying value
/// Text($"Count: {count}");  // ToString() forwards to Value
/// </code>
/// The class constraint is <see cref="INumber{TSelf}"/>, but <typeparamref name="T"/>
/// must also be a built-in numeric primitive that <see cref="MutableState{T}"/>
/// knows how to box for the JVM: <c>sbyte</c>, <c>byte</c>, <c>short</c>,
/// <c>ushort</c>, <c>int</c>, <c>uint</c>, <c>long</c>, <c>ulong</c>,
/// <c>float</c>, or <c>double</c>. Other <see cref="INumber{TSelf}"/>
/// implementations (<c>decimal</c>, <c>Half</c>, <c>BigInteger</c>,
/// <c>nint</c>, <c>nuint</c>) compile but throw at construction because
/// they have no clean Java box.
/// </summary>
public class MutableNumberState<T> : MutableState<T> where T : INumber<T>
{
    public MutableNumberState(T initial) : base(initial) { }

    public static MutableNumberState<T> operator ++(MutableNumberState<T> s) { s.Value++; return s; }
    public static MutableNumberState<T> operator --(MutableNumberState<T> s) { s.Value--; return s; }
}
