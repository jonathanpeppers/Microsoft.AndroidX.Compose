using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.Buttons;

/// <summary>The Material 3 chip family — Assist / Filter / Suggestion plus Elevated variants.</summary>
public static class ChipsDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "buttons-chips",
        CategoryId:  "buttons",
        Title:       "Chips",
        Description: "AssistChip, FilterChip, SuggestionChip (plus Elevated variants).",
        Build:       c =>
        {
            var count = c.MutableStateOf(0);
            var liked = c.MutableStateOf(false);
            return new Column(verticalArrangement: Arrangement.SpacedBy(8.Dp()))
            {
                new Text($"Count: {count}, liked: {liked.Value}"),
                new FlowRow
                {
                    Modifier.Companion.FillMaxWidth(),
                    new AssistChip(onClick: () => count++)
                        { Label = new Text("Assist (+1)") },
                    new ElevatedAssistChip(onClick: () => count++)
                        { Label = new Text("Elevated assist (+1)") },
                    new FilterChip(selected: liked.Value, onClick: () => liked.Value = !liked.Value)
                        { Label = new Text(liked.Value ? "Liked" : "Like") },
                    new ElevatedFilterChip(selected: liked.Value, onClick: () => liked.Value = !liked.Value)
                        { Label = new Text(liked.Value ? "Elevated liked" : "Elevated like") },
                    new SuggestionChip(onClick: () => count.Value = 0)
                        { Label = new Text("Reset") },
                    new ElevatedSuggestionChip(onClick: () => count.Value = 0)
                        { Label = new Text("Elevated reset") },
                },
            };
        });
}
