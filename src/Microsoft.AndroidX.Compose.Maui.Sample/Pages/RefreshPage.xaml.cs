namespace Microsoft.AndroidX.Compose.Maui.Sample.Pages;

/// <summary>
/// Refresh demo — exercises
/// <see cref="Microsoft.AndroidX.Compose.Maui.Handlers.RefreshViewHandler"/>:
/// pull-to-refresh through Compose's <c>PullToRefreshBox</c>, with a
/// counter on the page title and the rebuilt mock-item list as
/// proof that <see cref="RefreshView.Refreshing"/> ran.
/// </summary>
public partial class RefreshPage : ContentPage
{
    int _refreshCount;

    /// <summary>Build the page.</summary>
    public RefreshPage()
    {
        InitializeComponent();
    }

    async void OnRefreshing(object? sender, EventArgs e)
    {
        _refreshCount++;
        Title = $"Refresh — pulled {_refreshCount}×";
        StatusLabel.Text = $"Pulled {_refreshCount} times";

        // Simulate async work: rebuild the mock list with timestamps
        // so the user can confirm the refresh actually fired.
        await Task.Delay(700);

        var stamp = DateTime.Now.ToString("HH:mm:ss");
        Item0.Text = $"• Item 1 (refreshed {stamp})";
        Item1.Text = $"• Item 2 (refreshed {stamp})";
        Item2.Text = $"• Item 3 (refreshed {stamp})";
        Item3.Text = $"• Item 4 (refreshed {stamp})";
        Item4.Text = $"• Item 5 (refreshed {stamp})";

        // Clear the refreshing state - mirrors the contract in MAUI's
        // stock RefreshView handler (the spinner stays until the
        // consumer flips IsRefreshing back to false).
        Refresh.IsRefreshing = false;
    }

    void OnRefreshEnabledToggled(object? sender, ToggledEventArgs e)
    {
        Refresh.IsRefreshEnabled = e.Value;
        EnabledLabel.Text = $"IsRefreshEnabled = {e.Value}";
    }
}
