using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.Selection;

/// <summary>Two-state checkbox.</summary>
public static class CheckboxDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "selection-checkbox",
        CategoryId:  "selection",
        Title:       "Checkbox",
        Description: "Plain two-state checkbox bound to a MutableState<bool>. The enabled toggle greys out the checkbox and ignores taps.",
        Build:       c =>
        {
            var on      = c.MutableStateOf(false);
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
                    new Checkbox(@checked: on.Value, onCheckedChange: v => on.Value = v, enabled: enabled.Value),
                    new Text(on.Value ? "Checked" : "Unchecked"),
                },
            };
        });
}
