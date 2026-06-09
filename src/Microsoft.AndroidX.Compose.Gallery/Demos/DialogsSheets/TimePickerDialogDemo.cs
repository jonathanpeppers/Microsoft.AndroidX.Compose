using Microsoft.AndroidX.Compose.Gallery.Registry;

namespace Microsoft.AndroidX.Compose.Gallery.Demos.DialogsSheets;

/// <summary>TimePickerDialog — clock face dialog driven by TimePickerState.</summary>
public static class TimePickerDialogDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "dialogs-time-picker",
        CategoryId:  "dialogs-sheets",
        Title:       "TimePickerDialog",
        Description: "Confirm reads Hour + Minute from TimePickerState.",
        Build:       c =>
        {
            var open   = c.Remember(() => new MutableState<bool>(false));
            var picked = c.Remember(() => new MutableState<string>("(none)"));
            var state  = c.Remember(() => new TimePickerState(initialHour: 9, initialMinute: 30));
            return new Column
            {
                new Text($"Picked time: {picked}"),
                new Button(onClick: () => open.Value = true) { new Text("Pick time") },
                open.Value
                    ? new TimePickerDialog(onDismissRequest: () => open.Value = false)
                    {
                        Title         = new Text("Pick a time"),
                        ConfirmButton = new Button(onClick: () =>
                        {
                            picked.Value = $"{state.Hour:D2}:{state.Minute:D2}";
                            open.Value = false;
                        }) { new Text("OK") },
                        DismissButton = new Button(onClick: () => open.Value = false) { new Text("Cancel") },
                        Body          = new TimePicker(state),
                    }
                    : (ComposableNode?)null,
            };
        });
}
