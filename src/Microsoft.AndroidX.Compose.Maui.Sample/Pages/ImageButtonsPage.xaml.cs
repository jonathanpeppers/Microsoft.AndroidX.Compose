using System.Diagnostics;

namespace Microsoft.AndroidX.Compose.Maui.Sample.Pages;

/// <summary>
/// ImageButton demo — three buttons covering FileImageSource (drawable
/// fast path), UriImageSource (download), and FontImageSource (glyph
/// rasterisation). Logs each <see cref="ImageButton.Pressed"/> /
/// <see cref="ImageButton.Released"/> / <see cref="ImageButton.Clicked"/>
/// event so the order can be verified visually and via
/// <see cref="Debug.WriteLine(string)"/>.
/// </summary>
public partial class ImageButtonsPage : ContentPage
{
    int _clickCount;
    readonly Queue<string> _log = new();

    /// <summary>Build the page.</summary>
    public ImageButtonsPage()
    {
        InitializeComponent();
    }

    void OnAnyPressed (object? sender, EventArgs e) => Append(sender, "Pressed");
    void OnAnyReleased(object? sender, EventArgs e) => Append(sender, "Released");
    void OnAnyClicked (object? sender, EventArgs e)
    {
        _clickCount++;
        ClickCountLabel.Text = $"Total clicks: {_clickCount}";
        Append(sender, "Clicked");
    }

    void Append(object? sender, string evt)
    {
        var name = sender switch
        {
            ImageButton ib when ib == FileButton => "File",
            ImageButton ib when ib == UriButton  => "Uri",
            ImageButton ib when ib == FontButton => "Font",
            _                                    => "?",
        };
        var line = $"[{DateTime.Now:HH:mm:ss.fff}] {name}: {evt}";
        Debug.WriteLine($"ImageButtonsPage: {line}");

        _log.Enqueue(line);
        while (_log.Count > 8)
            _log.Dequeue();

        EventLog.Text = string.Join('\n', _log.Reverse());
    }
}
