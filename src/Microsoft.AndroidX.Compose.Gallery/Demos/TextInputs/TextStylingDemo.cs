using Microsoft.AndroidX.Compose.Gallery.Registry;

namespace Microsoft.AndroidX.Compose.Gallery.Demos.TextInputs;

/// <summary>Bold, italic, size, decoration, spacing — typed Text properties.</summary>
public static class TextStylingDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "text-styling",
        CategoryId:  "text-inputs",
        Title:       "Text styling",
        Description: "FontSize, FontWeight, Decoration, LetterSpacing, LineHeight.",
        Build:       () => new Column
        {
            new Text("Large + Bold")
            {
                FontSize   = 24,
                FontWeight = FontWeight.Bold,
            },
            new Text("Medium underline")
            {
                FontSize   = 16,
                FontWeight = FontWeight.Medium,
                Decoration = TextDecoration.Underline,
            },
            new Text("Light strikethrough")
            {
                FontSize   = 14,
                FontWeight = FontWeight.Light,
                Decoration = TextDecoration.LineThrough,
            },
            new Text("Wide letter spacing, taller lines — rendered glyphs visibly drift apart and rows breathe.")
            {
                FontSize      = 14,
                LetterSpacing = 2,
                LineHeight    = 22,
                Modifier      = Modifier.Companion.Padding(8),
            },
        });
}
