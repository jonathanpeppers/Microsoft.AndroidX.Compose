using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.Selection;

/// <summary>RadioButton group with three options.</summary>
public static class RadioButtonGroupDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "selection-radio-group",
        CategoryId:  "selection",
        Title:       "RadioButton group",
        Description: "Single-selection group built from three RadioButtons. Toggle Enabled to disable the whole group.",
        Build:       c =>
        {
            var picked  = c.MutableStateOf(0);
            var enabled = c.MutableStateOf(true);
            return new Column
            {
                new Row
                {
                    new Switch(@checked: enabled.Value, onCheckedChange: v => enabled.Value = v),
                    new Text(enabled.Value ? "Enabled" : "Disabled"),
                },
                new Row
                {
                    new RadioButton(selected: picked.Value == 0, onClick: () => picked.Value = 0, enabled: enabled.Value),
                    new Text("A"),
                    new RadioButton(selected: picked.Value == 1, onClick: () => picked.Value = 1, enabled: enabled.Value),
                    new Text("B"),
                    new RadioButton(selected: picked.Value == 2, onClick: () => picked.Value = 2, enabled: enabled.Value),
                    new Text("C"),
                },
                new Text($"Picked: {(char)('A' + picked.Value)}"),
            };
        });
}
