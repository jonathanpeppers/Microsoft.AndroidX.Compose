namespace AndroidX.Compose;

/// <summary>
/// Tier 2 static entry points that mirror the tree-style facade
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
/// then call by bare name — <c>Text(c, "Hi")</c>,
/// <c>Column(c, cc =&gt; { ... })</c>. The static method names match
/// the tree-style facade type names (<see cref="Text"/>,
/// <see cref="Button"/>, <see cref="Column"/>, <see cref="Row"/>,
/// <see cref="Box"/>) so the two styles read symmetrically; C# resolves
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
