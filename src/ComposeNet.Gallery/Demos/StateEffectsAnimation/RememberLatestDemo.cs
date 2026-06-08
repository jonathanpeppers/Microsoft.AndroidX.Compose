using ComposeNet.Gallery.Registry;

namespace ComposeNet.Gallery.Demos.StateEffectsAnimation;

/// <summary>
/// Demonstrates <see cref="Compose.RememberLatest{T}"/>: capture the
/// latest value of a counter from inside a long-lived
/// <c>LaunchedEffect</c> body whose closure was sealed on first
/// composition. Without it, the loop would forever read the value
/// captured on first composition (always 0).
/// </summary>
public static class RememberLatestDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "state-remember-latest",
        CategoryId:  "state-effects",
        Title:       "RememberLatest",
        Description: "Capture the latest counter value from inside a long-lived LaunchedEffect closure via Compose.RememberLatest.",
        Build:       () =>
        {
            var counter = Compose.Remember(() => new MutableNumberState<int>(0));

            // Always reads the *latest* counter value, even when called
            // from inside the long-lived LaunchedEffect lambda below.
            var latest = Compose.RememberLatest(counter.Value);

            var lastSnapshot = Compose.Remember(() => new MutableNumberState<int>(0));

            return new Column
            {
                new Text($"counter (live):           {counter}"),
                new Text($"snapshot captured by loop: {lastSnapshot}"),

                new LaunchedEffect(key1: "snapshot-loop", async ct =>
                {
                    try
                    {
                        while (!ct.IsCancellationRequested)
                        {
                            await System.Threading.Tasks.Task.Delay(500, ct);
                            // The latest() delegate identity is stable across
                            // recompositions; invoking it returns the freshest
                            // counter value.
                            lastSnapshot.Value = latest();
                        }
                    }
                    catch (System.OperationCanceledException) { }
                }),

                new Spacer { Modifier = Modifier.Companion.Height(12) },
                new Row
                {
                    new Button(onClick: () => counter++) { new Text("counter++") },
                    new Spacer { Modifier = Modifier.Companion.Width(8) },
                    new Button(onClick: () => counter.Value = 0) { new Text("Reset") },
                },
            };
        });
}

