using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.Modifiers;

/// <summary>GraphicsLayer — rotationZ, scale, alpha, plus a non-default TransformOrigin.</summary>
public static class GraphicsLayerDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "modifiers-graphics-layer",
        CategoryId:  "modifiers",
        Title:       "GraphicsLayer",
        Description: "Drag-driven rotation + alpha + asymmetric scale, with one tile pivoting at the top-left corner and the other at its center.",
        Build:       c =>
        {
            var density = Android.Content.Res.Resources.System!.DisplayMetrics!.Density;
            var dragX   = c.Remember(() => new MutableState<float>(0f));
            var state   = c.RememberDraggableState(delta => dragX.Value += delta / density);

            return new Column
            {
                new Text("Drag the purple bar below to drive rotation/scale/alpha:"),
                new Box
                {
                    Modifier.Companion.FillMaxWidth().Height(48).Padding(4)
                        .Border(1, Color.FromRgb(0xB0, 0xB0, 0xB0)),
                    new Box
                    {
                        Modifier.Companion
                            .Offset(x: dragX.Value)
                            .Size(40)
                            .Background(Color.FromRgb(0xCE, 0x93, 0xD8))
                            .Draggable(state, Orientation.Horizontal),
                    },
                },
                new Text($"rotationZ = {dragX.Value:F0}"),
                new Row
                {
                    Modifier.Companion.FillMaxWidth().Height(96).Padding(4),
                    new Box
                    {
                        Modifier.Companion
                            .Size(64)
                            .GraphicsLayer(
                                rotationZ:       dragX.Value,
                                alpha:           0.6f + 0.4f * MathF.Min(1f, MathF.Abs(dragX.Value) / 90f),
                                transformOrigin: TransformOrigin.Pack(0f, 0f))
                            .Background(Color.FromRgb(0xAB, 0x47, 0xBC)),
                        new Text("⟲") { Modifier = Modifier.Companion.Padding(20) },
                    },
                    new Spacer { Modifier = Modifier.Companion.WidthIn(16, null) },
                    new Box
                    {
                        Modifier.Companion
                            .Size(64)
                            .GraphicsLayer(
                                rotationZ: -dragX.Value,
                                scaleX:    1.0f + dragX.Value / 200f,
                                scaleY:    1.0f - dragX.Value / 400f)
                            .Background(Color.FromRgb(0x66, 0xBB, 0x6A)),
                        new Text("↔") { Modifier = Modifier.Companion.Padding(20) },
                    },
                },
                new Button(onClick: () => dragX.Value = 0f) { new Text("Reset") },
            };
        });
}
