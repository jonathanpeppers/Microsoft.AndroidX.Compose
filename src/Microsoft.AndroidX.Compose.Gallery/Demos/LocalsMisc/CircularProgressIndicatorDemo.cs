using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.LocalsMisc;

/// <summary>CircularProgressIndicator determinate and indeterminate modes.</summary>
public static class CircularProgressIndicatorDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "misc-circular-progress",
        CategoryId:  "locals-misc",
        Title:       "CircularProgressIndicator",
        Description: "Determinate progress with live updates alongside the indeterminate animation.",
        Build:       c =>
        {
            var progress = c.MutableStateOf(0.25f);
            var determinate = new CircularProgressIndicator
            {
                StrokeWidthDp = 6,
            };
            return new Composed(_ =>
            {
                determinate.Progress = progress.Value;
                return new Column
                {
                    new Text($"Determinate: {progress.Value:P0}"),
                    determinate,
                    new Row(horizontalArrangement: Arrangement.SpacedBy(8))
                    {
                        new Button(onClick: () => progress.Value = Math.Max(0f, progress.Value - 0.1f))
                        {
                            new Text("-10%"),
                        },
                        new Button(onClick: () => progress.Value = Math.Min(1f, progress.Value + 0.1f))
                        {
                            new Text("+10%"),
                        },
                    },
                    new Text("Indeterminate:"),
                    new CircularProgressIndicator
                    {
                        StrokeWidthDp = 6,
                    },
                };
            });
        });
}
