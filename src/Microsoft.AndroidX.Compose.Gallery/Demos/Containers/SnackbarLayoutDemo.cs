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
        Description: "Static layout plus queued SnackbarHostState messages and results.",
        Build:       c =>
        {
            var newLine = c.MutableStateOf(false);
            var host = c.Remember(() => new SnackbarHostState());
            var result = c.MutableStateOf("No snackbar shown");
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
                new Button(async () =>
                {
                    var value = await host.ShowSnackbarAsync(
                        "Saved",
                        actionLabel: "Undo",
                        withDismissAction: true,
                        duration: SnackbarDuration.Long);
                    result.Value = $"Result: {value}";
                })
                {
                    new Text("Show queued snackbar"),
                },
                new Text(result.Value),
                new SnackbarHost(host),
            };
        });
}
