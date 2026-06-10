using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.TextInputs;

/// <summary>
/// <see cref="TextField.TextStyle"/> — override the text appearance
/// (color, size, weight) without touching the field chrome. Exercises
/// the <c>TextStyle?</c> slot the bridge generator adds via the
/// <c>OptionalValue</c> classification.
/// </summary>
public static class TextStyleOverrideDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "text-textfield-textstyle-override",
        CategoryId:  "text-inputs",
        Title:       "TextField text style override",
        Description: "TextStyle slot lets callers restyle text without affecting chrome.",
        Build:       c =>
        {
            var plain = c.MutableStateOf("Plain text");
            var fancy = c.MutableStateOf("Big bold blue");
            return new Column
            {
                new TextField(plain)
                {
                    Label = new Text("Default style"),
                },
                new TextField(fancy)
                {
                    Label     = new Text("Custom style"),
                    TextStyle = new TextStyle
                    {
                        Color      = Color.FromRgb(0x21, 0x96, 0xF3),
                        FontSize   = 22,
                        FontWeight = FontWeight.Bold,
                    },
                },
            };
        });
}
