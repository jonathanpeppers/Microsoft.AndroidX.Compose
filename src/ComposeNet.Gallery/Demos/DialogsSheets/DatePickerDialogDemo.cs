using ComposeNet.Gallery.Registry;

namespace ComposeNet.Gallery.Demos.DialogsSheets;

/// <summary>DatePickerDialog — calendar picker wrapped in a dialog frame.</summary>
public static class DatePickerDialogDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "dialogs-date-picker",
        CategoryId:  "dialogs-sheets",
        Title:       "DatePickerDialog",
        Description: "Tap Pick date to open a calendar dialog. Read SelectedDateMillis on confirm.",
        Build:       () =>
        {
            var open   = Compose.Remember(() => new MutableState<bool>(false));
            var picked = Compose.Remember(() => new MutableState<string>("(none)"));
            var state  = Compose.Remember(() => new DatePickerState());
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
                                ? System.DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime.ToString("yyyy-MM-dd")
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
