using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.Theming;

/// <summary>
/// Builds custom M3 type slots via <c>AndroidX.Compose.TextStyle</c> +
/// <c>MaterialTheme.BuildTypography(...)</c>. Material 3 components
/// internally consume specific slots (e.g. <c>Button</c> uses
/// <c>labelLarge</c>); overriding the slot changes the rendered text.
/// </summary>
public static class CustomTypographyDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "theming-typography",
        CategoryId:  "theming",
        Title:       "Custom Typography",
        Description: "Build TextStyle instances and wire them into MaterialTheme.BuildTypography(...). Components that read MaterialTheme.typography.labelLarge (Button, Chip, FAB) pick up the override automatically.",
        Build:       _ =>
        {
            var boldWide = new MaterialTheme
            {
                Typography = MaterialTheme.BuildTypography(
                    labelLarge: new TextStyle
                    {
                        FontWeight    = FontWeight.Bold,
                        FontSize      = new Sp(18),
                        LetterSpacing = new Sp(2),
                    }),
                UseDynamicColor = false,
            };
            boldWide.Add(new Column(verticalArrangement: Arrangement.SpacedBy(8))
            {
                new Button(onClick: () => { })            { new Text("Click me") },
                new FilledTonalButton(onClick: () => { }) { new Text("Tonal") },
                new AssistChip(onClick: () => { })        { Label = new Text("Chip text") },
            });

            var italicCondensed = new MaterialTheme
            {
                Typography = MaterialTheme.BuildTypography(
                    labelLarge: new TextStyle
                    {
                        FontStyle     = FontStyle.Italic,
                        FontSize      = new Sp(12),
                        LetterSpacing = new Sp(0),
                    }),
                UseDynamicColor = false,
            };
            italicCondensed.Add(new Button(onClick: () => { }) { new Text("Condensed italic") });

            return new Column(verticalArrangement: Arrangement.SpacedBy(12))
            {
                new Text("Default M3 Button text (labelLarge):"),
                new Button(onClick: () => { }) { new Text("Click me") },
                new Text("Override labelLarge → Bold, 18 sp, wide tracking:"),
                boldWide,
                new Text("Override labelLarge → Italic, condensed (smaller + tight tracking):"),
                italicCondensed,
            };
        });
}
