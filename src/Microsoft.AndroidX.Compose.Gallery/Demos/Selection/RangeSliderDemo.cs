using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.Selection;

/// <summary>Two-thumb RangeSlider using managed FloatRange values.</summary>
public static class RangeSliderDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "selection-range-slider",
        CategoryId:  "selection",
        Title:       "RangeSlider",
        Description: "Two-thumb slider selecting a FloatRange within an overall 0..10 range.",
        Build:       c =>
        {
            var start = c.MutableStateOf(2f);
            var end   = c.MutableStateOf(8f);
            return new Column
            {
                new RangeSlider(
                    value: new FloatRange(start.Value, end.Value),
                    onValueChange: r =>
                    {
                        start.Value = r.Start;
                        end.Value   = r.End;
                    })
                {
                    ValueRange = new FloatRange(0f, 10f),
                },
                new Text($"Range: {start.Value:F2} - {end.Value:F2}"),
            };
        });
}
