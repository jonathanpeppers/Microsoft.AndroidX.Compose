using System.Collections.Generic;
using Android.Runtime;

namespace ComposeNet;

/// <summary>
/// Wraps an arbitrary managed value <typeparamref name="T"/> as a
/// <see cref="Java.Lang.Object"/> so it can cross the JNI boundary.
/// Used by <see cref="CompositionLocal{T}"/> to hand pure-C# POCO
/// values (themes, settings, custom contexts) to Kotlin's
/// <c>compositionLocalOf</c> / <c>staticCompositionLocalOf</c>
/// machinery without forcing users to make those types derive
/// from <see cref="Java.Lang.Object"/>.
///
/// <para>Equality is forwarded to <see cref="EqualityComparer{T}"/>
/// so structural-equality <c>CompositionLocalOf</c> readers
/// recompose only when the underlying value actually changes — two
/// distinct <see cref="ManagedBox{T}"/> instances wrapping equal
/// values compare equal.</para>
///
/// <para>Held by the Java-side provider machinery while a
/// <see cref="CompositionLocalProvider"/> is on the composition's
/// stack; collected once Compose drops its reference. The JCW base
/// class keeps the managed peer alive for the JVM peer's lifetime.</para>
/// </summary>
[Register("composenet/compose/ManagedBox")]
internal sealed class ManagedBox<T> : Java.Lang.Object
{
    public T Value { get; }

    public ManagedBox(T value) => Value = value;

    public override bool Equals(Java.Lang.Object? obj) =>
        obj is ManagedBox<T> other &&
        EqualityComparer<T>.Default.Equals(Value, other.Value);

    public override int GetHashCode() =>
        Value is null ? 0 : EqualityComparer<T>.Default.GetHashCode(Value);

    public override string ToString() =>
        Value?.ToString() ?? "null";
}
