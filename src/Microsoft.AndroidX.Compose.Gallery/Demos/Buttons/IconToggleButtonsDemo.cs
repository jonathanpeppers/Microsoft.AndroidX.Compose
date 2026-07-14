using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.Buttons;

/// <summary>Two-state icon buttons that swap glyph when checked.</summary>
public static class IconToggleButtonsDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "buttons-icon-toggle",
        CategoryId:  "buttons",
        Title:       "Icon toggle buttons",
        Description: "IconToggleButton + filled/tonal/outlined variants.",
        Build:       c =>
        {
            var a = c.MutableStateOf(false);
            var b = c.MutableStateOf(false);
            var ct = c.MutableStateOf(false);
            var d = c.MutableStateOf(false);
            var enabled = c.MutableStateOf(true);
            return new Column
            {
                new Row(horizontalArrangement: Arrangement.SpacedBy(8.Dp()),
                        verticalAlignment: Alignment.Vertical.CenterVertically)
                {
                    new Switch(@checked: enabled.Value, onCheckedChange: value => enabled.Value = value),
                    new Text("Enabled"),
                },
                new Row
                {
                    new IconToggleButton(@checked: a.Value, onCheckedChange: v => a.Value = v, enabled: enabled.Value)
                        { new Text(a.Value ? "★" : "☆") },
                    new FilledIconToggleButton(@checked: b.Value, onCheckedChange: v => b.Value = v, enabled: enabled.Value)
                        { new Text(b.Value ? "★" : "☆") },
                    new FilledTonalIconToggleButton(@checked: ct.Value, onCheckedChange: v => ct.Value = v, enabled: enabled.Value)
                        { new Text(ct.Value ? "◆" : "◇") },
                    new OutlinedIconToggleButton(@checked: d.Value, onCheckedChange: v => d.Value = v, enabled: enabled.Value)
                        { new Text(d.Value ? "◆" : "◇") },
                },
            };
        });
}
