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
            var enabled = c.MutableStateOf(true);
            return new Column
            {
                new Text($"Tapped: {count}"),
                new Row(horizontalArrangement: Arrangement.SpacedBy(8.Dp()),
                        verticalAlignment: Alignment.Vertical.CenterVertically)
                {
                    new Switch(@checked: enabled.Value, onCheckedChange: value => enabled.Value = value),
                    new Text("Enabled"),
                },
                new Button(onClick: () => count++, enabled: enabled.Value) { new Text("Filled") },
                new ElevatedButton(onClick: () => count++, enabled: enabled.Value) { new Text("Elevated") },
                new FilledTonalButton(onClick: () => count++, enabled: enabled.Value) { new Text("Filled tonal") },
                new OutlinedButton(onClick: () => count++, enabled: enabled.Value) { new Text("Outlined") },
                new TextButton(onClick: () => count++, enabled: enabled.Value) { new Text("Text") },
            };
        });
}
