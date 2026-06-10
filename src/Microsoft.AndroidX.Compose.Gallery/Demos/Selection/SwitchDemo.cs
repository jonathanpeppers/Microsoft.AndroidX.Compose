using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.Selection;

/// <summary>Material 3 switch.</summary>
public static class SwitchDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "selection-switch",
        CategoryId:  "selection",
        Title:       "Switch",
        Description: "Bool-bound Switch with live status text. The enabled toggle disables the lower switch.",
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
                    new Switch(@checked: on.Value, onCheckedChange: v => on.Value = v, enabled: enabled.Value),
                    new Text(on.Value ? "On" : "Off"),
                },
            };
        });
}
