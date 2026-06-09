using Microsoft.AndroidX.Compose.Gallery.Registry;

namespace Microsoft.AndroidX.Compose.Gallery.Demos.DialogsSheets;

/// <summary>DateRangePickerDialog — pick a start + end date in one dialog.</summary>
public static class DateRangePickerDialogDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "dialogs-date-range-picker",
        CategoryId:  "dialogs-sheets",
        Title:       "DateRangePickerDialog",
        Description: "Confirm reads SelectedStartDateMillis + SelectedEndDateMillis.",
        Build:       () =>
        {
            var open   = ComposeRuntime.Remember(() => new MutableState<bool>(false));
            var picked = ComposeRuntime.Remember(() => new MutableState<string>("(none)"));
            var state  = ComposeRuntime.Remember(() => new DateRangePickerState());
            return new Column
            {
                new Text($"Picked range: {picked}"),
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
                        Body          = new DateRangePicker(state),
                    }
                    : (ComposableNode?)null,
            };
        });
}
