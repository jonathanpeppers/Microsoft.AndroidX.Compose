using ComposeNet.Gallery.Registry;

namespace ComposeNet.Gallery.Demos.StateEffectsAnimation;

/// <summary>ProduceState — an async producer drives a State&lt;T&gt;; restarts on key change.</summary>
public static class ProduceStateTickerDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "state-producestate-ticker",
        CategoryId:  "state-effects",
        Title:       "ProduceState ticker",
        Description: "A 1Hz tick loop publishes into a Compose State. Bumping the seed cancels the current loop and restarts it from 0.",
        Build:       () =>
        {
            var seed   = Compose.Remember(() => new MutableNumberState<int>(0));
            var ticker = Compose.ProduceState<int>(0, seed.Value, async (state, ct) =>
            {
                state.Value = 0;
                while (!ct.IsCancellationRequested)
                {
                    try { await System.Threading.Tasks.Task.Delay(1000, ct); }
                    catch (System.OperationCanceledException) { return; }
                    state.Value++;
                }
            });

            return new Column
            {
                new Text($"Seed: {seed}"),
                new Text($"Ticker: {ticker.Value}s"),
                new Button(onClick: () => seed++) { new Text("New seed (restarts ticker)") },
            };
        });
}
