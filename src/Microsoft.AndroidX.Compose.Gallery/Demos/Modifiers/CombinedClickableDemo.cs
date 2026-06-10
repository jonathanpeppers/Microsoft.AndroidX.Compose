using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.Modifiers;

/// <summary>CombinedClickable — onClick (+1), onLongClick (+10), onDoubleClick (+100).</summary>
public static class CombinedClickableDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "modifiers-combined-clickable",
        CategoryId:  "modifiers",
        Title:       "CombinedClickable",
        Description: "One gesture modifier that dispatches single / long / double taps to separate callbacks.",
        Build:       c =>
        {
            var taps = c.MutableStateOf(0);
            return new Column
            {
                new Text($"Taps (single+1, long+10, double+100): {taps}"),
                new Text("Tap, hold, or double-tap me")
                {
                    Modifier = Modifier
                        .FillMaxWidth()
                        .CombinedClickable(
                            onClick:       () => taps.Value += 1,
                            onLongClick:   () => taps.Value += 10,
                            onDoubleClick: () => taps.Value += 100)
                        .Padding(12),
                },
                new Button(onClick: () => taps.Value = 0) { new Text("Reset") },
            };
        });
}
