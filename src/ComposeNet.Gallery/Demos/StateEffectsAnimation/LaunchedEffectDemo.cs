using ComposeNet.Gallery.Registry;

namespace ComposeNet.Gallery.Demos.StateEffectsAnimation;

/// <summary>LaunchedEffect — launches a Task tied to composition; cancels on key change / dispose.</summary>
public static class LaunchedEffectDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "effects-launchedeffect",
        CategoryId:  "state-effects",
        Title:       "LaunchedEffect",
        Description: "Background tick loop scoped to this composition. Bumping the key cancels and restarts it.",
        Build:       () =>
        {
            var ticks = Compose.Remember(() => new MutableNumberState<int>(0));
            var key   = Compose.Remember(() => new MutableNumberState<int>(0));

            return new Column
            {
                new Text($"Ticks: {ticks}"),
                new Text($"Effect key: {key}"),
                new LaunchedEffect(key.Value, async ct =>
                {
                    try
                    {
                        while (!ct.IsCancellationRequested)
                        {
                            await System.Threading.Tasks.Task.Delay(1000, ct);
                            ticks.Value++;
                        }
                    }
                    catch (System.OperationCanceledException) { }
                }),
                new Button(onClick: () => key++) { new Text("Restart (key++)") },
                new Button(onClick: () => ticks.Value = 0) { new Text("Reset ticks") },
            };
        });
}
