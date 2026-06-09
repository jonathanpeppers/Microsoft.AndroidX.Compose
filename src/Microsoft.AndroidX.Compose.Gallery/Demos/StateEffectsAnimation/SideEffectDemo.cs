using Microsoft.AndroidX.Compose.Gallery.Registry;

namespace Microsoft.AndroidX.Compose.Gallery.Demos.StateEffectsAnimation;

/// <summary>SideEffect — runs after every successful recomposition; logs to logcat.</summary>
public static class SideEffectDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "effects-sideeffect",
        CategoryId:  "state-effects",
        Title:       "SideEffect",
        Description: "Writes a log line after every successful recomposition. Filter logcat for 'Microsoft.AndroidX.Compose.Gallery' to see it.",
        Build:       () =>
        {
            var count = ComposeRuntime.Remember(() => new MutableNumberState<int>(0));
            return new Column
            {
                new Text($"Count: {count}"),
                new Text("See logcat (filter: Microsoft.AndroidX.Compose.Gallery) — one line per recomposition."),
                new SideEffect(() =>
                    global::Android.Util.Log.Debug("Microsoft.AndroidX.Compose.Gallery",
                        $"SideEffect ran (count={count.Value})")),
                new Button(onClick: () => count++) { new Text("+1 (recompose)") },
            };
        });
}
