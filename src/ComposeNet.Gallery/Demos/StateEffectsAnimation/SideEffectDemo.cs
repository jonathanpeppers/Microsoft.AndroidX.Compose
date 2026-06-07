using ComposeNet.Gallery.Registry;

namespace ComposeNet.Gallery.Demos.StateEffectsAnimation;

/// <summary>SideEffect — runs after every successful recomposition; logs to logcat.</summary>
public static class SideEffectDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "effects-sideeffect",
        CategoryId:  "state",
        Title:       "SideEffect",
        Description: "Writes a log line after every successful recomposition. Filter logcat for 'ComposeNet.Gallery' to see it.",
        Build:       () =>
        {
            var count = Compose.Remember(() => new MutableNumberState<int>(0));
            return new Column
            {
                new Text($"Count: {count}"),
                new Text("See logcat (filter: ComposeNet.Gallery) — one line per recomposition."),
                new SideEffect(() =>
                    Android.Util.Log.Debug("ComposeNet.Gallery",
                        $"SideEffect ran (count={count.Value})")),
                new Button(onClick: () => count++) { new Text("+1 (recompose)") },
            };
        });
}
