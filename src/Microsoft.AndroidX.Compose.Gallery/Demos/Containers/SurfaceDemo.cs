using AndroidX.Compose.Foundation;
using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.Containers;

/// <summary>Surface styling, inherited content colors, and live default resets.</summary>
public static class SurfaceDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "containers-surface",
        CategoryId:  "containers",
        Title:       "Surface",
        Description: "Custom colors, border, tonal/shadow elevation, and live resets in both palettes.",
        Build:       _ => new Composed(c =>
        {
            var mode = c.RememberSaveable(() => new MutableNumberState<int>(0));
            var dark = c.RememberSaveable(() => new MutableState<bool>(false));
            var border = c.Remember(() => BorderStrokeKt.BorderStroke(
                2f, Color.FromRgb(0xE6, 0xAF, 0x2E).ToPacked()));
            var shape = c.Remember(() => Shape.RoundedCorners(12));
            var styled = new Surface
            {
                Modifier = Modifier.FillMaxWidth(),
                Shape = shape,
                Color = mode.Value == 1 ? Color.FromRgb(0x51, 0x2B, 0xD4)
                    : mode.Value == 2 ? Color.Transparent : null,
                ContentColor = mode.Value == 1 ? Color.White : null,
                TonalElevation = mode.Value == 1 ? 8 : mode.Value == 2 ? 0 : null,
                ShadowElevation = mode.Value == 1 ? 8 : mode.Value == 2 ? 0 : null,
                Border = mode.Value == 1 ? border : null,
            };
            styled.Add(new Text("Inherited content color") { Modifier = Modifier.Padding(16) });
            var tonal = new Surface { TonalElevation = 8, Modifier = Modifier.FillMaxWidth() };
            tonal.Add(new Text("Theme surface + 8dp tonal elevation") { Modifier = Modifier.Padding(16) });
            var theme = new MaterialTheme { Dark = dark.Value, UseDynamicColor = false };
            theme.Add(new Column
            {
                new Button(() => mode.Value = (mode.Value + 1) % 3) { new Text("Cycle styling") },
                new Button(() => dark.Value = !dark.Value) { new Text("Toggle palette") },
                new Text($"Mode: {mode.Value} (0 defaults, 1 styled, 2 explicit zero)"),
                new Spacer { Modifier = Modifier.Height(12) },
                styled,
                new Spacer { Modifier = Modifier.Height(16) },
                tonal,
            });
            return theme;
        }));
}
