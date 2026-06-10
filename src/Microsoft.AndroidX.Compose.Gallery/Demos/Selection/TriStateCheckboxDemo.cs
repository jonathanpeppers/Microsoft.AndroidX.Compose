using AndroidX.Compose.UI.State;
using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.Selection;

/// <summary>Three-state checkbox (on / off / indeterminate).</summary>
public static class TriStateCheckboxDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "selection-tristate-checkbox",
        CategoryId:  "selection",
        Title:       "TriStateCheckbox",
        Description: "Cycles through On → Off → Indeterminate on tap. The enabled toggle disables the cycling checkbox.",
        Build:       c =>
        {
            var state   = c.MutableStateOf(ToggleableState.Indeterminate!);
            var enabled = c.MutableStateOf(true);
            return new Column
            {
                new Row(horizontalArrangement: Arrangement.SpacedBy(8), verticalAlignment: Alignment.Vertical.CenterVertically)
                {
                    new Switch(@checked: enabled.Value, onCheckedChange: v => enabled.Value = v),
                    new Text(enabled.Value ? "Enabled" : "Disabled"),
                },
                new Row(horizontalArrangement: Arrangement.SpacedBy(8), verticalAlignment: Alignment.Vertical.CenterVertically)
                {
                    new TriStateCheckbox(
                        state:   state.Value,
                        onClick: () => state.Value = (state.Value == ToggleableState.On
                            ? ToggleableState.Off
                            : state.Value == ToggleableState.Off
                                ? ToggleableState.Indeterminate
                                : ToggleableState.On)!,
                        enabled: enabled.Value),
                    new Text(state.Value.ToString() ?? "?"),
                },
            };
        });
}
