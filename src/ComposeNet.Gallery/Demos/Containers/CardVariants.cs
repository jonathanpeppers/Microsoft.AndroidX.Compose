using ComposeNet.Gallery.Registry;

namespace ComposeNet.Gallery.Demos.Containers;

/// <summary>Card, ElevatedCard, OutlinedCard side by side.</summary>
public static class CardVariants
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "containers-card-variants",
        CategoryId:  "containers",
        Title:       "Card variants",
        Description: "Card (tonal), ElevatedCard (shadow), OutlinedCard (border).",
        Build:       () => new Column
        {
            new Card { new Text("Card (tonal)") },
            new ElevatedCard { new Text("ElevatedCard (shadow)") },
            new OutlinedCard { new Text("OutlinedCard (border)") },
        });
}
