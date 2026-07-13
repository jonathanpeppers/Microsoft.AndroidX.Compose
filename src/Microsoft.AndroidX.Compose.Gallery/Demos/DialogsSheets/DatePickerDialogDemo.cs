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
        Description: "Tap Pick date to open a calendar dialog. Past + far-future dates are blocked via SelectableDates; read SelectedDateMillis on confirm.",
        Build:       c =>
        {
            var open      = c.MutableStateOf(false);
            var picked    = c.MutableStateOf("(none)");
            var showModes = c.MutableStateOf(true);
            // One JCW adapter per demo instance — its identity is part
            // of the rememberDatePickerState cache key.
            var bounds    = c.Remember(() => new DateRangeSelectableDates());
            var todayUtc  = DateTimeOffset.UtcNow.Date;
            bounds.MinUtcMillis = new DateTimeOffset(todayUtc).ToUnixTimeMilliseconds();
            bounds.MaxUtcMillis = new DateTimeOffset(todayUtc.AddDays(30)).ToUnixTimeMilliseconds();
            var state     = c.Remember(() => new DatePickerState(
                initialSelectedDateMillis: bounds.MinUtcMillis,
                initialSelectableDates:    bounds));
            return new Column
            {
                new Text($"Picked date: {picked}"),
                new Text("(Today through Today+30 are selectable.)"),
                new Row(horizontalArrangement: Arrangement.SpacedBy(8.Dp()),
                        verticalAlignment: Alignment.Vertical.CenterVertically)
                {
                    new Switch(@checked: showModes.Value, onCheckedChange: value => showModes.Value = value),
                    new Text("Show mode toggle"),
                },
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
                        Body          = new DatePicker(state, showModeToggle: showModes.Value),
                    }
                    : (ComposableNode?)null,
            };
        });
}
