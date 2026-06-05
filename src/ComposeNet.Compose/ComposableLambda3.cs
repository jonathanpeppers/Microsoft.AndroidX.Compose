using Android.Runtime;
using AndroidX.Compose.Runtime;
using Kotlin.Jvm.Functions;

namespace ComposeNet;

/// <summary>
/// Function3&lt;Scope, Composer, Integer, Unit&gt; — Column/Row/Box/Button
/// content. <c>p0</c> is the receiver scope (RowScope/ColumnScope),
/// <c>p1</c> is the composer, <c>p2</c> is <c>$changed</c>.
///
/// Two ctors: the <c>Action&lt;IComposer&gt;</c> form discards the scope
/// (used wherever children don't need to know it — Column, Box, Button);
/// the <c>Action&lt;IntPtr, IComposer&gt;</c> form receives the raw scope
/// handle, used by container composables whose children are
/// extension-receiver composables (<c>RowScope.NavigationBarItem</c>).
/// The scope is published via <see cref="RenderContext"/> so the child
/// <c>Render</c> can read it.
/// </summary>
[Register("composenet/compose/ComposableLambda3")]
internal sealed class ComposableLambda3 : Java.Lang.Object, IFunction3
{
    readonly System.Action<IntPtr, IComposer> _body;

    public ComposableLambda3(System.Action<IComposer> body)
        : this((_, c) => body(c)) { }

    public ComposableLambda3(System.Action<IntPtr, IComposer> body) => _body = body;

    // Kotlin Function3<Scope, Composer, Int, Unit> contractually returns
    // Unit.INSTANCE. See ComposableLambda0 / issue #43 for the rationale.
    public Java.Lang.Object Invoke(Java.Lang.Object? p0, Java.Lang.Object? p1, Java.Lang.Object? p2)
    {
        System.ArgumentNullException.ThrowIfNull(p1);
        var composer = Android.Runtime.Extensions.JavaCast<IComposer>(p1);
        using var _ = ComposeContext.Push(composer);
        _body(p0?.Handle ?? IntPtr.Zero, composer);
        return global::Kotlin.Unit.Instance!;
    }
}
