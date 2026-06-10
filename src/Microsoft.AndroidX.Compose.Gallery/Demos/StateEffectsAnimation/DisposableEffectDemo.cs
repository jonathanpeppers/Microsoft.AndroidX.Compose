using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.StateEffectsAnimation;

/// <summary>DisposableEffect — register/cleanup pair tied to a key.</summary>
public static class DisposableEffectDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "effects-disposableeffect",
        CategoryId:  "state-effects",
        Title:       "DisposableEffect",
        Description: "Fake 'register external listener' pattern. The cleanup callback bumps a counter on every key change / leaving composition.",
        Build:       c =>
        {
            var cleanups = c.Remember(() => new MutableNumberState<int>(0));
            var key      = c.Remember(() => new MutableNumberState<int>(0));

            return new Column
            {
                new Text($"Cleanups so far: {cleanups}"),
                new Text($"Effect key: {key}"),
                new DisposableEffect(key.Value, scope => () => cleanups.Value++),
                new Button(onClick: () => key++) { new Text("Change key (triggers cleanup)") },
                new Button(onClick: () => cleanups.Value = 0) { new Text("Reset counter") },
            };
        });
}
