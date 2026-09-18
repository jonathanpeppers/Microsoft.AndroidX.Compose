using Android.Runtime;
using AndroidX.Compose.Runtime;
using Kotlin.Jvm.Functions;

namespace AndroidX.Compose;

/// <summary>
/// Function3&lt;ScopeOrValue, Composer, Integer, Result&gt; — content and
/// transition value mappings. <c>p0</c> is the receiver scope or typed state,
/// <c>p1</c> is the composer, <c>p2</c> is <c>$changed</c>.
///
/// Action constructors return Kotlin Unit; the Func constructor preserves its result.
/// Content constructor shapes:
/// <list type="bullet">
///   <item><description><c>Action&lt;IComposer&gt;</c> discards the scope —
///     used wherever children don't need to know it (Column, Box,
///     Button).</description></item>
///   <item><description><c>Action&lt;IntPtr, IComposer&gt;</c> receives the
///     raw scope handle, used by container composables whose children
///     are extension-receiver composables (<c>RowScope.NavigationBarItem</c>).
///     The scope is published via <see cref="RenderContext"/> so the
///     child <c>Render</c> can read it.</description></item>
///   <item><description><c>Action&lt;Java.Lang.Object?, IComposer&gt;</c>
///     receives the boxed <c>p0</c> as a <see cref="Java.Lang.Object"/>.
///     Used by value-typed Function3 slots — e.g.
///     <c>Crossfade</c>'s <c>content: @Composable (T) -&gt; Unit</c>
///     where <c>p0</c> is the boxed targetState rather than a scope
///     receiver — so the body can unbox to the user-facing
///     <c>T</c>.</description></item>
/// </list>
/// </summary>
[Register("net/compose/ComposableLambda3")]
internal sealed class ComposableLambda3 : Java.Lang.Object, IFunction3
{
    Func<Java.Lang.Object?, IComposer, Java.Lang.Object?> _body;
    Animation.IAnimatedVisibilityScope? _animatedScope = RenderContext.CurrentAnimatedVisibilityScope;

    public ComposableLambda3(Action<IComposer> body)
        : this((Java.Lang.Object? _, IComposer c) => body(c)) { }

    public ComposableLambda3(Action<IntPtr, IComposer> body)
        : this((Java.Lang.Object? p0, IComposer c) => body(p0?.Handle ?? IntPtr.Zero, c)) { }

    public ComposableLambda3(Action<Java.Lang.Object?, IComposer> body)
        : this((value, composer) =>
        {
            body(value, composer);
            return Kotlin.Unit.Instance
                ?? throw new InvalidOperationException("Kotlin Unit was unavailable in ComposableLambda3.");
        }) { }

    public ComposableLambda3(Func<Java.Lang.Object?, IComposer, Java.Lang.Object?> body) => _body = body;

    internal void UpdateResult(Func<Java.Lang.Object?, IComposer, Java.Lang.Object?> body)
    {
        _body = body;
        _animatedScope = RenderContext.CurrentAnimatedVisibilityScope;
    }

    public Java.Lang.Object? Invoke(Java.Lang.Object? p0, Java.Lang.Object? p1, Java.Lang.Object? p2)
    {
        ArgumentNullException.ThrowIfNull(p1);
        var composer = Android.Runtime.Extensions.JavaCast<IComposer>(p1);
        try
        {
            using var context = ComposableContext.Enter(composer);
            using var animation = RenderContext.PushAnimatedVisibilityScope(_animatedScope);
            return _body(p0, composer);
        }
        finally
        {
            // Raw scope callbacks borrow the JNI reference owned by this peer.
            GC.KeepAlive(p0);
        }
    }
}
