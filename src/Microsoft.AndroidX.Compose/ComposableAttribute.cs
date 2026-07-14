namespace AndroidX.Compose;

/// <summary>
/// Marks a static method as a Tier 2 composable — the C# equivalent
/// of a Kotlin <c>@Composable</c> function.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="AndroidX.Compose.SourceGenerators.ComposableMethodGenerator"/>
/// source generator scans every call site of a <c>[Composable]</c>
/// method in user code and emits an
/// <c>[InterceptsLocation]</c>-decorated wrapper that opens a Compose
/// restart group, diffs each argument via
/// <see cref="ComposeExtensions.DiffSlot{T}"/>, calls the user method
/// only when something changed (otherwise <c>SkipToGroupEnd</c>), and
/// registers an <c>UpdateScope</c> recompose lambda. The interception
/// happens at the C# compiler level — the user calls
/// <c>Greeting(c, "world")</c> and the compiler silently rewires it
/// to <c>ComposableInterceptors.Composable_xxxx(c, "world")</c>.
/// </para>
/// <para>
/// Pattern (mirrors Kotlin: <em>one</em> function):
/// </para>
/// <code>
/// [Composable]
/// public static void Greeting(IComposer composer, string name)
/// {
///     // Plain method body — no partial, no Impl companion, no
///     // _changed parameter. Any [Composable] call inside this body
///     // is itself intercepted, so Tier 2 composes cleanly all the
///     // way down.
///     Composables.Text(composer, $"Hello, {name}");
/// }
/// </code>
/// <para>
/// Tier 2 composables coexist with the tree-style facade catalog —
/// the existing <see cref="ComposableNode"/>-based composables keep
/// working unchanged, and a Tier 2 method can call into them from
/// inside a <c>SetContent</c> lambda or vice versa.
/// </para>
/// <para>
/// Requirements (enforced by generator diagnostics):
/// </para>
/// <list type="bullet">
///   <item><description><c>static</c> (CN5001)</description></item>
///   <item><description>returns <c>void</c> (CN5002)</description></item>
///   <item><description>first parameter is
///     <see cref="AndroidX.Compose.Runtime.IComposer"/> (CN5003)</description></item>
/// </list>
/// <para>
/// Because the generator emits <c>[InterceptsLocation]</c> wrappers,
/// consuming projects must opt-in to the C# Interceptors preview
/// feature by adding
/// <c>&lt;InterceptorsPreviewNamespaces&gt;$(InterceptorsPreviewNamespaces);Microsoft.AndroidX.Compose.Generated&lt;/InterceptorsPreviewNamespaces&gt;</c>
/// to their project. The root <c>Directory.Build.props</c> in this
/// repository already does this for every project under <c>src/</c>.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class ComposableAttribute : Attribute
{
}
