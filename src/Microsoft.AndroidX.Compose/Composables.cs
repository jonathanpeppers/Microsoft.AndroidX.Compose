namespace AndroidX.Compose;

/// <summary>
/// Static composable entry points that mirror the tree-style facade
/// catalog. <c>ComposeFacadeGenerator</c> emits siblings for generated
/// facades; generator holdouts remain hand-written. Each method
/// carries <see cref="ComposableAttribute"/> and its call-site wrapper opens a Compose restart group,
/// diffs each parameter with
/// <see cref="ComposeExtensions.DiffSlot{T}"/>, and elides the
/// body when nothing changed since the previous composition.
/// </summary>
/// <remarks>
/// <para>
/// Recommended usage: <c>using static AndroidX.Compose.Composables;</c>
/// then call by bare name — <c>Text("Hi")</c>,
/// <c>Column(() =&gt; { ... })</c>. Explicit-composer catalog adapters stay
/// internal; low-level custom nodes override
/// <see cref="ComposableNode.Render(AndroidX.Compose.Runtime.IComposer)"/>.
/// The static method names match
/// the tree-style facade type names
/// (<see cref="global::AndroidX.Compose.Text"/>,
/// <see cref="global::AndroidX.Compose.Button"/>,
/// <see cref="global::AndroidX.Compose.Column"/>,
/// <see cref="global::AndroidX.Compose.Row"/>,
/// <see cref="global::AndroidX.Compose.Box"/>) so the two styles read
/// symmetrically; C# resolves
/// <c>new X(...)</c> to the type and bare <c>X(...)</c> to the method.
/// </para>
/// <para>
/// The bodies currently delegate to the tree-style facade catalog —
/// when the wrapper's skip path fires, the tree allocation never
/// happens, which is the actual perf win. A follow-up will rewrite
/// the bodies to call the underlying <c>[ComposeBridge]</c> JNI
/// methods directly so the skip-miss path is also alloc-free.
/// </para>
/// </remarks>
public static partial class Composables
{
}
