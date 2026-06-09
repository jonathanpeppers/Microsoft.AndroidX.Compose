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
        Description: "Continuous Slider mapped to a MutableNumberState<float>.",
        Build:       c =>
        {
            var value = c.Remember(() => new MutableNumberState<float>(0.4f));
            return new Column
            {
                new Slider(value: value.Value, onValueChange: v => value.Value = v),
                new Text($"Value: {value.Value:F2}"),
            };
        });
}
