using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.Buttons;

/// <summary>Icon-button fill variants: standard, filled, tonal, outlined.</summary>
public static class IconButtonsDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "buttons-icon-buttons",
        CategoryId:  "buttons",
        Title:       "Icon buttons",
        Description: "IconButton, FilledIconButton, FilledTonalIconButton, OutlinedIconButton.",
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
                new Row
                {
                    new IconButton(onClick: () => count++, enabled: enabled.Value) { new Text("☆") },
                    new FilledIconButton(onClick: () => count++, enabled: enabled.Value) { new Text("★") },
                    new FilledTonalIconButton(onClick: () => count++, enabled: enabled.Value) { new Text("◆") },
                    new OutlinedIconButton(onClick: () => count++, enabled: enabled.Value) { new Text("◇") },
                },
            };
        });
}
