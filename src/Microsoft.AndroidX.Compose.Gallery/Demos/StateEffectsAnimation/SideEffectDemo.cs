using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.StateEffectsAnimation;

/// <summary>SideEffect — runs after every successful recomposition; logs to logcat.</summary>
public static class SideEffectDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "effects-sideeffect",
        CategoryId:  "state-effects",
        Title:       "SideEffect",
        Description: $"Writes a log line after every successful recomposition. Filter logcat for '{GalleryApp.LogTag}' to see it.",
        Build:       c =>
        {
            var count = c.Remember(() => new MutableNumberState<int>(0));
            return new Column
            {
                new Text($"Count: {count}"),
                new Text($"See logcat (filter: {GalleryApp.LogTag}) — one line per recomposition."),
                new SideEffect(() =>
                    Android.Util.Log.Debug(GalleryApp.LogTag,
                        $"SideEffect ran (count={count.Value})")),
                new Button(onClick: () => count++) { new Text("+1 (recompose)") },
            };
        });
}
