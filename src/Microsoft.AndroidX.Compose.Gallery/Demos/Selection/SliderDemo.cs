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
        Description: "Continuous Slider mapped to a MutableNumberState<float>. The enabled toggle disables the slider; the second uses FloatRange(-10..10) and 4 internal stops.",
        Build:       c =>
        {
            var value   = c.MutableStateOf(0.4f);
            var stepped = c.MutableStateOf(0f);
            var enabled = c.MutableStateOf(true);
            return new Column
            {
                new Row(horizontalArrangement: Arrangement.SpacedBy(8), verticalAlignment: Alignment.Vertical.CenterVertically)
                {
                    new Switch(@checked: enabled.Value, onCheckedChange: v => enabled.Value = v),
                    new Text(enabled.Value ? "Enabled" : "Disabled"),
                },
                new Slider(value: value.Value, onValueChange: v => value.Value = v, enabled: enabled.Value),
                new Text($"Continuous: {value.Value:F2}"),
                new Slider(value: stepped.Value, onValueChange: v => stepped.Value = v, enabled: enabled.Value, steps: 4)
                {
                    ValueRange = new FloatRange(-10f, 10f),
                },
                new Text($"Stepped (-10..10): {stepped.Value:F2}"),
            };
        });
}
