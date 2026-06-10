using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.DialogsSheets;

/// <summary>AlertDialog — Title, Text body, Confirm + Dismiss buttons.</summary>
public static class AlertDialogDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "dialogs-alert",
        CategoryId:  "dialogs-sheets",
        Title:       "AlertDialog",
        Description: "Modal alert with confirm + dismiss buttons.",
        Build:       c =>
        {
            var open  = c.MutableStateOf(false);
            var count = c.MutableStateOf(0);
            return new Column
            {
                new Text($"count = {count}"),
                new Button(onClick: () => open.Value = true) { new Text("Show dialog") },
                open.Value
                    ? new AlertDialog(onDismissRequest: () => open.Value = false)
                    {
                        Title         = new Text("Reset counter?"),
                        Text          = new Text("This will set the counter back to zero."),
                        ConfirmButton = new Button(onClick: () => { count.Value = 0; open.Value = false; })
                        {
                            new Text("Reset"),
                        },
                        DismissButton = new Button(onClick: () => open.Value = false)
                        {
                            new Text("Cancel"),
                        },
                    }
                    : (ComposableNode?)null,
            };
        });
}
