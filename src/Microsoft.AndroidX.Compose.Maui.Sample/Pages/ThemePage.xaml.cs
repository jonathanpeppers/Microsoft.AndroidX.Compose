namespace Microsoft.AndroidX.Compose.Maui.Sample.Pages;

/// <summary>
/// Theme demo — flips
/// <see cref="Microsoft.Maui.Controls.Application.UserAppTheme"/>
/// between Light, Dark, and Unspecified. Confirms the Compose-backed
/// <see cref="Microsoft.AndroidX.Compose.Maui.Platform.ThemeManager"/>
/// bridges MAUI's <c>RequestedThemeChanged</c> event into the
/// page-rooted Material 3 <c>ColorScheme</c>.
/// </summary>
public partial class ThemePage : ContentPage
{
    /// <summary>Build the page and seed the status label.</summary>
    public ThemePage()
    {
        InitializeComponent();
        UpdateStatus();
    }

    void OnRequestedThemeChanged(object? sender, AppThemeChangedEventArgs e) =>
        UpdateStatus();

    /// <inheritdoc/>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (Application.Current is { } app)
            app.RequestedThemeChanged += OnRequestedThemeChanged;
        UpdateStatus();
    }

    /// <inheritdoc/>
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (Application.Current is { } app)
            app.RequestedThemeChanged -= OnRequestedThemeChanged;
    }

    void UpdateStatus()
    {
        var app = Application.Current;
        StatusLabel.Text =
            $"UserAppTheme: {app?.UserAppTheme}  ·  RequestedTheme: {app?.RequestedTheme}";
    }

    void OnLightClicked(object? sender, EventArgs e)
    {
        if (Application.Current is { } app) app.UserAppTheme = AppTheme.Light;
        UpdateStatus();
    }

    void OnDarkClicked(object? sender, EventArgs e)
    {
        if (Application.Current is { } app) app.UserAppTheme = AppTheme.Dark;
        UpdateStatus();
    }

    void OnUnspecifiedClicked(object? sender, EventArgs e)
    {
        if (Application.Current is { } app) app.UserAppTheme = AppTheme.Unspecified;
        UpdateStatus();
    }

    void OnSampleButtonClicked(object? sender, EventArgs e)
    {
        // No-op: the buttons are visual probes for theme flips.
    }
}
