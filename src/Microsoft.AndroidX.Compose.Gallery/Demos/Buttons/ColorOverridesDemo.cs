using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.Buttons;

/// <summary>
/// <see cref="Button.Colors"/> — override the container color while
/// keeping the M3 default content / disabled colors. Exercises the
/// <c>composer.ButtonColors(...)</c> extension, the bridge's
/// <c>colors</c> slot, and the auto-default-mask path.
/// </summary>
public static class ColorOverridesDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "buttons-color-overrides",
        CategoryId:  "buttons",
        Title:       "Color overrides",
        Description: "Override containerColor via ButtonColors slot (MAUI Primary #512BD4).",
        Build:       c =>
        {
            Button MakeFilled(string text, AndroidX.Compose.Material3.ButtonColors? colors = null)
            {
                var b = new Button(onClick: () => { }) { new Text(text) };
                b.Colors = colors;
                return b;
            }
            ElevatedButton MakeElevated(string text, AndroidX.Compose.Material3.ButtonColors? colors = null)
            {
                var b = new ElevatedButton(onClick: () => { }) { new Text(text) { Color = Color.Black } };
                b.Colors = colors;
                return b;
            }
            return new Column
            {
                new Text("Default colors"),
                MakeFilled("Filled (theme default)"),
                new Text("Container = MAUI Primary"),
                MakeFilled("Filled (#512BD4)",
                    c.ButtonColors(containerColor: Color.FromRgb(0x51, 0x2B, 0xD4))),
                new Text("Container = teal, content = white"),
                MakeFilled("Filled (teal + white)",
                    c.ButtonColors(
                        containerColor: Color.FromRgb(0x00, 0x79, 0x6B),
                        contentColor:   Color.White)),
                new Text("ElevatedButton + container override"),
                MakeElevated("Elevated (light blue)",
                    c.ButtonColors(containerColor: Color.FromRgb(0xE3, 0xF2, 0xFD))),
            };
        });
}
