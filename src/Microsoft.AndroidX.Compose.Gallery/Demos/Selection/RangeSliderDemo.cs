using Microsoft.AndroidX.Compose.Gallery.Registry;

namespace Microsoft.AndroidX.Compose.Gallery.Demos.Selection;

/// <summary>Two-thumb RangeSlider for selecting a (start, end) interval.</summary>
public static class RangeSliderDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "selection-range-slider",
        CategoryId:  "selection",
        Title:       "RangeSlider",
        Description: "Two-thumb slider for a (start, end) interval.",
        Build:       () =>
        {
            var start = ComposeRuntime.Remember(() => new MutableNumberState<float>(0.2f));
            var end   = ComposeRuntime.Remember(() => new MutableNumberState<float>(0.8f));
            return new Column
            {
                new RangeSlider(
                    value: (start.Value, end.Value),
                    onValueChange: r =>
                    {
                        start.Value = r.Start;
                        end.Value   = r.End;
                    }),
                new Text($"Range: {start.Value:F2} – {end.Value:F2}"),
            };
        });
}
