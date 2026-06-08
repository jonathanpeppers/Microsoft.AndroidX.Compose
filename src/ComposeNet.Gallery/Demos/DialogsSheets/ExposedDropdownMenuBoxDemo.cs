using ComposeNet.Gallery.Registry;

namespace ComposeNet.Gallery.Demos.DialogsSheets;

/// <summary>ExposedDropdownMenuBox — read-only TextField anchored to a popup list.</summary>
public static class ExposedDropdownMenuBoxDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "dialogs-exposed-dropdown",
        CategoryId:  "dialogs-sheets",
        Title:       "ExposedDropdownMenuBox",
        Description: "Read-only TextField + ▼ button toggles an anchored popup list.",
        Build:       () =>
        {
            var open     = Compose.Remember(() => new MutableState<bool>(false));
            var selected = Compose.Remember(() => new MutableState<string>("Apple"));
            return new ExposedDropdownMenuBox(
                expanded:         open.Value,
                onExpandedChange: v => open.Value = v)
            {
                new Row
                {
                    new TextField(value: selected.Value, onValueChange: _ => { }),
                    new IconButton(onClick: () => open.Value = !open.Value)
                    {
                        new Text(open.Value ? "▲" : "▼"),
                    },
                },
                new ExposedDropdownMenu(
                    expanded:         open.Value,
                    onDismissRequest: () => open.Value = false)
                {
                    new DropdownMenuItem(text: new Text("Apple"),  onClick: () => { selected.Value = "Apple";  open.Value = false; }),
                    new DropdownMenuItem(text: new Text("Banana"), onClick: () => { selected.Value = "Banana"; open.Value = false; }),
                    new DropdownMenuItem(text: new Text("Cherry"), onClick: () => { selected.Value = "Cherry"; open.Value = false; }),
                },
            };
        });
}
