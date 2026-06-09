using Microsoft.AndroidX.Compose.Gallery.Registry;

namespace Microsoft.AndroidX.Compose.Gallery.Demos.Buttons;

/// <summary>Icon-button fill variants: standard, filled, tonal, outlined.</summary>
public static class IconButtonsDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "buttons-icon-buttons",
        CategoryId:  "buttons",
        Title:       "Icon buttons",
        Description: "IconButton, FilledIconButton, FilledTonalIconButton, OutlinedIconButton.",
        Build:       c =>
        {
            var count = c.Remember(() => new MutableNumberState<int>(0));
            return new Column
            {
                new Text($"Tapped: {count}"),
                new Row
                {
                    new IconButton(onClick: () => count++) { new Text("☆") },
                    new FilledIconButton(onClick: () => count++) { new Text("★") },
                    new FilledTonalIconButton(onClick: () => count++) { new Text("◆") },
                    new OutlinedIconButton(onClick: () => count++) { new Text("◇") },
                },
            };
        });
}
