using AndroidX.Compose.Gallery.Registry;
using AndroidX.Compose.Runtime;
using static AndroidX.Compose.Composables;

namespace AndroidX.Compose.Gallery.Demos.Tier2;

/// <summary>
/// Tier 2 demo: a counter rendered by a <c>[Composable]</c> static
/// method. The button click flips a <see cref="MutableNumberState{T}"/>
/// the body reads; the generator-emitted call-site interceptor around
/// <see cref="Counter"/> runs its <see cref="ComposeExtensions.DiffSlot{T}"/>
/// diff over each parameter and skips re-running the body when nothing
/// changed.
/// </summary>
/// <remarks>
/// <para>
/// This is the simplest end-to-end exerciser of the Tier 2 pipeline —
/// it lives alongside the tree-style facade catalog so both styles
/// can coexist in the same gallery. Its body calls the generated
/// Tier 2 <see cref="Column"/> / <see cref="Text"/> /
/// <see cref="Button"/> entry points directly.
/// </para>
/// </remarks>
public static class Tier2CounterDemo
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
    /// Tier 2 composable — a single plain static method. Every call
    /// site of <see cref="Counter"/> is intercepted by the source
    /// generator and rewired to a wrapper that opens a restart group,
    /// diffs each parameter via <see cref="ComposeExtensions.DiffSlot{T}"/>,
    /// and calls <c>SkipToGroupEnd</c> when nothing changed.
    /// </summary>
    [Composable]
    public static void Counter(IComposer composer, int count, Action onIncrement)
    {
        // Tier 2 all the way down — every call here is itself an
        // intercepted [Composable] call site with its own restart
        // group + DiffSlot + skip path.
        Column(composer, c =>
        {
            Text(c, $"Tier 2 count: {count}");
            Button(c, onIncrement, cc => Text(cc, "Tap to increment"));
        });
    }
}
