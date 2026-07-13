using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.TextInputs;

/// <summary>TextField with every named slot — label, placeholder, leading/trailing icon, supporting text.</summary>
public static class TextFieldSlotsDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "text-textfield-slots",
        CategoryId:  "text-inputs",
        Title:       "TextField slots",
        Description: "Slots plus constructor-backed Enabled, ReadOnly, and SingleLine defaults.",
        Build:       c =>
        {
            var name     = c.MutableStateOf("");
            var enabled  = c.MutableStateOf(true);
            var readOnly = c.MutableStateOf(false);
            return new Column
            {
                new TextField(
                    name,
                    enabled: enabled.Value,
                    readOnly: readOnly.Value,
                    singleLine: true)
                {
                    Label          = new Text("Your name"),
                    Placeholder    = new Text("Type something…"),
                    LeadingIcon    = new Text("👤"),
                    TrailingIcon   = new Text("✎"),
                    SupportingText = new Text("All five slots filled"),
                },
                new Row(horizontalArrangement: Arrangement.SpacedBy(8.Dp()))
                {
                    new Button(onClick: () => enabled.Value = !enabled.Value)
                    {
                        new Text(enabled.Value ? "Disable" : "Enable"),
                    },
                    new Button(onClick: () => readOnly.Value = !readOnly.Value)
                    {
                        new Text(readOnly.Value ? "Make editable" : "Make read-only"),
                    },
                },
                new Text($"Hi {(string.IsNullOrEmpty(name.Value) ? "stranger" : name.Value)}"),
            };
        });
}
