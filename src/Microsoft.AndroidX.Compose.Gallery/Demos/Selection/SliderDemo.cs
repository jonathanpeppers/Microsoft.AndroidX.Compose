using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.Selection;

/// <summary>Continuous Slider bound to a float.</summary>
public static class SliderDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "selection-slider",
        CategoryId:  "selection",
        Title:       "Slider",
        Description: "Continuous Slider mapped to a MutableNumberState<float>. The enabled toggle disables the slider; steps = 4 snaps the second one to 5 discrete positions.",
        Build:       c =>
        {
            var value   = c.MutableStateOf(0.4f);
            var stepped = c.MutableStateOf(0.5f);
            var enabled = c.MutableStateOf(true);
            return new Column
            {
                new Row
                {
                    new Switch(@checked: enabled.Value, onCheckedChange: v => enabled.Value = v),
                    new Text(enabled.Value ? "Enabled" : "Disabled"),
                },
                new Slider(value: value.Value, onValueChange: v => value.Value = v, enabled: enabled.Value),
                new Text($"Continuous: {value.Value:F2}"),
                new Slider(value: stepped.Value, onValueChange: v => stepped.Value = v, enabled: enabled.Value, steps: 4),
                new Text($"Stepped (4 internal stops): {stepped.Value:F2}"),
            };
        });
}
