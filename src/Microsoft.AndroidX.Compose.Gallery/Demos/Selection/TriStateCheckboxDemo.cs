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
        Description: "Cycles through On → Off → Indeterminate on tap.",
        Build:       c =>
        {
            var state = c.MutableStateOf(ToggleableState.Indeterminate!);
            return new Column
            {
                new Row
                {
                    new TriStateCheckbox(
                        state:   state.Value,
                        onClick: () => state.Value = (state.Value == ToggleableState.On
                            ? ToggleableState.Off
                            : state.Value == ToggleableState.Off
                                ? ToggleableState.Indeterminate
                                : ToggleableState.On)!),
                    new Text(state.Value.ToString() ?? "?"),
                },
            };
        });
}
