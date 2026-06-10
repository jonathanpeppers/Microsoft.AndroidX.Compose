using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.DialogsSheets;

/// <summary>ModalBottomSheet — slides up from the bottom edge.</summary>
public static class ModalBottomSheetDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "dialogs-bottom-sheet",
        CategoryId:  "dialogs-sheets",
        Title:       "ModalBottomSheet",
        Description: "Drag down or tap outside to dismiss.",
        Build:       c =>
        {
            var open = c.MutableStateOf(false);
            return new Column
            {
                new Button(onClick: () => open.Value = true) { new Text("Show sheet") },
                open.Value
                    ? new ModalBottomSheet(onDismissRequest: () => open.Value = false)
                    {
                        new Column
                        {
                            new Text("Modal bottom sheet"),
                            new Text("Drag down or tap outside to dismiss."),
                            new Button(onClick: () => open.Value = false) { new Text("Hide") },
                        },
                    }
                    : (ComposableNode?)null,
            };
        });
}
