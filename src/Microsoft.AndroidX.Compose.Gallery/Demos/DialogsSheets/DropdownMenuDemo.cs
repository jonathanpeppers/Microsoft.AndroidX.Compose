using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.DialogsSheets;

/// <summary>DropdownMenu anchored to an IconButton inside a Box.</summary>
public static class DropdownMenuDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "dialogs-dropdown-menu",
        CategoryId:  "dialogs-sheets",
        Title:       "DropdownMenu",
        Description: "IconButton + DropdownMenu inside a shared Box for anchoring.",
        Build:       c =>
        {
            var open      = c.Remember(() => new MutableState<bool>(false));
            var selection = c.Remember(() => new MutableState<string>("(none)"));
            return new Column
            {
                new Text($"Last menu choice: {selection}"),
                new Row
                {
                    new Text("Tap ⋮ for actions:"),
                    new Spacer { Modifier = Modifier.Width(8) },
                    new Box
                    {
                        new IconButton(onClick: () => open.Value = true) { new Text("⋮") },
                        new DropdownMenu(
                            expanded:         open.Value,
                            onDismissRequest: () => open.Value = false)
                        {
                            new DropdownMenuItem(text: new Text("Refresh"),
                                onClick: () => { selection.Value = "Refresh";  open.Value = false; }),
                            new DropdownMenuItem(text: new Text("Settings"),
                                onClick: () => { selection.Value = "Settings"; open.Value = false; }),
                            new DropdownMenuItem(text: new Text("About"),
                                onClick: () => { selection.Value = "About";    open.Value = false; }),
                        },
                    },
                },
            };
        });
}
