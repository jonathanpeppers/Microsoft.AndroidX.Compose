namespace AndroidX.Compose;

/// <summary>
/// Marks a <c>static partial</c> method as a Tier 2 composable —
/// the C# equivalent of a Kotlin <c>@Composable</c> function.
/// The
/// <see cref="AndroidX.Compose.SourceGenerators.ComposableMethodGenerator"/>
/// source generator emits the partial implementation that wraps the
/// user-supplied body (the sibling <c>&lt;Name&gt;Impl</c> static
/// method) in a Compose restart group with per-parameter
/// <c>$changed</c> diffing and a skip-when-unchanged fast path.
/// </summary>
/// <remarks>
/// <para>
/// Tier 2 trades the tree-style <see cref="ComposableNode"/> facade for
/// directly-composer-threaded code. The resulting render loop allocates
/// zero <see cref="ComposableNode"/> instances per recomposition and
/// gives Compose's runtime the same <c>StartRestartGroup</c> +
/// <c>SkipToGroupEnd</c> skip prelude the Kotlin compose-compiler
/// plugin emits.
/// </para>
/// <para>
/// Pattern:
/// </para>
/// <code>
/// public static partial class Screens
/// {
///     [Composable]
///     public static partial void Greeting(IComposer composer, string name);
///
///     // User-written body — the generator wraps this with a restart
///     // group, per-param DiffSlot diffing, and a skip path.
///     static void GreetingImpl(IComposer composer, string name)
///     {
///         // … call other [Composable] methods, threading composer …
///     }
/// }
/// </code>
/// <para>
/// Both the partial declaration and the <c>&lt;Name&gt;Impl</c> body
/// must live in <c>partial</c> declarations of the same containing
/// type. The first parameter must be
/// <see cref="AndroidX.Compose.Runtime.IComposer"/>. The
/// <c>Impl</c> companion must have the same parameter list (modulo
/// name); the generator reports <c>CN5002</c> when it's missing and
/// <c>CN5004</c> when the signatures disagree.
/// </para>
/// <para>
/// Tier 2 composables coexist with the tree-style facade catalog —
/// the existing <see cref="ComposableNode"/>-based composables keep
/// working unchanged, and a Tier 2 method can call into them from
/// inside a <c>SetContent</c> lambda or vice versa.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class ComposableAttribute : Attribute
{
}
