using Microsoft.AndroidX.Compose.Gallery.Registry;

namespace Microsoft.AndroidX.Compose.Gallery.Demos.Containers;

/// <summary>Spacer — a sized empty slot in a layout.</summary>
public static class SpacerDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "containers-spacer",
        CategoryId:  "containers",
        Title:       "Spacer",
        Description: "Spacer with explicit Height / Width for fixed gaps.",
        Build:       () => new Column
        {
            new Text("32-dp vertical Spacer between the two blocks:"),
            new Box
            {
                Modifier.Companion
                    .Height(40)
                    .FillMaxWidth()
                    .Background(Color.FromRgb(0xB3, 0xE5, 0xFC)),
            },
            new Spacer { Modifier = Modifier.Companion.Height(32) },
            new Box
            {
                Modifier.Companion
                    .Height(40)
                    .FillMaxWidth()
                    .Background(Color.FromRgb(0xC8, 0xE6, 0xC9)),
            },
            new Text("48-dp horizontal Spacer between the two blocks:"),
            new Row
            {
                new Box
                {
                    Modifier.Companion
                        .Size(56)
                        .Background(Color.FromRgb(0xFF, 0xE0, 0xB2)),
                },
                new Spacer { Modifier = Modifier.Companion.Width(48) },
                new Box
                {
                    Modifier.Companion
                        .Size(56)
                        .Background(Color.FromRgb(0xCE, 0x93, 0xD8)),
                },
            },
        });
}
