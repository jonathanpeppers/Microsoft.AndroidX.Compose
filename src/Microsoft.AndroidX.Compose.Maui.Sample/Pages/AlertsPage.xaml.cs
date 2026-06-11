namespace Microsoft.AndroidX.Compose.Maui.Sample.Pages;

/// <summary>
/// Exercises <see cref="Page.DisplayAlert(string, string, string)"/> /
/// <see cref="Page.DisplayActionSheet(string, string, string, string[])"/>
/// / <see cref="Page.DisplayPromptAsync(string, string, string, string, string, int, Keyboard, string)"/>.
/// All three are intercepted by
/// <c>ComposeAlertManagerSubscription</c> so the visible dialog is a
/// Compose <c>AlertDialog</c> / <c>ModalBottomSheet</c>, not the
/// stock AppCompat dialog.
/// </summary>
public partial class AlertsPage : ContentPage
{
    /// <summary>Build the page.</summary>
    public AlertsPage()
    {
        InitializeComponent();
    }

    async void OnShowAlertClicked(object? sender, EventArgs e)
    {
        // bool DisplayAlertAsync(string title, string message,
        //                        string accept, string cancel)
        var ok = await DisplayAlertAsync(
            "Confirm reset",
            "This clears every demo's local state. Continue?",
            "OK",
            "Cancel");
        ResultLabel.Text = $"DisplayAlertAsync → {(ok ? "OK" : "Cancel")}";
    }

    async void OnShowActionSheetClicked(object? sender, EventArgs e)
    {
        // string DisplayActionSheetAsync(string title, string cancel,
        //                                string destruction,
        //                                params string[] buttons)
        var pick = await DisplayActionSheetAsync(
            "Pick one",
            "Cancel",
            "Delete",
            "Edit", "Archive", "Share");
        ResultLabel.Text = $"DisplayActionSheetAsync → {pick ?? "(null)"}";
    }

    async void OnShowPromptClicked(object? sender, EventArgs e)
    {
        // string DisplayPromptAsync(string title, string message,
        //                           string accept, string cancel,
        //                           string placeholder, int maxLength,
        //                           Keyboard keyboard, string initialValue)
        var name = await DisplayPromptAsync(
            "Your name",
            "Enter the name to greet:",
            "OK",
            "Cancel",
            "name",
            -1,
            Keyboard.Default,
            "World");
        ResultLabel.Text = $"DisplayPromptAsync → {name ?? "(null — cancel)"}";
    }

    async void OnShowSingleButtonClicked(object? sender, EventArgs e)
    {
        // void DisplayAlertAsync(string title, string message, string cancel)
        await DisplayAlertAsync(
            "All done",
            "This alert has only an OK button — no DismissButton slot.",
            "OK");
        ResultLabel.Text = "DisplayAlertAsync (single button) → dismissed";
    }
}
