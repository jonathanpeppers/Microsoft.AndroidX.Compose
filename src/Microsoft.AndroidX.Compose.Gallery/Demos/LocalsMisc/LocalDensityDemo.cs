using AndroidX.Compose.Runtime;
using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.LocalsMisc;

/// <summary>
/// Reads <see cref="LocalDensity"/> and provides an app-controlled font scale
/// and density to a subtree through <see cref="CompositionLocalProvider"/>.
/// </summary>
public static class LocalDensityDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "locals-density",
        CategoryId:  "locals-misc",
        Title:       "LocalDensity",
        Description: "Provides an app text scale (0.85x to 1.5x of the inherited font scale) and an optional 1.5x density to the wrapped subtree. Its text and 48dp tile resize; the sibling after the provider keeps the inherited density.",
        Build:       c =>
        {
            var textScale = c.MutableStateOf(1f);
            var largerDensity = c.MutableStateOf(false);
            var inherited = LocalDensity.Current(c);
            float density = largerDensity.Value ? inherited.Density * 1.5f : inherited.Density;
            float fontScale = inherited.FontScale * textScale.Value;
            return new Column
            {
                new Text("Inherited:"),
                new DensityLabel(),
                new Slider(value: textScale.Value, onValueChange: v => textScale.Value = v)
                {
                    ValueRange = new FloatRange(0.85f, 1.5f),
                },
                new Text($"App text scale: {textScale.Value:F2}x"),
                new Row(horizontalArrangement: Arrangement.SpacedBy(8), verticalAlignment: Alignment.Vertical.CenterVertically)
                {
                    new Switch(@checked: largerDensity.Value, onCheckedChange: v => largerDensity.Value = v),
                    new Text("Provide 1.5x density"),
                },
                new Text("Inside CompositionLocalProvider:"),
                new CompositionLocalProvider
                {
                    LocalDensity.Provides(density, fontScale),
                    new Column
                    {
                        Modifier.Padding(8),
                        new DensityLabel(),
                        new Text("16sp body text") { FontSize = 16 },
                        new Box { Modifier.Size(48).Background(Color.Blue) },
                        new Text("48dp tile"),
                    },
                },
                new Text("Sibling after the provider:"),
                new DensityLabel(),
                new Text("16sp body text") { FontSize = 16 },
            };
        });

    sealed class DensityLabel : ComposableNode
    {
        public override void Render(IComposer composer)
        {
            var density = LocalDensity.Current(composer);
            new Text($"  LocalDensity: density {density.Density:F2}, fontScale {density.FontScale:F2}").Render(composer);
        }
    }
}
