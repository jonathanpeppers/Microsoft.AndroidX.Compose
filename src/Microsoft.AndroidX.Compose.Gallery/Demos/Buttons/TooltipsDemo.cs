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
            var count = c.Remember(() => new MutableNumberState<int>(0));
            return new Column
            {
                new Text($"Tapped: {count}"),
                new Tooltip
                {
                    Tip    = new Surface { new Text("Helpful hint") },
                    Anchor = new Button(onClick: () => count++) { new Text("Long-press me") },
                },
            };
        });
}
