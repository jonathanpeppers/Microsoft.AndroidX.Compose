using System;
using System.Runtime.CompilerServices;
using AndroidX.Compose.Runtime;
using AndroidX.Compose.Runtime.Internal;
using Kotlin.Jvm.Functions;

namespace ComposeNet;

/// <summary>
/// Helpers that wrap <see cref="ComposableLambda2"/> /
/// <see cref="ComposableLambda3"/> @Composable content lambdas through
/// Compose's own <c>ComposableLambdaKt.ComposableLambda</c> factory so
/// the runtime owns lambda identity across recompositions.
///
/// <para>The factory does, internally:</para>
/// <list type="number">
///   <item><description><c>composer.startReplaceableGroup(key)</c></description></item>
///   <item><description>Looks up a remembered slot; on first reach it
///     stores a fresh <c>ComposableLambdaImpl(key, tracked)</c>, otherwise
///     it reuses the stored instance.</description></item>
///   <item><description>Updates the impl's <c>block</c> field to point
///     at the fresh inner <see cref="ComposableLambda2"/> /
///     <see cref="ComposableLambda3"/> we just allocated.</description></item>
///   <item><description><c>composer.endReplaceableGroup()</c></description></item>
///   <item><description>Returns the stable impl.</description></item>
/// </list>
///
/// <para>The result is an <see cref="IComposableLambda"/> whose identity
/// is stable across recompositions even though we allocate a fresh
/// inner adapter each call — exactly what
/// <c>SubcomposeLayout</c> needs to keep its per-lambda-identity content
/// cache from thrashing.</para>
///
/// <para><b>Convention:</b> never construct
/// <see cref="ComposableLambda2"/> / <see cref="ComposableLambda3"/>
/// directly inside a <c>Render</c> method — always route through
/// <see cref="Wrap2"/> / <see cref="Wrap3(IComposer, Action{IComposer}, int, string)"/>.
/// The non-@Composable adapters <see cref="ComposableLambda0"/> /
/// <see cref="ComposableLambda1"/> (onClick / onValueChange callbacks)
/// are NOT wrapped — they run outside composition and would crash
/// inside Compose's restart-group machinery.</para>
/// </summary>
internal static class ComposableLambdas
{
    /// <summary>
    /// Wrap an <see cref="Action{IComposer}"/> as an identity-stable
    /// <see cref="IFunction2"/> (the Function2&lt;Composer, Int, Unit&gt;
    /// @Composable content-slot shape used by <c>topBar</c>,
    /// <c>bottomBar</c>, <c>title</c>, <c>icon</c>, etc.).
    /// </summary>
    public static IFunction2 Wrap2(
        IComposer composer,
        Action<IComposer> body,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
        => (IFunction2)ComposableLambdaKt.ComposableLambda(
            composer, HashCode.Combine(line, file), tracked: true,
            block: new ComposableLambda2(body));

    /// <summary>
    /// Wrap an <see cref="Action{IComposer}"/> as an identity-stable
    /// <see cref="IFunction3"/> (the Function3&lt;Scope, Composer, Int, Unit&gt;
    /// @Composable content-slot shape used by <c>Column</c> / <c>Row</c> /
    /// <c>Box</c> / <c>Button</c> bodies). The scope receiver is discarded.
    /// </summary>
    public static IFunction3 Wrap3(
        IComposer composer,
        Action<IComposer> body,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
        => (IFunction3)ComposableLambdaKt.ComposableLambda(
            composer, HashCode.Combine(line, file), tracked: true,
            block: new ComposableLambda3(body));

    /// <summary>
    /// Wrap an <see cref="Action{IntPtr, IComposer}"/> as an
    /// identity-stable <see cref="IFunction3"/> that receives the raw
    /// scope handle (RowScope, ColumnScope, SegmentedButtonRowScope,
    /// etc.) — for containers whose children need to forward the scope
    /// to a scope-extension Kotlin static (e.g.
    /// <c>RowScope.NavigationBarItem</c>).
    /// </summary>
    public static IFunction3 Wrap3(
        IComposer composer,
        Action<IntPtr, IComposer> body,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
        => (IFunction3)ComposableLambdaKt.ComposableLambda(
            composer, HashCode.Combine(line, file), tracked: true,
            block: new ComposableLambda3(body));
}
