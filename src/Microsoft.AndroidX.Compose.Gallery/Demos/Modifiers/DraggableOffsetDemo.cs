using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.Modifiers;

/// <summary>Draggable + Offset — finger drag accumulates into a Dp value.</summary>
public static class DraggableOffsetDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "modifiers-draggable-offset",
        CategoryId:  "modifiers",
        Title:       "Draggable + Offset",
        Description: "Touch and drag the purple square horizontally; the DraggableState delta is divided by screen density so dragX accumulates as Dp.",
        Build:       c =>
        {
            var density = Android.Content.Res.Resources.System!.DisplayMetrics!.Density;
            var dragX   = c.MutableStateOf(0f);
            var state   = c.RememberDraggableState(delta => dragX.Value += delta / density);

            return new Column
            {
                new Text($"Drag offset: {dragX.Value:F0}dp") { Modifier = Modifier.Companion.SafeContentPadding() },
                new Box
                {
                    Modifier.Companion.FillMaxWidth().Height(72).Padding(4)
                        .Border(1, Color.FromRgb(0xB0, 0xB0, 0xB0)),
                    new Box
                    {
                        Modifier.Companion
                            .Offset(x: dragX.Value)
                            .Size(56)
                            .Background(Color.FromRgb(0xCE, 0x93, 0xD8))
                            .Draggable(state, Orientation.Horizontal),
                    },
                },
                new Button(onClick: () => dragX.Value = 0f) { new Text("Reset") },
            };
        });
}
