using Microsoft.AndroidX.Compose.Gallery.Registry;

namespace Microsoft.AndroidX.Compose.Gallery.Demos.TextInputs;

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
            new Text("Light")     { FontWeight = FontWeight.Light },
            new Text("Normal")    { FontWeight = FontWeight.Normal },
            new Text("Medium")    { FontWeight = FontWeight.Medium },
            new Text("SemiBold")  { FontWeight = FontWeight.SemiBold },
            new Text("Bold")      { FontWeight = FontWeight.Bold },
            new Text("Black")     { FontWeight = FontWeight.Black },
            new Text("Italic")    { FontStyle  = FontStyle.Italic },
            new Text("Serif")     { FontFamily = FontFamily.Serif },
            new Text("Monospace") { FontFamily = FontFamily.Monospace },
        });
}
