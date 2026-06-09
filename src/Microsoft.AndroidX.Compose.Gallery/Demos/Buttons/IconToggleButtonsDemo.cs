using Microsoft.AndroidX.Compose.Gallery.Registry;

namespace Microsoft.AndroidX.Compose.Gallery.Demos.Buttons;

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
            var a = c.Remember(() => new MutableState<bool>(false));
            var b = c.Remember(() => new MutableState<bool>(false));
            var ct = c.Remember(() => new MutableState<bool>(false));
            var d = c.Remember(() => new MutableState<bool>(false));
            return new Row
            {
                new IconToggleButton(@checked: a.Value, onCheckedChange: v => a.Value = v)
                    { new Text(a.Value ? "★" : "☆") },
                new FilledIconToggleButton(@checked: b.Value, onCheckedChange: v => b.Value = v)
                    { new Text(b.Value ? "★" : "☆") },
                new FilledTonalIconToggleButton(@checked: ct.Value, onCheckedChange: v => ct.Value = v)
                    { new Text(ct.Value ? "◆" : "◇") },
                new OutlinedIconToggleButton(@checked: d.Value, onCheckedChange: v => d.Value = v)
                    { new Text(d.Value ? "◆" : "◇") },
            };
        });
}
