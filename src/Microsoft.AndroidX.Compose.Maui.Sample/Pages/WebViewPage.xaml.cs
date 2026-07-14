namespace Microsoft.AndroidX.Compose.Maui.Sample.Pages;

/// <summary>
/// Demo page for the Compose-backed <c>WebViewHandler</c>. Exercises
/// URL vs HTML source, navigation commands, JS evaluation, and
/// cross-cutting <c>Modifier</c> propagation (Opacity slider).
/// </summary>
public partial class WebViewPage : ContentPage
{
    /// <summary>Build the page from the XAML.</summary>
    public WebViewPage()
    {
        InitializeComponent();
    }

    void OnLoadUrlClicked(object? sender, EventArgs e)
    {
        Web.Source = new UrlWebViewSource { Url = "https://www.example.com" };
        StatusLabel.Text = "Loaded URL source: https://www.example.com";
    }

    void OnLoadHtmlClicked(object? sender, EventArgs e)
    {
        Web.Source = new HtmlWebViewSource
        {
            Html =
                """
                <!doctype html>
                <html>
                <head>
                  <title>Inline HTML</title>
                  <style>
                    body { font: 16px sans-serif; padding: 24px;
                           background: linear-gradient(135deg,#673AB7,#2196F3);
                           color: #fff; }
                    h1 { margin-top: 0; }
                    button { font-size: 14px; padding: 8px 14px; border-radius: 6px;
                             border: 0; background: rgba(255,255,255,0.18);
                             color: #fff; }
                  </style>
                </head>
                <body>
                  <h1>Inline HTML source</h1>
                  <p>This document was loaded via <code>HtmlWebViewSource.Html</code>.</p>
                  <button onclick="window.location.href='https://www.example.com'">
                    Navigate to example.com
                  </button>
                </body>
                </html>
                """,
        };
        StatusLabel.Text = "Loaded inline HTML source.";
    }

    void OnBackClicked(object? sender, EventArgs e)
    {
        if (Web.CanGoBack)
        {
            Web.GoBack();
            StatusLabel.Text = "Navigated back.";
        }
        else
        {
            StatusLabel.Text = "Can't go back — no prior page.";
        }
    }

    void OnForwardClicked(object? sender, EventArgs e)
    {
        if (Web.CanGoForward)
        {
            Web.GoForward();
            StatusLabel.Text = "Navigated forward.";
        }
        else
        {
            StatusLabel.Text = "Can't go forward — no next page.";
        }
    }

    void OnReloadClicked(object? sender, EventArgs e)
    {
        Web.Reload();
        StatusLabel.Text = "Reloaded.";
    }

    async void OnEvalClicked(object? sender, EventArgs e)
    {
        try
        {
            var title = await Web.EvaluateJavaScriptAsync("document.title");
            StatusLabel.Text = $"document.title = {title ?? "<null>"}";
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"Eval failed: {ex.Message}";
        }
    }

    void OnOpacityChanged(object? sender, ValueChangedEventArgs e)
    {
        Web.Opacity = e.NewValue;
    }
}
