namespace Microsoft.AndroidX.Compose.Maui.Sample.Pages;

using System.ComponentModel;

/// <summary>
/// Pickers demo — exercises every mapper on
/// <see cref="Microsoft.AndroidX.Compose.Maui.Handlers.PickerHandler"/>,
/// <see cref="Microsoft.AndroidX.Compose.Maui.Handlers.DatePickerHandler"/>,
/// and <see cref="Microsoft.AndroidX.Compose.Maui.Handlers.TimePickerHandler"/>:
/// ItemsSource + SelectedIndex (Picker, two-way echo via SelectedIndexChanged),
/// Date + MinimumDate + MaximumDate + Format (DatePicker, two-way echo via
/// DateSelected), Time + Format (TimePicker, two-way echo via the
/// <c>PropertyChanged</c> event surfaced by <see cref="BindableObject"/>),
/// and a Reset button that pushes new values back through the handlers.
/// </summary>
public partial class PickersPage : ContentPage
{
    static readonly TimeSpan s_defaultAlarm = new(7, 30, 0);
    static readonly DateTime s_defaultDate  = new(2000, 1, 1);

    /// <summary>Build the page.</summary>
    public PickersPage()
    {
        InitializeComponent();

        // Clamp the date picker to ±2 years from today so the
        // MinimumDate / MaximumDate mappers are easy to verify.
        var today = DateTime.Today;
        BirthDate.MinimumDate = today.AddYears(-2);
        BirthDate.MaximumDate = today.AddYears(2);
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
        BirthDate.Date            = s_defaultDate;
        DateEcho.Text             = "No date picked yet.";
        AlarmTime.Time            = s_defaultAlarm;
        TimeEcho.Text             = "No time picked yet.";
    }
}
