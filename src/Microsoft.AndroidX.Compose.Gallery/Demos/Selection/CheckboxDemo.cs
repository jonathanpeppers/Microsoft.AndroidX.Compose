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
        Description: "Plain two-state checkbox bound to a MutableState<bool>.",
        Build:       c =>
        {
            var on = c.Remember(() => new MutableState<bool>(false));
            return new Column
            {
                new Row
                {
                    new Checkbox(@checked: on.Value, onCheckedChange: v => on.Value = v),
                    new Text(on.Value ? "Checked" : "Unchecked"),
                },
            };
        });
}
