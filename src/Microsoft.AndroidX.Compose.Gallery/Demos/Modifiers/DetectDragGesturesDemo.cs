using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.Modifiers;

/// <summary>DetectDragGestures — single-pointer drag with onDragStart / onDrag / onDragEnd.</summary>
public static class DetectDragGesturesDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "modifiers-detect-drag-gestures",
        CategoryId:  "modifiers",
        Title:       "DetectDragGestures",
        Description: "Low-level drag detector — tracks per-frame deltas in pixels and converts to Dp via screen density to position a draggable tile.",
        Build:       c =>
        {
            var density = Android.Content.Res.Resources.System!.DisplayMetrics!.Density;
            var x = c.MutableStateOf(0f);
            var y = c.MutableStateOf(0f);
            var status = c.MutableStateOf("(idle)");

            return new Column
            {
                Modifier.FillMaxWidth().Padding(8),
                new Text($"Status: {status.Value}"),
                new Text($"Offset: ({x.Value:F0}, {y.Value:F0}) dp"),
                new Box
                {
                    Modifier
                        .FillMaxWidth()
                        .Height(280)
                        .Background(Color.FromArgb(0xFFE3F2FD)),
                    new Box
                    {
                        Modifier
                            .Offset(x: x.Value, y: y.Value)
                            .Size(96)
                            .Background(Color.FromArgb(0xFF1976D2))
                            .DetectDragGestures(
                                onDragStart: _ => status.Value = "Dragging",
                                onDrag:      delta =>
                                {
                                    x.Value += delta.X / density;
                                    y.Value += delta.Y / density;
                                },
                                onDragEnd:    () => status.Value = "Released",
                                onDragCancel: () => status.Value = "Cancelled"),
                    },
                },
                new Button(onClick: () => { x.Value = 0; y.Value = 0; status.Value = "(idle)"; })
                {
                    new Text("Reset"),
                },
            };
        });
}

