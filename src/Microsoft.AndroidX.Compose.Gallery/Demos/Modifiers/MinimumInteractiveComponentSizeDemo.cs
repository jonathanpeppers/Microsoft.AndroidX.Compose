using Microsoft.AndroidX.Compose.Gallery.Registry;

namespace Microsoft.AndroidX.Compose.Gallery.Demos.Modifiers;

/// <summary>
/// <c>Modifier.minimumInteractiveComponentSize()</c> — reserves a 48dp
/// touch target around a small composable. Two icon-only buttons sit
/// over a pastel background so the difference in reserved hit-area is
/// visible to the eye, not just the gesture recognizer.
/// </summary>
public static class MinimumInteractiveComponentSizeDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "modifiers-minimum-interactive-component-size",
        CategoryId:  "modifiers",
        Title:       "MinimumInteractiveComponentSize",
        Description: "Reserves 48dp around an icon-only button to keep its touch target accessible.",
        Build:       c =>
        {
            var taps = c.Remember(() => new MutableNumberState<int>(0));
            return new Column
            {
                new Text($"Tapped: {taps}"),
                new Text("Yellow tiles show the 48dp touch region reserved around each button."),
                new Row(horizontalArrangement: Arrangement.SpacedBy(24))
                {
                    new Column
                    {
                        new Text("Without"),
                        new Box
                        {
                            Modifier.Companion
                                .Background(Color.FromHex("#FFF59D")),
                            new IconButton(onClick: () => taps++)
                            {
                                Modifier.Companion.Size(24),
                                new Text("☆") { Color = Color.Black },
                            },
                        },
                    },
                    new Column
                    {
                        new Text("With"),
                        new Box
                        {
                            Modifier.Companion
                                .Background(Color.FromHex("#FFF59D")),
                            new IconButton(onClick: () => taps++)
                            {
                                Modifier.Companion
                                    .MinimumInteractiveComponentSize()
                                    .Size(24),
                                new Text("★") { Color = Color.Black },
                            },
                        },
                    },
                },
            };
        });
}
