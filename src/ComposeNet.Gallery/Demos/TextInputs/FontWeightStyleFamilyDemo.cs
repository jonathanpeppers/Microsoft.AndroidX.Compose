using ComposeNet.Gallery.Registry;

namespace ComposeNet.Gallery.Demos.TextInputs;

/// <summary>FontWeight + FontStyle + FontFamily — covers the three font axes.</summary>
public static class FontWeightStyleFamilyDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "text-font-weight-style-family",
        CategoryId:  "text-inputs",
        Title:       "FontWeight / Style / Family",
        Description: "Weight (Light…Black), italic FontStyle, and Serif/Monospace families.",
        Build:       () => new Column
        {
            new Text("Light")     { FontWeight = ComposeNet.FontWeight.Light },
            new Text("Normal")    { FontWeight = ComposeNet.FontWeight.Normal },
            new Text("Medium")    { FontWeight = ComposeNet.FontWeight.Medium },
            new Text("SemiBold")  { FontWeight = ComposeNet.FontWeight.SemiBold },
            new Text("Bold")      { FontWeight = ComposeNet.FontWeight.Bold },
            new Text("Black")     { FontWeight = ComposeNet.FontWeight.Black },
            new Text("Italic")    { FontStyle  = ComposeNet.FontStyle.Italic },
            new Text("Serif")     { FontFamily = ComposeNet.FontFamily.Serif },
            new Text("Monospace") { FontFamily = ComposeNet.FontFamily.Monospace },
        });
}
