using AndroidX.Compose.Runtime;
using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.LocalsMisc;

/// <summary>Custom typed CompositionLocal&lt;T&gt; — provide an override and watch a nested subtree pick it up.</summary>
public static class CustomCompositionLocalDemo
{
    public record Palette(string Name, long PrimaryHex);

    static readonly Palette DefaultPalette  = new("Default (M3 purple)", 0xFF6750A4);
    static readonly Palette OverridePalette = new("Override (pink)",    0xFFE91E63);

    static readonly CompositionLocal<Palette> LocalPalette =
        CompositionLocal.Of(() => DefaultPalette);

    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "locals-custom",
        CategoryId:  "locals-misc",
        Title:       "Custom CompositionLocal<Palette>",
        Description: "Outer reads return the default; CompositionLocalProvider with LocalPalette.Provides(override) flips the override on for the wrapped subtree; siblings after the provider revert.",
        Build:       _ => new Column
        {
            new Text("Outer (no provider) — default palette:"),
            new ThemeLabel(),

            new Text("Inner (CompositionLocalProvider overrides):"),
            new CompositionLocalProvider
            {
                LocalPalette.Provides(OverridePalette),
                new Column
                {
                    Modifier.Companion.Padding(8),
                    new ThemeLabel(),
                    new ThemeLabel(),
                },
            },

            new Text("Sibling after provider — back to default:"),
            new ThemeLabel(),
        });

    sealed class ThemeLabel : ComposableNode
    {
        public override void Render(IComposer composer)
        {
            var palette = LocalPalette.GetCurrent(composer);
            new Text($"  Palette: {palette.Name} (0x{palette.PrimaryHex:X8})").Render(composer);
        }
    }
}
