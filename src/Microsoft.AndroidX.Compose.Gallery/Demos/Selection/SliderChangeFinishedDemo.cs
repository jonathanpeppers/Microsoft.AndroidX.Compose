using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.Selection;

/// <summary>
/// <see cref="Slider.OnValueChangeFinished"/> and
/// <see cref="RangeSlider.OnValueChangeFinished"/>: commit a value once a
/// change ends instead of on every <c>onValueChange</c>.
/// </summary>
public static class SliderChangeFinishedDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "selection-slider-change-finished",
        CategoryId:  "selection",
        Title:       "Slider change finished",
        Description: "OnValueChangeFinished runs when a drag or tap is released. The live value follows every onValueChange; the committed value and its count change only when a change ends.",
        Build:       c =>
        {
            var live        = c.MutableStateOf(0.5f);
            var committed   = c.MutableStateOf(0.5f);
            var finished    = c.MutableStateOf(0);
            var rangeStart  = c.MutableStateOf(2f);
            var rangeEnd    = c.MutableStateOf(8f);
            var committedRange = c.MutableStateOf(new FloatRange(2f, 8f));
            var rangeFinished  = c.MutableStateOf(0);
            return new Column
            {
                new Text("Slider"),
                new Slider(value: live.Value, onValueChange: v => live.Value = v)
                {
                    OnValueChangeFinished = () =>
                    {
                        committed.Value = live.Value;
                        finished.Value++;
                    },
                },
                new Text($"Live: {live.Value:F2}"),
                new Text($"Committed: {committed.Value:F2} (finished {finished.Value} times)"),
                new Text("RangeSlider (0..10)"),
                new RangeSlider(
                    value: new FloatRange(rangeStart.Value, rangeEnd.Value),
                    onValueChange: r =>
                    {
                        rangeStart.Value = r.Start;
                        rangeEnd.Value   = r.End;
                    })
                {
                    ValueRange = new FloatRange(0f, 10f),
                    OnValueChangeFinished = () =>
                    {
                        committedRange.Value = new FloatRange(rangeStart.Value, rangeEnd.Value);
                        rangeFinished.Value++;
                    },
                },
                new Text($"Live: {rangeStart.Value:F2} - {rangeEnd.Value:F2}"),
                new Text($"Committed: {committedRange.Value.Start:F2} - {committedRange.Value.End:F2} (finished {rangeFinished.Value} times)"),
            };
        });
}
