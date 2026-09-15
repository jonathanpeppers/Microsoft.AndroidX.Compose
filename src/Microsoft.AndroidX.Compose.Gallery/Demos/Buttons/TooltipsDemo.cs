using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.Buttons;

/// <summary>Long-press tooltip anchored to a button.</summary>
public static class TooltipsDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "buttons-tooltips",
        CategoryId:  "buttons",
        Title:       "Tooltips",
        Description: "Tooltip wraps an Anchor; the Tip pops on long-press.",
        Build:       c =>
        {
            var count = c.MutableStateOf(0);
            var dragStatus = c.MutableStateOf("Hold the second anchor, then release");
            return new Column
            {
                new Text($"Tapped: {count}"),
                new Tooltip
                {
                    Tip    = new Surface { new Text("Helpful hint") },
                    Anchor = new Button(onClick: () => count++) { new Text("Long-press me") },
                },
                new Text(dragStatus.Value),
                new Tooltip
                {
                    EnableUserInput = false,
                    Tip = new Surface { new Text("Automatic input is disabled") },
                    Anchor = new Box
                    {
                        Modifier.FillMaxWidth().Height(96)
                            .DetectDragGesturesAfterLongPress(
                                _ => dragStatus.Value = "Dragging",
                                onDragStart: _ => dragStatus.Value = "Started (no tooltip)",
                                onDragEnd: () => dragStatus.Value = "Released",
                                onDragCancel: () => dragStatus.Value = "Cancelled"),
                        new Text("Hold here: anchor owns long press"),
                    },
                },
            };
        });
}
