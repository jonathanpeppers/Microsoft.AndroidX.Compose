using Android.Runtime;
using AndroidX.Compose.Runtime;
using Kotlin.Jvm.Functions;

namespace ComposeNet;

// Three small [Register]'d Java.Lang.Object ACW adapters — one per
// arity Compose actually hands us. Container nodes' Render impls
// construct one of these to wrap a C# delegate that walks children.
//
// These are internal: user code never sees them. They exist because
// Compose's content parameters are typed as Function0/Function2/Function3
// in the bytecode, and the only way to hand Compose a "Kotlin lambda"
// from C# is to implement the IFunctionN interface from a real Java
// class — hence [Register].

[Register("composenet/compose/ComposableLambda0")]
internal sealed class ComposableLambda0 : Java.Lang.Object, IFunction0
{
    readonly System.Action _body;
    public ComposableLambda0(System.Action body) => _body = body;
    public Java.Lang.Object? Invoke() { _body(); return null; }
}

// Function1<T, Unit> — single-arg callbacks (TextField's onValueChange,
// onCheckedChange, etc.). The body receives the raw Java arg and is
// responsible for unboxing it; ComposableLambda1String below covers the
// common String case.
[Register("composenet/compose/ComposableLambda1")]
internal sealed class ComposableLambda1 : Java.Lang.Object, IFunction1
{
    readonly System.Action<Java.Lang.Object?> _body;
    public ComposableLambda1(System.Action<Java.Lang.Object?> body) => _body = body;

    public Java.Lang.Object? Invoke(Java.Lang.Object? p0)
    {
        _body(p0);
        return null;
    }
}

// Function2<Composer, Integer, Unit> — top-level composition + theme/scope
// content. p0 = composer, p1 = $changed.
[Register("composenet/compose/ComposableLambda2")]
internal sealed class ComposableLambda2 : Java.Lang.Object, IFunction2
{
    readonly System.Action<IComposer> _body;
    public ComposableLambda2(System.Action<IComposer> body) => _body = body;

    public Java.Lang.Object? Invoke(Java.Lang.Object? p0, Java.Lang.Object? p1)
    {
        System.ArgumentNullException.ThrowIfNull(p0);
        var composer = Android.Runtime.Extensions.JavaCast<IComposer>(p0);
        _body(composer);
        return null;
    }
}

// Function3<Scope, Composer, Integer, Unit> — Column/Row/Box/Button content.
// p0 = scope (RowScope/ColumnScope), p1 = composer, p2 = $changed.
// The scope receiver is ignored here; users can't access it from the
// tree-style API anyway (modifiers like .weight live on Modifier
// extensions, which are a Tier 2 problem).
[Register("composenet/compose/ComposableLambda3")]
internal sealed class ComposableLambda3 : Java.Lang.Object, IFunction3
{
    readonly System.Action<IComposer> _body;
    public ComposableLambda3(System.Action<IComposer> body) => _body = body;

    public Java.Lang.Object? Invoke(Java.Lang.Object? p0, Java.Lang.Object? p1, Java.Lang.Object? p2)
    {
        System.ArgumentNullException.ThrowIfNull(p1);
        var composer = Android.Runtime.Extensions.JavaCast<IComposer>(p1);
        _body(composer);
        return null;
    }
}
