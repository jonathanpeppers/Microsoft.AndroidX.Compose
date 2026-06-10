using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.Modifiers;

/// <summary>Toggleable + Selectable + Semantics modifiers; whole row collapses into one a11y node.</summary>
public static class ToggleableSelectableSemanticsDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "modifiers-toggleable-selectable-semantics",
        CategoryId:  "modifiers",
        Title:       "Toggleable + Selectable + Semantics",
        Description: "A Row collapsed into one a11y node, three Selectable rows acting as a radio group, and a Box that announces a custom semantic role.",
        Build:       c =>
        {
            var liked       = c.MutableStateOf(false);
            var selectedRow = c.MutableStateOf(0);
            var taps        = c.MutableStateOf(0);

            return new Column
            {
                new Text("Toggleable row (announced as one a11y node):"),
                new Row
                {
                    Modifier.Companion
                        .FillMaxWidth()
                        .Toggleable(liked.Value, v => liked.Value = v)
                        .Semantics(mergeDescendants: true, liked.Value ? "Liked" : "Not liked")
                        .Padding(8),
                    new Text(liked.Value ? "♥ Liked" : "♡ Tap to like"),
                },
                new Text($"Selectable group — selected row: {selectedRow}"),
                new Text("Row 0") { Modifier = Modifier.Companion
                    .FillMaxWidth().Selectable(selectedRow.Value == 0, () => selectedRow.Value = 0).Padding(6) },
                new Text("Row 1") { Modifier = Modifier.Companion
                    .FillMaxWidth().Selectable(selectedRow.Value == 1, () => selectedRow.Value = 1).Padding(6) },
                new Text("Row 2") { Modifier = Modifier.Companion
                    .FillMaxWidth().Selectable(selectedRow.Value == 2, () => selectedRow.Value = 2).Padding(6) },
                new Text($"Semantics(role) custom 'button' (taps: {taps}):"),
                new Box
                {
                    Modifier.Companion
                        .FillMaxWidth()
                        .Height(48)
                        .Background(Color.FromRgb(0xB3, 0xE5, 0xFC))
                        .Clickable(() => taps.Value++)
                        .Semantics("Custom tap target", SemanticsRole.Button)
                        .Padding(12),
                    new Text("Tap me — announced as 'button'") { Color = Color.Black },
                },
            };
        });
}
