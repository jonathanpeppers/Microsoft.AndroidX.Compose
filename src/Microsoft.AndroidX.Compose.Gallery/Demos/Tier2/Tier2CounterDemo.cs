using AndroidX.Compose.Gallery.Registry;
using AndroidX.Compose.Runtime;

namespace AndroidX.Compose.Gallery.Demos.Tier2;

/// <summary>
/// Tier 2 demo: a counter rendered by a <c>[Composable]</c> static
/// method. The button click flips a <see cref="MutableNumberState{T}"/>
/// the body reads; the generator-emitted restart-group wrapper around
/// <see cref="Counter"/> runs its <see cref="ComposeExtensions.DiffSlot{T}"/>
/// diff over each parameter and skips re-running the body when nothing
/// changed.
/// </summary>
/// <remarks>
/// <para>
/// This is the simplest end-to-end exerciser of the Tier 2 pipeline —
/// it lives alongside the tree-style facade catalog so both styles
/// can coexist in the same gallery. The body invokes the tree-style
/// <see cref="Column"/> / <see cref="Text"/> / <see cref="Button"/>
/// facades for now (until Tier 2 sibling entry points for those
/// facades land in a follow-up PR), so the on-screen output is
/// indistinguishable from the equivalent tree-style demo — the
/// difference is structural: <see cref="CounterImpl"/> only runs
/// when an input parameter actually changed.
/// </para>
/// </remarks>
public static partial class Tier2CounterDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "tier2-counter",
        CategoryId:  "tier2",
        Title:       "[Composable] counter",
        Description: "A counter rendered by a Tier 2 [Composable] static method — restart group, per-param DiffSlot, skip-on-unchanged.",
        Build:       static c =>
        {
            var count = c.Remember(() => new MutableNumberState<int>(0));
            return new Tier2Adapter(comp => Counter(comp, count.Value, () => count.Value++));
        });

    /// <summary>
    /// Tier 2 composable: declarative C# entry point the generator
    /// wraps with <c>StartRestartGroup</c> + <c>DiffSlot</c> +
    /// <c>SkipToGroupEnd</c> + <c>EndRestartGroup.UpdateScope</c>.
    /// The user-written body lives in <see cref="CounterImpl"/>.
    /// </summary>
    [Composable]
    public static partial void Counter(IComposer composer, int count, Action onIncrement);

    /// <summary>
    /// User-written body for <see cref="Counter"/>. The generator
    /// detects the <c>Impl</c> suffix and emits the wrapper that
    /// invokes this method only when at least one parameter changed
    /// since the previous composition (or the runtime forces a real
    /// pass).
    /// </summary>
    static void CounterImpl(IComposer composer, int count, Action onIncrement)
    {
        // For the MVP we render the body through the existing
        // tree-style facade catalog by constructing a one-shot node
        // tree and calling Render. Once Tier 2 sibling entry points
        // are emitted for the facade catalog (follow-up), this body
        // becomes a sequence of statically-threaded calls with no
        // ComposableNode allocations.
        var node = new Column
        {
            new Text($"Tier 2 count: {count}"),
            new Button(onClick: onIncrement)
            {
                new Text("Tap to increment"),
            },
        };
        node.Render(composer);
    }
}

/// <summary>
/// Adapter <see cref="ComposableNode"/> that delegates its
/// <c>Render</c> implementation to a callback. Lets a Tier 2
/// <c>[Composable]</c> static method plug into the gallery's
/// tree-style demo <c>Build</c> contract.
/// </summary>
internal sealed class Tier2Adapter : ComposableNode
{
    readonly Action<IComposer> _body;

    public Tier2Adapter(Action<IComposer> body)
    {
        ArgumentNullException.ThrowIfNull(body);
        _body = body;
    }

    public override void Render(IComposer composer)
    {
        ArgumentNullException.ThrowIfNull(composer);
        _body(composer);
    }
}
