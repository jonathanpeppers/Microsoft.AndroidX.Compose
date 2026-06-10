using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.Modifiers;

/// <summary>FlowRow scope dispatch — chips use RowScope-only Modifier.Align.</summary>
public static class FlowRowScopeDispatchDemo
{
    static readonly string[] Fruits =
    {
        "Apple", "Banana", "Cherry", "Durian", "Elderberry",
        "Fig", "Grape", "Honeydew",
    };

    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "modifiers-flowrow-scope-dispatch",
        CategoryId:  "modifiers",
        Title:       "FlowRow scope dispatch",
        Description: "Chips wrap onto multiple lines and each one calls Modifier.Align(Alignment.Vertical.CenterVertically), a RowScope-only modifier — proves FlowRow forwards its RowScope receiver.",
        Build:       c =>
        {
            var taps = c.MutableStateOf(0);
            var flow = new FlowRow
            {
                Modifier.FillMaxWidth().Padding(4),
            };
            foreach (var f in Fruits)
            {
                flow.Add(new AssistChip(onClick: () => taps.Value++)
                {
                    Label    = new Text(f),
                    Modifier = Modifier.Align(Alignment.Vertical.CenterVertically).Padding(2),
                });
            }
            return new Column
            {
                new Text($"Chip taps: {taps}"),
                flow,
            };
        });
}
