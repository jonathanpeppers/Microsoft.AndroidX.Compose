using Microsoft.AndroidX.Compose.Gallery.Registry;

namespace Microsoft.AndroidX.Compose.Gallery.Demos.Selection;

/// <summary>Material 3 switch.</summary>
public static class SwitchDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "selection-switch",
        CategoryId:  "selection",
        Title:       "Switch",
        Description: "Bool-bound Switch with live status text.",
        Build:       () =>
        {
            var on = ComposeRuntime.Remember(() => new MutableState<bool>(false));
            return new Column
            {
                new Row
                {
                    new Switch(@checked: on.Value, onCheckedChange: v => on.Value = v),
                    new Text(on.Value ? "On" : "Off"),
                },
            };
        });
}
