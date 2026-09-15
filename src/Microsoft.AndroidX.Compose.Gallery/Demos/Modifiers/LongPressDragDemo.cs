using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.Modifiers;

/// <summary>Native long-press drag, live callbacks, key cancellation, and removal.</summary>
public static class LongPressDragDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id: "modifiers-long-press-drag",
        CategoryId: "modifiers",
        Title: "Long-press drag lifecycle",
        Description: "Hold then move. Deltas recompose without restarting. Release ends; changing the key or removing the tile cancels.",
        Build: c =>
        {
            var x = c.MutableStateOf(0f);
            var y = c.MutableStateOf(0f);
            var status = c.MutableStateOf("Hold the tile");
            var version = c.MutableStateOf(0);
            var key = c.MutableStateOf(0);
            var visible = c.MutableStateOf(true);
            int callbackVersion = version.Value;
            return new Column
            {
                Modifier.FillMaxWidth().Padding(8),
                new Text($"{status.Value}; X={x.Value:F0}px Y={y.Value:F0}px"),
                new Text($"Callback generation: {callbackVersion}"),
                new Text("Use a second finger on the controls while holding the tile."),
                visible.Value ? new Box
                {
                    Modifier.FillMaxWidth().Height(160)
                        .Background(Color.FromArgb(0xFFE3F2FD))
                        .DetectDragGesturesAfterLongPress(
                            onDrag: delta =>
                            {
                                x.Value += delta.X;
                                y.Value += delta.Y;
                                status.Value = $"Moving (callback {callbackVersion})";
                            },
                            onDragStart: _ => { x.Value = y.Value = 0; status.Value = "Started"; },
                            onDragEnd: () => status.Value = $"Released (callback {callbackVersion})",
                            onDragCancel: () => status.Value = $"Cancelled (callback {callbackVersion})",
                            key: key.Value),
                    new Text("Hold, drag, release") { Color = Color.Black },
                } : null,
                new Button(() => version.Value++) { new Text("Recompose callbacks") },
                new Button(() => key.Value++) { new Text("Change key") },
                new Button(() => visible.Value = !visible.Value) { new Text("Remove / restore tile") },
            };
        });
}
