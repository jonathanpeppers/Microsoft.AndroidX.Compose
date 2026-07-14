using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.DialogsSheets;

/// <summary>DateRangePickerDialog — pick a start + end date in one dialog.</summary>
public static class DateRangePickerDialogDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "dialogs-date-range-picker",
        CategoryId:  "dialogs-sheets",
        Title:       "DateRangePickerDialog",
        Description: "Confirm reads SelectedStartDateMillis + SelectedEndDateMillis.",
        Build:       c =>
        {
            var open   = c.MutableStateOf(false);
            var picked = c.MutableStateOf("(none)");
            var state  = c.Remember(() => new DateRangePickerState());
            var showModes = c.MutableStateOf(true);
            return new Column
            {
                new Text($"Picked range: {picked}"),
                new Row(horizontalArrangement: Arrangement.SpacedBy(8.Dp()),
                        verticalAlignment: Alignment.Vertical.CenterVertically)
                {
                    new Switch(@checked: showModes.Value, onCheckedChange: value => showModes.Value = value),
                    new Text("Show mode toggle"),
                },
                new Button(onClick: () => open.Value = true) { new Text("Pick range") },
                open.Value
                    ? new DateRangePickerDialog(onDismissRequest: () => open.Value = false)
                    {
                        ConfirmButton = new Button(onClick: () =>
                        {
                            static string Fmt(long? ms) => ms is long m
                                ? DateTimeOffset.FromUnixTimeMilliseconds(m).UtcDateTime.ToString("yyyy-MM-dd")
                                : "(none)";
                            picked.Value =
                                $"{Fmt(state.SelectedStartDateMillis)} → {Fmt(state.SelectedEndDateMillis)}";
                            open.Value = false;
                        }) { new Text("OK") },
                        DismissButton = new Button(onClick: () => open.Value = false) { new Text("Cancel") },
                        Body          = new DateRangePicker(state, showModeToggle: showModes.Value),
                    }
                    : (ComposableNode?)null,
            };
        });
}
