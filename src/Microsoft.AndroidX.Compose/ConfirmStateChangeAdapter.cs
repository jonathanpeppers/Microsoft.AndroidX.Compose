using System.Diagnostics.CodeAnalysis;
using Kotlin.Jvm.Functions;

namespace AndroidX.Compose;

/// <summary>
/// Abstract base for <c>(T) -&gt; Boolean</c> "confirm state change"
/// veto adapters consumed by Material 3 Remember bridges
/// (<c>rememberDrawerState</c>, <c>rememberModalBottomSheetState</c>,
/// <c>rememberStandardBottomSheetState</c>, …). The callback is part
/// of the <c>remember</c> cache key in Kotlin, so each subclass
/// instance is allocated <strong>once per
/// <see cref="ComposableNode"/></strong> and reused across
/// recompositions — a fresh adapter each pass would invalidate the
/// cached state holder and the drawer/sheet would forget its
/// position. <see cref="Callback"/> is read on every JNI invocation,
/// so callers may mutate it freely without re-allocating. When
/// <see cref="Callback"/> is <c>null</c> the adapter behaves as the
/// Kotlin default <c>{ true }</c> — every transition is allowed.
/// </summary>
/// <typeparam name="T">
/// Kotlin enum/value type the callback receives (e.g.
/// <c>DrawerValue</c>, <c>SheetValue</c>).
/// </typeparam>
/// <remarks>
/// <para>
/// Concrete subclasses (one per state-holder enum) carry their own
/// <c>[Register("net/compose/&lt;TName&gt;ConfirmStateChange")]</c>
/// attribute so each gets a stable JCW class on the Java side, and
/// follow the source generator's naming convention
/// <c>&lt;TName&gt;ConfirmStateChange</c> so a
/// <c>[ConfirmStateChange(typeof(T))]</c> attribute on a Remember
/// bridge parameter resolves to the right adapter automatically. JCW
/// registration must live on the concrete class — abstract / generic
/// base classes can't hold <c>[Register]</c>.
/// </para>
/// <para>
/// Public so generated facade classes (which live alongside this
/// type in <c>AndroidX.Compose</c>) can declare the concrete
/// subclasses as <c>readonly</c> fields; the type itself is not part
/// of the developer-facing API and should not be constructed
/// directly.
/// </para>
/// </remarks>
public abstract class ConfirmStateChangeAdapter<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
    T> : Java.Lang.Object, IFunction1
    where T : Java.Lang.Object
{
    /// <summary>
    /// Developer-supplied veto delegate, assigned by the generator-
    /// emitted facade in its <c>Render</c> preamble from the facade's
    /// <c>ConfirmStateChange</c>/<c>ConfirmValueChange</c> property.
    /// Treated as <c>{ true }</c> when <c>null</c>.
    /// </summary>
    public Func<T, bool>? Callback { get; set; }

    /// <summary>
    /// Kotlin <c>Function1.invoke</c> entry point. Marshals the JNI
    /// argument to <typeparamref name="T"/>, invokes
    /// <see cref="Callback"/>, and returns a boxed
    /// <see cref="Java.Lang.Boolean"/>.
    /// </summary>
    public Java.Lang.Object? Invoke(Java.Lang.Object? p0)
    {
        var cb = Callback;
        if (cb is null)
            return Java.Lang.Boolean.True;
        var value = Android.Runtime.Extensions.JavaCast<T>(p0!)
            ?? throw new InvalidOperationException(
                $"Expected a Java peer of type {typeof(T).Name} from Kotlin; got '{p0?.Class?.Name ?? "null"}'.");
        return cb(value) ? Java.Lang.Boolean.True : Java.Lang.Boolean.False;
    }
}
