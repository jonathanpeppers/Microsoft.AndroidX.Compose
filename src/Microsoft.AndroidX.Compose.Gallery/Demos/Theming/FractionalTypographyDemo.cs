using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.Theming;

/// <summary>Exercises fractional font sizes, line heights, and positive/negative letter spacing.</summary>
public static class FractionalTypographyDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "theming-fractional-typography",
        CategoryId:  "theming",
        Title:       "Fractional typography",
        Description: "Float Sp values retain Kotlin TextUnit precision, including subpixel and negative tracking.",
        Build:       _ =>
        {
            var labelTheme = new MaterialTheme
            {
                Typography = MaterialTheme.BuildTypography(
                    labelLarge: new TextStyle
                    {
                        FontSize = 14,
                        LineHeight = 20,
                        LetterSpacing = 0.1f.Sp(),
                        FontWeight = FontWeight.SemiBold,
                    }),
            };
            labelTheme.Add(new Button(onClick: () => { }) { new Text("Send") });
            return new Column(verticalArrangement: Arrangement.SpacedBy(8.Dp()))
            {
                new Text("Font 16.25 sp / line height 24.75 sp"),
                new Text("Fractional type\nkeeps its precision")
                {
                    FontSize = new Sp(16.25f),
                    LineHeight = 24.75f.Sp(),
                },
                new Text("Same text, 0 / +0.5 / -0.5 sp tracking:"),
                new Text("Compose typography") { FontSize = 20, LetterSpacing = 0f.Sp() },
                new Text("Compose typography") { FontSize = 20, LetterSpacing = 0.5f.Sp() },
                new Text("Compose typography") { FontSize = 20, LetterSpacing = (-0.5f).Sp() },
                new Text("Jetchat labelSmall: 11 / 16 / +0.5 sp"),
                new Text("Today")
                {
                    FontSize = 11,
                    LineHeight = 16,
                    LetterSpacing = 0.5f.Sp(),
                    FontWeight = FontWeight.SemiBold,
                },
                new Text("Jetchat labelLarge: 14 / 20 / +0.1 sp"),
                labelTheme,
            };
        });
}
