using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.Containers;

/// <summary>Box stacks children on top of each other.</summary>
public static class BoxAlignmentDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "containers-box-alignment",
        CategoryId:  "containers",
        Title:       "Box alignment",
        Description: "Children stack at the same anchor — later children draw above earlier ones.",
        Build:       _ => new Box
        {
            Modifier.Companion
                .Size(160)
                .Background(Color.FromRgb(0xB3, 0xE5, 0xFC)),
            new Box
            {
                Modifier.Companion
                    .Size(120)
                    .Padding(8)
                    .Background(Color.FromRgb(0x81, 0xD4, 0xFA)),
            },
            new Box
            {
                Modifier.Companion
                    .Size(80)
                    .Padding(16)
                    .Background(Color.FromRgb(0x4F, 0xC3, 0xF7)),
            },
            new Text("Front")
            {
                Modifier = Modifier.Companion.Padding(28),
            },
        });
}
