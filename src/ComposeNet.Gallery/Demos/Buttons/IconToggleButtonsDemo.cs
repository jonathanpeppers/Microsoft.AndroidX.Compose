using ComposeNet.Gallery.Registry;

namespace ComposeNet.Gallery.Demos.Buttons;

/// <summary>Two-state icon buttons that swap glyph when checked.</summary>
public static class IconToggleButtonsDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "buttons-icon-toggle",
        CategoryId:  "buttons",
        Title:       "Icon toggle buttons",
        Description: "IconToggleButton + filled/tonal/outlined variants.",
        Build:       () =>
        {
            var a = Compose.Remember(() => new MutableState<bool>(false));
            var b = Compose.Remember(() => new MutableState<bool>(false));
            var c = Compose.Remember(() => new MutableState<bool>(false));
            var d = Compose.Remember(() => new MutableState<bool>(false));
            return new Row
            {
                new IconToggleButton(@checked: a.Value, onCheckedChange: v => a.Value = v)
                    { new Text(a.Value ? "★" : "☆") },
                new FilledIconToggleButton(@checked: b.Value, onCheckedChange: v => b.Value = v)
                    { new Text(b.Value ? "★" : "☆") },
                new FilledTonalIconToggleButton(@checked: c.Value, onCheckedChange: v => c.Value = v)
                    { new Text(c.Value ? "◆" : "◇") },
                new OutlinedIconToggleButton(@checked: d.Value, onCheckedChange: v => d.Value = v)
                    { new Text(d.Value ? "◆" : "◇") },
            };
        });
}
