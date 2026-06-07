using AndroidX.Compose.UI.Graphics;
using ComposeNet.Gallery.Registry;

namespace ComposeNet.Gallery.Demos.TextInputs;

/// <summary>Color + TextAlign on Text.</summary>
public static class ColorAndAlignment
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "text-color-alignment",
        CategoryId:  "text-inputs",
        Title:       "Color & alignment",
        Description: "Coloured Text plus Start / Center / End TextAlign.",
        Build:       () => new Column
        {
            new Text("Red, centered")
            {
                Color    = Color.FromRgb(0xC6, 0x28, 0x28),
                Align    = ComposeNet.TextAlign.Center,
                Modifier = Modifier.Companion.FillMaxWidth(),
            },
            new Text("Blue, end-aligned")
            {
                Color    = Color.FromRgb(0x15, 0x65, 0xC0),
                Align    = ComposeNet.TextAlign.End,
                Modifier = Modifier.Companion.FillMaxWidth(),
            },
            new Text("Green, start (default)")
            {
                Color    = Color.FromRgb(0x2E, 0x7D, 0x32),
                Align    = ComposeNet.TextAlign.Start,
                Modifier = Modifier.Companion.FillMaxWidth(),
            },
        });
}
