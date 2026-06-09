using ComposeNet.Gallery.Registry;

namespace ComposeNet.Gallery.Demos.StateEffectsAnimation;

/// <summary>SnapshotFlow — bridge Compose state reads into a C# IAsyncEnumerable.</summary>
public static class SnapshotFlowDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "state-snapshotflow",
        CategoryId:  "state-effects",
        Title:       "SnapshotFlow",
        Description: "Compose.SnapshotFlow(producer) yields a new value on every snapshot apply. Tap +1 a few times — 'observed' tracks live. Tap Burst x10 — Kotlin's snapshot conflation means you usually only see the final value, demonstrating the bounded(1) DropOldest semantics.",
        Build:       () =>
        {
            var counter  = Compose.Remember(() => new MutableNumberState<int>(0));
            var observed = Compose.Remember(() => new MutableNumberState<int>(0));
            var seen     = Compose.Remember(() => new MutableNumberState<int>(0));

            return new Column
            {
                new Text($"Counter:  {counter}"),
                new Text($"Observed: {observed}"),
                new Text($"Emissions seen by flow: {seen}"),

                new LaunchedEffect(key1: null, async ct =>
                {
                    try
                    {
                        await foreach (var value in
                            Compose.SnapshotFlow(() => counter.Value).WithCancellation(ct))
                        {
                            observed.Value = value;
                            seen.Value++;
                        }
                    }
                    catch (OperationCanceledException) { }
                }),

                new Button(onClick: () => counter++) { new Text("+1") },
                new Button(onClick: () =>
                {
                    for (int i = 0; i < 10; i++)
                        counter++;
                }) { new Text("Burst x10 (conflates)") },
                new Button(onClick: () =>
                {
                    counter.Value = 0;
                    seen.Value = 0;
                }) { new Text("Reset") },
            };
        });
}
