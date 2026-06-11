namespace Microsoft.AndroidX.Compose.Maui.Sample.Pages;

/// <summary>
/// Toggles demo — exercises every mapper on the three new
/// Phase 2 Slice 3 handlers
/// (<see cref="Microsoft.AndroidX.Compose.Maui.Handlers.CheckBoxHandler"/>,
/// <see cref="Microsoft.AndroidX.Compose.Maui.Handlers.SwitchHandler"/>,
/// <see cref="Microsoft.AndroidX.Compose.Maui.Handlers.RadioButtonHandler"/>).
/// </summary>
/// <remarks>
/// The echo labels are updated from each control's
/// <c>CheckedChanged</c> / <c>Toggled</c> event handler — toggling a
/// CheckBox / Switch / RadioButton on the device flips the bound
/// virtual-view property, which the event surfaces back into the
/// labels here.
/// </remarks>
public partial class TogglesPage : ContentPage
{
    /// <summary>Build the page.</summary>
    public TogglesPage()
    {
        InitializeComponent();
    }

    void OnDefaultCheckChanged(object? sender, CheckedChangedEventArgs e) =>
        UpdateCheckEcho();

    void OnColoredCheckChanged(object? sender, CheckedChangedEventArgs e) =>
        UpdateCheckEcho();

    void UpdateCheckEcho() =>
        CheckEcho.Text = $"Default: {DefaultCheck.IsChecked} · Pink: {ColoredCheck.IsChecked}";

    void OnDefaultSwitchToggled(object? sender, ToggledEventArgs e) =>
        UpdateSwitchEcho();

    void OnTintedSwitchToggled(object? sender, ToggledEventArgs e) =>
        UpdateSwitchEcho();

    void UpdateSwitchEcho() =>
        SwitchEcho.Text = $"Default: {DefaultSwitch.IsToggled} · Tinted: {TintedSwitch.IsToggled}";

    void OnFruitChanged(object? sender, CheckedChangedEventArgs e)
    {
        // RadioButton fires CheckedChanged on every member of the group
        // when one flips — the previous selection's event also fires
        // (with Value=false). Only update the echo when *this* radio
        // became the selected one.
        if (!e.Value || sender is not RadioButton rb)
            return;
        FruitEcho.Text = $"Selected: {rb.Value}";
    }
}
