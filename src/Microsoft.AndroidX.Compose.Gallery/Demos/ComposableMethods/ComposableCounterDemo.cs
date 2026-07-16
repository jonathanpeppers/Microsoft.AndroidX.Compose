using AndroidX.Compose.Gallery.Registry;
using static AndroidX.Compose.Composables;

namespace AndroidX.Compose.Gallery.Demos.ComposableMethods;

/// <summary>
/// Composable-method demo: a counter rendered by a <c>[Composable]</c> static
/// method. The button callback captures the next count on each composition,
/// proving its remembered JNI adapter rebinds to the latest managed target.
/// The generator-emitted call-site interceptor around
/// <see cref="Counter"/> runs without an explicit composer parameter;
/// generated interceptors recover the active composer for restart groups
/// and nested calls.
/// </summary>
/// <remarks>
/// <para>
/// This is the simplest end-to-end exerciser of composable-method generation —
/// it lives alongside the tree-style facade catalog so both styles
/// can coexist in the same gallery. Its body calls the generated
/// method-style <see cref="Column"/> / <see cref="Text"/> /
/// <see cref="Button"/> entry points directly.
/// </para>
/// </remarks>
public static class ComposableCounterDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "composable-counter",
        CategoryId:  "composable-methods",
        Title:       "[Composable] counter",
        Description: "A composable counter proving stable content identity and callback target rebinding.",
        Build:       static _ => new ComposableDemoAdapter(() => Counter()));

    /// <summary>
    /// Composable entry point — a single plain static method. Every call
    /// site of <see cref="Counter"/> is intercepted by the source
    /// generator and rewired to a wrapper that acquires the active
    /// composer and opens a restart group.
    /// </summary>
    [Composable]
    public static void Counter()
    {
        var count = Remember(() => new MutableNumberState<int>(0));
        int nextCount = count.Value + 1;

        // Composable methods all the way down — every call here is itself an
        // intercepted [Composable] call site with its own restart
        // group + DiffSlot + skip path.
        Column(() =>
        {
            Text($"Composable count: {count.Value}");
            Button(
                () => count.Value = nextCount,
                () => Text("Tap to increment"));
        });
    }
}
