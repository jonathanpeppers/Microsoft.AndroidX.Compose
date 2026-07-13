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
        Description: "Children stack at one anchor; propagateMinConstraints controls child minimums.",
        Build:       _ => new Column
        {
            new Text("Default constraints"),
            new Box
            {
                Modifier
                    .Size(160)
                    .Background(Color.FromRgb(0xB3, 0xE5, 0xFC)),
                new Text("Front")
                {
                    Color = Color.Black,
                    Modifier = Modifier.Padding(28),
                },
            },
            new Text("propagateMinConstraints: true"),
            new Box(propagateMinConstraints: true)
            {
                Modifier
                    .Size(160)
                    .Background(Color.FromRgb(0x81, 0xD4, 0xFA)),
                new Box
                {
                    Modifier.Background(Color.FromRgb(0x4F, 0xC3, 0xF7)),
                    new Text("Child receives minimums") { Color = Color.Black },
                },
            },
        });
}
