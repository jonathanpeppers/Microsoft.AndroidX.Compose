using Microsoft.AndroidX.Compose.Gallery.Registry;

namespace Microsoft.AndroidX.Compose.Gallery.Demos.Theming;

/// <summary>
/// Wraps a nested <c>MaterialTheme</c> with a custom palette built via
/// <c>MaterialTheme.LightColorScheme(...)</c> and shows components
/// picking up the override.
/// </summary>
public static class CustomColorSchemeDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "theming-color-scheme",
        CategoryId:  "theming",
        Title:       "Custom ColorScheme",
        Description: "MaterialTheme.LightColorScheme(primary, secondary, tertiary, ...) — set just the slots you care about; the rest fall back to the Material 3 baseline.",
        Build:       _ =>
        {
            // Build the nested MaterialTheme separately so we don't mix
            // property assignments with collection-init items in one
            // initializer block (C# disallows that — CS0747).
            var themed = new MaterialTheme
            {
                ColorScheme = MaterialTheme.LightColorScheme(
                    primary:              Color.FromRgb(0x00, 0x79, 0x6B),
                    onPrimary:            Color.White,
                    primaryContainer:     Color.FromRgb(0x80, 0xCB, 0xC4),
                    onPrimaryContainer:   Color.Black,
                    secondary:            Color.FromRgb(0xFF, 0xA0, 0x00),
                    onSecondary:          Color.Black,
                    secondaryContainer:   Color.FromRgb(0xFF, 0xE0, 0xB2),
                    onSecondaryContainer: Color.Black,
                    tertiary:             Color.FromRgb(0xC2, 0x18, 0x5B),
                    onTertiary:           Color.White,
                    outline:              Color.FromRgb(0x00, 0x79, 0x6B)),
                UseDynamicColor = false,
            };
            themed.Add(new Row(horizontalArrangement: Arrangement.SpacedBy(8))
            {
                new Button(onClick: () => { })            { new Text("Primary") },
                new FilledTonalButton(onClick: () => { }) { new Text("Tonal") },
                new OutlinedButton(onClick: () => { })    { new Text("Outline") },
            });

            return new Column(verticalArrangement: Arrangement.SpacedBy(12))
            {
                new Text("Default M3 baseline:"),
                new Row(horizontalArrangement: Arrangement.SpacedBy(8))
                {
                    new Button(onClick: () => { })            { new Text("Primary") },
                    new FilledTonalButton(onClick: () => { }) { new Text("Tonal") },
                    new OutlinedButton(onClick: () => { })    { new Text("Outline") },
                },
                new Text("Custom palette (teal primary, amber secondary, magenta tertiary):"),
                themed,
            };
        });
}
