using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.StateEffectsAnimation;

/// <summary>RememberSaveable — values survive Activity recreation (rotation, dark/light).</summary>
public static class RememberSaveableDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "state-remembersaveable",
        CategoryId:  "state-effects",
        Title:       "RememberSaveable",
        Description: "Values stay put across configuration changes (rotate the device, toggle dark mode).",
        Build:       c =>
        {
            var count = c.RememberSaveable(() => new MutableNumberState<int>(0));
            var name  = c.RememberSaveable(() => new MutableState<string>(""));

            return new Column
            {
                new Text($"Count: {count}  (rotate device — value persists)"),
                new Button(onClick: () => count++) { new Text("+1") },
                new TextField(name) { Placeholder = new Text("Type something") },
                new Text($"You typed: {name}"),
            };
        });
}
