using Microsoft.AndroidX.Compose.Gallery.Registry;

namespace Microsoft.AndroidX.Compose.Gallery.Demos.Containers;

/// <summary>Card, ElevatedCard, OutlinedCard side by side.</summary>
public static class CardVariantsDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "containers-card-variants",
        CategoryId:  "containers",
        Title:       "Card variants",
        Description: "Card (tonal), ElevatedCard (shadow), OutlinedCard (border).",
        Build:       () => new Column(verticalArrangement: Arrangement.SpacedBy(12))
        {
            Modifier.Companion.FillMaxWidth(),
            new Card
            {
                Modifier.Companion.FillMaxWidth(),
                new Column
                {
                    Modifier.Companion.Padding(16),
                    new Text("Card (tonal)"),
                    new Text("Default Material 3 Card — uses surfaceVariant for the background."),
                },
            },
            new ElevatedCard
            {
                Modifier.Companion.FillMaxWidth(),
                new Column
                {
                    Modifier.Companion.Padding(16),
                    new Text("ElevatedCard (shadow)"),
                    new Text("Lifts off the surface with a soft drop shadow."),
                },
            },
            new OutlinedCard
            {
                Modifier.Companion.FillMaxWidth(),
                new Column
                {
                    Modifier.Companion.Padding(16),
                    new Text("OutlinedCard (border)"),
                    new Text("Stroked outline, no fill or elevation."),
                },
            },
        });
}
