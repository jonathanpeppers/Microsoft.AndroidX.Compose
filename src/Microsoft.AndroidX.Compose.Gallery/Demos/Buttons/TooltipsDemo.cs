using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.Buttons;

/// <summary>Automatic and programmatically controlled tooltips.</summary>
public static class TooltipsDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "buttons-tooltips",
        CategoryId:  "buttons",
        Title:       "Tooltips",
        Description: "Long-press, show, and dismiss Tooltip state.",
        Build:       c =>
        {
            var count = c.MutableStateOf(0);
            var state = c.Remember(() => new TooltipStateHolder(isPersistent: true));
            var dragStatus = c.MutableStateOf("Hold the second anchor, then release");
            return new Column
            {
                new Text($"Tapped: {count}"),
                new Tooltip
                {
                    Tip    = new Surface { new Text("Helpful hint") },
                    Anchor = new Button(onClick: () => count++) { new Text("Long-press me") },
                },
                new Row
                {
                    new Button(onClick: () => _ = state.ShowAsync()) { new Text("Show tip") },
                    new Button(onClick: state.Dismiss) { new Text("Dismiss tip") },
                },
                new Tooltip(state)
                {
                    EnableUserInput = false,
                    Tip = new Surface { new Text("Controlled by TooltipStateHolder") },
                    Anchor = new Text("Programmatic tip anchor"),
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
