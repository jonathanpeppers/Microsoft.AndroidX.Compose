using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.TextInputs;

/// <summary>Bundled font matching, loading strategies, and light/dark text rendering.</summary>
public static class ResourceFontsDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id: "text-resource-fonts",
        CategoryId: "text-inputs",
        Title: "Resource fonts",
        Description: "Bundled Karla regular/bold, real italic, and Montserrat; light/dark and GC stress.",
        Build: c =>
        {
            var family = c.Remember(() => FontFamily.FromFonts(
                Font.Resource(Resource.Font.karla_regular),
                Font.Resource(Resource.Font.karla_bold, FontWeight.Bold),
                Font.Resource(Resource.Font.karla_italic, style: FontStyle.Italic)));
            var montserrat = c.Remember(() => FontFamily.FromFonts(
                Font.Resource(Resource.Font.montserrat_regular,
                    loadingStrategy: FontLoadingStrategy.OptionalLocal)));
            var asyncFamily = c.Remember(() => FontFamily.FromFonts(
                Font.Resource(Resource.Font.karla_bold, FontWeight.Bold,
                    loadingStrategy: FontLoadingStrategy.Async)));
            var passes = c.Remember(() => new MutableNumberState<int>(0));
            return new Column
            {
                new Text($"Render / GC cycles: {passes.Value}"),
                new Button(() =>
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    Java.Lang.JavaSystem.Gc();
                    passes.Value++;
                }) { new Text("Collect and re-render") },
                Sample(family, montserrat, Color.White, Color.Black, "Light"),
                Sample(family, montserrat, Color.Black, Color.White, "Dark"),
                new Text("Async Karla bold (may reflow)")
                {
                    FontFamily = asyncFamily,
                    FontWeight = FontWeight.Bold,
                },
            };
        });

    static Column Sample(FontFamily family, FontFamily montserrat, Color background, Color foreground, string label) =>
        new()
        {
            Modifier.FillMaxWidth().Background(background).Padding(12),
            new Text($"{label}: Karla regular") { FontFamily = family, FontSize = 22, Color = foreground },
            new Text("Karla bold") { FontFamily = family, FontWeight = FontWeight.Bold, FontSize = 22, Color = foreground },
            new Text("Karla italic") { FontFamily = family, FontStyle = FontStyle.Italic, FontSize = 22, Color = foreground },
            new Text("Montserrat optional-local") { FontFamily = montserrat, FontSize = 20, Color = foreground },
        };
}
