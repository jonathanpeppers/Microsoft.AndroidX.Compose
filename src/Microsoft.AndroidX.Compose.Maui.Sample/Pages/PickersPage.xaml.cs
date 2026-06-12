namespace Microsoft.AndroidX.Compose.Maui.Sample.Pages;

using System.ComponentModel;

/// <summary>
/// Pickers demo — exercises every mapper on
/// <see cref="Microsoft.AndroidX.Compose.Maui.Handlers.PickerHandler"/>,
/// <see cref="Microsoft.AndroidX.Compose.Maui.Handlers.DatePickerHandler"/>,
/// and <see cref="Microsoft.AndroidX.Compose.Maui.Handlers.TimePickerHandler"/>:
/// ItemsSource + SelectedIndex (Picker, two-way echo via SelectedIndexChanged),
/// Date + Format (DatePicker, two-way echo via DateSelected),
/// Time + Format (TimePicker, two-way echo via the
/// <c>PropertyChanged</c> event surfaced by <see cref="BindableObject"/>),
/// and a Reset button that pushes new values back through the handlers.
/// </summary>
public partial class PickersPage : ContentPage
{
    static readonly TimeSpan s_defaultAlarm = new(7, 30, 0);

    /// <summary>Build the page.</summary>
    public PickersPage()
    {
        InitializeComponent();

        // Seed the date picker with today and clamp the calendar to
        // a Today..Today+30 window. Phase 4b's RememberDatePickerState
        // lift wires MinimumDate / MaximumDate through to Compose's
        // SelectableDates so days outside this range render greyed
        // out (issue #264).
        var today = DateTime.Today;
        BirthDate.MinimumDate = today;
        BirthDate.MaximumDate = today.AddDays(30);
        BirthDate.Date        = today;

        AlarmTime.Time             = s_defaultAlarm;
        AlarmTime.PropertyChanged += OnAlarmTimePropertyChanged;
    }

    void OnFruitChanged(object? sender, EventArgs e)
    {
        FruitEcho.Text = FruitPicker.SelectedItem is string s
            ? $"You picked: {s}"
            : "No fruit picked yet.";
    }

    void OnDateSelected(object? sender, DateChangedEventArgs e)
    {
        DateEcho.Text = $"You picked: {e.NewDate:MMM d, yyyy}";
    }

    void OnAlarmTimePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(TimePicker.Time))
            return;
        TimeEcho.Text = $"Alarm set for: {AlarmTime.Time:hh\\:mm}";
    }

    void OnResetClicked(object? sender, EventArgs e)
    {
        FruitPicker.SelectedIndex = -1;
        FruitEcho.Text            = "No fruit picked yet.";
        // Reset within the [Today, Today + 30] window so the assignment
        // doesn't get clamped by the MAUI DatePicker's range validator.
        BirthDate.Date            = DateTime.Today;
        DateEcho.Text             = "No date picked yet.";
        AlarmTime.Time            = s_defaultAlarm;
        TimeEcho.Text             = "No time picked yet.";
    }
}
