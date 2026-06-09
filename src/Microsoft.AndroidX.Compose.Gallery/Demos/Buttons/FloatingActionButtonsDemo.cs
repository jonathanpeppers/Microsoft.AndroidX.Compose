using Microsoft.AndroidX.Compose.Gallery.Registry;

namespace Microsoft.AndroidX.Compose.Gallery.Demos.Buttons;

/// <summary>The FAB family: standard, Small, Large, and Extended.</summary>
public static class FloatingActionButtonsDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "buttons-fabs",
        CategoryId:  "buttons",
        Title:       "Floating action buttons",
        Description: "FloatingActionButton, SmallFloatingActionButton, LargeFloatingActionButton, ExtendedFloatingActionButton.",
        Build:       c =>
        {
            var count = c.Remember(() => new MutableNumberState<int>(0));
            return new Column(verticalArrangement: Arrangement.SpacedBy(12))
            {
                new Text($"Tapped: {count}"),
                new FloatingActionButton(onClick: () => count++)
                    { new Text("✕") },
                new Row(horizontalArrangement: Arrangement.SpacedBy(12))
                {
                    new SmallFloatingActionButton(onClick: () => count++) { new Text("+") },
                    new LargeFloatingActionButton(onClick: () => count++) { new Text("+") },
                },
                new ExtendedFloatingActionButton(onClick: () => count++, expanded: true)
                {
                    Icon = new Text("✓"),
                    Text = new Text("Increment"),
                },
            };
        });
}
