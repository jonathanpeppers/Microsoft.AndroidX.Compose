using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.TextInputs;

/// <summary>Color + TextAlign on Text.</summary>
public static class ColorAndAlignmentDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "text-color-alignment",
        CategoryId:  "text-inputs",
        Title:       "Color & alignment",
        Description: "Coloured Text plus Start / Center / End TextAlign.",
        Build:       _ => new Column
        {
            new Text("Red, centered")
            {
                Color    = Color.FromRgb(0xC6, 0x28, 0x28),
                Align    = TextAlign.Center,
                Modifier = Modifier.FillMaxWidth(),
            },
            new Text("Blue, end-aligned")
            {
                Color    = Color.FromRgb(0x15, 0x65, 0xC0),
                Align    = TextAlign.End,
                Modifier = Modifier.FillMaxWidth(),
            },
            new Text("Green, start (default)")
            {
                Color    = Color.FromRgb(0x2E, 0x7D, 0x32),
                Align    = TextAlign.Start,
                Modifier = Modifier.FillMaxWidth(),
            },
        });
}
