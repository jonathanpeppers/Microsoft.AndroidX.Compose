using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.DialogsSheets;

/// <summary>DatePickerDialog — calendar picker wrapped in a dialog frame.</summary>
public static class DatePickerDialogDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "dialogs-date-picker",
        CategoryId:  "dialogs-sheets",
        Title:       "DatePickerDialog",
        Description: "Tap Pick date to open a calendar dialog. Read SelectedDateMillis on confirm.",
        Build:       c =>
        {
            var open   = c.MutableStateOf(false);
            var picked = c.MutableStateOf("(none)");
            var state  = c.Remember(() => new DatePickerState());
            return new Column
            {
                new Text($"Picked date: {picked}"),
                new Button(onClick: () => open.Value = true) { new Text("Pick date") },
                open.Value
                    ? new DatePickerDialog(onDismissRequest: () => open.Value = false)
                    {
                        ConfirmButton = new Button(onClick: () =>
                        {
                            picked.Value = state.SelectedDateMillis is long ms
                                ? DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime.ToString("yyyy-MM-dd")
                                : "(none)";
                            open.Value = false;
                        }) { new Text("OK") },
                        DismissButton = new Button(onClick: () => open.Value = false) { new Text("Cancel") },
                        Body          = new DatePicker(state),
                    }
                    : (ComposableNode?)null,
            };
        });
}
