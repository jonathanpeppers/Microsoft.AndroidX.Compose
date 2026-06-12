using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.Modifiers;

/// <summary>DetectTransformGestures — multi-pointer pinch / pan / rotate.</summary>
public static class DetectTransformGesturesDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "modifiers-detect-transform-gestures",
        CategoryId:  "modifiers",
        Title:       "DetectTransformGestures",
        Description: "Pinch to scale, two-finger drag to translate, twist to rotate. The single onGesture callback receives per-frame pan / zoom / rotation deltas.",
        Build:       c =>
        {
            var density = Android.Content.Res.Resources.System!.DisplayMetrics!.Density;
            var scale    = c.MutableStateOf(1f);
            var rotation = c.MutableStateOf(0f);
            var x        = c.MutableStateOf(0f);
            var y        = c.MutableStateOf(0f);

            return new Column
            {
                Modifier.FillMaxWidth().Padding(8),
                new Text($"Scale: {scale.Value:F2}    Rotation: {rotation.Value:F0}°"),
                new Text($"Pan: ({x.Value:F0}, {y.Value:F0}) dp"),
                new Box
                {
                    Modifier
                        .FillMaxWidth()
                        .Height(320)
                        .Background(Color.FromArgb(0xFFFFF3E0))
                        .DetectTransformGestures(onGesture: (centroid, pan, zoom, rot) =>
                        {
                            scale.Value    *= zoom;
                            rotation.Value += rot;
                            x.Value        += pan.X / density;
                            y.Value        += pan.Y / density;
                        }),
                    new Box
                    {
                        Modifier
                            .Offset(x: x.Value, y: y.Value)
                            .Size(120)
                            .GraphicsLayer(scaleX: scale.Value, scaleY: scale.Value, rotationZ: rotation.Value)
                            .Background(Color.FromArgb(0xFFFB8C00)),
                    },
                },
                new Button(onClick: () =>
                {
                    scale.Value = 1f;
                    rotation.Value = 0f;
                    x.Value = 0f;
                    y.Value = 0f;
                })
                {
                    new Text("Reset"),
                },
            };
        });
}
