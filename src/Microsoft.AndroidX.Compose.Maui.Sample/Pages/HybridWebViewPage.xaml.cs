namespace Microsoft.AndroidX.Compose.Maui.Sample.Pages;

/// <summary>
/// HybridWebView demo — boots a tiny HTML root and round-trips a
/// message through MAUI's <c>HybridWebView.SendRawMessage</c> /
/// <c>RawMessageReceived</c> bridge to prove the JS bridge survives
/// being hosted inside Compose <see cref="AndroidX.Compose.AndroidView"/>.
/// </summary>
public partial class HybridWebViewPage : ContentPage
{
    /// <summary>Build the page.</summary>
    public HybridWebViewPage()
    {
        InitializeComponent();
    }

    void OnSendClicked(object? sender, EventArgs e) => HybridView.SendRawMessage("ping");

    void OnRawMessageReceived(object? sender, HybridWebViewRawMessageReceivedEventArgs e)
    {
        EchoLabel.Text = $"JS sent: {e.Message}";
    }
}
