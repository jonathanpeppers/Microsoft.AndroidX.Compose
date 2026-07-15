using AndroidX.Compose.Gallery.Registry;
using static AndroidX.Compose.Composables;

namespace AndroidX.Compose.Gallery.Demos.Tier2;

/// <summary>
/// Tier 2 demo: a counter rendered by a <c>[Composable]</c> static
/// method. The button callback captures the next count on each composition,
/// proving its remembered JNI adapter rebinds to the latest managed target.
/// The generator-emitted call-site interceptor around
/// <see cref="Counter"/> runs without an explicit composer parameter;
/// generated interceptors recover the active composer for restart groups
/// and nested calls.
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
        Description: "A Tier 2 counter proving stable content identity and callback target rebinding.",
        Build:       static _ => new Tier2Adapter(() => Counter()));

    /// <summary>
    /// Tier 2 composable — a single plain static method. Every call
    /// site of <see cref="Counter"/> is intercepted by the source
    /// generator and rewired to a wrapper that acquires the active
    /// composer and opens a restart group.
    /// </summary>
    [Composable]
    public static void Counter()
    {
        var count = Remember(() => new MutableNumberState<int>(0));
        int nextCount = count.Value + 1;

        // Tier 2 all the way down — every call here is itself an
        // intercepted [Composable] call site with its own restart
        // group + DiffSlot + skip path.
        Column(() =>
        {
            Text($"Tier 2 count: {count.Value}");
            Button(
                () => count.Value = nextCount,
                () => Text("Tap to increment"));
        });
    }
}
