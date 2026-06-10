using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.Buttons;

/// <summary>The five Material 3 filled-style button variants side by side.</summary>
public static class FillStylesDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "buttons-fill-styles",
        CategoryId:  "buttons",
        Title:       "Fill styles",
        Description: "Filled, Elevated, Filled tonal, Outlined, and Text buttons.",
        Build:       c =>
        {
            var count = c.MutableStateOf(0);
            return new Column
            {
                new Text($"Tapped: {count}"),
                new Button(onClick: () => count++) { new Text("Filled") },
                new ElevatedButton(onClick: () => count++) { new Text("Elevated") },
                new FilledTonalButton(onClick: () => count++) { new Text("Filled tonal") },
                new OutlinedButton(onClick: () => count++) { new Text("Outlined") },
                new TextButton(onClick: () => count++) { new Text("Text") },
            };
        });
}
