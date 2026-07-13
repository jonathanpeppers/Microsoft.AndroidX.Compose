using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.Containers;

/// <summary>Snackbar action placement controlled by actionOnNewLine.</summary>
public static class SnackbarLayoutDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "containers-snackbar-layout",
        CategoryId:  "containers",
        Title:       "Snackbar action layout",
        Description: "Toggle whether the action is placed on a new line.",
        Build:       c =>
        {
            var newLine = c.MutableStateOf(false);
            return new Column
            {
                new Row(horizontalArrangement: Arrangement.SpacedBy(8.Dp()),
                        verticalAlignment: Alignment.Vertical.CenterVertically)
                {
                    new Switch(@checked: newLine.Value, onCheckedChange: value => newLine.Value = value),
                    new Text("Action on new line"),
                },
                new Snackbar(actionOnNewLine: newLine.Value)
                {
                    Body = new Text("A message with an action"),
                    Action = new Text("UNDO"),
                },
            };
        });
}
