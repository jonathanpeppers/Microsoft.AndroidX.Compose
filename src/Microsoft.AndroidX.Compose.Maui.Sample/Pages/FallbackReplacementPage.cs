namespace Microsoft.AndroidX.Compose.Maui.Sample.Pages;

/// <summary>
/// Exercises replacement and removal of stock MAUI views hosted by a
/// Compose-backed <see cref="ContentView"/>.
/// </summary>
public sealed class FallbackReplacementPage : ContentPage
{
    readonly Grid _viewA;
    readonly Grid _viewB;
    readonly ContentView _fallbackHost;
    readonly Label _status;
    View? _current;
    bool _sequenceRunning;

    /// <summary>Construct the fallback replacement acceptance page.</summary>
    public FallbackReplacementPage()
    {
        Title = "Fallback replacement";

        _viewA = CreateFallbackView(
            "Fallback A",
            "Fixed-size stock Grid - red",
            Color.FromArgb("#B3261E"),
            "fallback-view-a");
        _viewB = CreateFallbackView(
            "Fallback B",
            "Fixed-size stock Grid - teal",
            Color.FromArgb("#006A6A"),
            "fallback-view-b");

        _status = new Label
        {
            AutomationId = "fallback-replacement-status",
            Text = "Showing A.",
            FontAttributes = FontAttributes.Bold,
        };
        _fallbackHost = new ContentView
        {
            AutomationId = "fallback-replacement-host",
            Content = _viewA,
            BackgroundColor = Color.FromArgb("#1F000000"),
            Padding = new Thickness(8),
        };
        _current = _viewA;

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(24),
                Spacing = 12,
                Children =
                {
                    new Label
                    {
                        Text = "ContentView fallback replacement",
                        FontSize = 26,
                        FontAttributes = FontAttributes.Bold,
                    },
                    new Label
                    {
                        Text = "The colored stock Grid must change immediately. Run the sequence to exercise A -> B in one AndroidView slot, the same B instance, null removal, and A/B replacement after restoration.",
                        FontSize = 13,
                    },
                    _status,
                    CreateButton("Show A", "fallback-show-a", () =>
                        SetContent(_viewA, "Showing A.")),
                    CreateButton("Show B", "fallback-show-b", () =>
                        SetContent(_viewB, "Showing B.")),
                    CreateButton("Reassign same instance", "fallback-reassign", ReassignCurrent),
                    CreateButton("Clear content", "fallback-clear", () =>
                        SetContent(null, "Content cleared; the host must be empty.")),
                    CreateButton("Run A/B/null sequence", "fallback-run-sequence", RunSequence),
                    _fallbackHost,
                },
            },
        };
    }

    static Grid CreateFallbackView(
        string title,
        string subtitle,
        Color background,
        string automationId) =>
        new()
        {
            AutomationId = automationId,
            WidthRequest = 280,
            HeightRequest = 140,
            HorizontalOptions = LayoutOptions.Center,
            BackgroundColor = background,
            Children =
            {
                new VerticalStackLayout
                {
                    Padding = new Thickness(18),
                    Spacing = 6,
                    VerticalOptions = LayoutOptions.Center,
                    Children =
                    {
                        new Label
                        {
                            Text = title,
                            TextColor = Colors.White,
                            FontSize = 24,
                            FontAttributes = FontAttributes.Bold,
                            HorizontalTextAlignment = TextAlignment.Center,
                        },
                        new Label
                        {
                            Text = subtitle,
                            TextColor = Colors.White,
                            HorizontalTextAlignment = TextAlignment.Center,
                        },
                    },
                },
            },
        };

    static Button CreateButton(string text, string automationId, Action action)
    {
        var button = new Button
        {
            AutomationId = automationId,
            Text = text,
            HorizontalOptions = LayoutOptions.Fill,
        };
        button.Clicked += (_, _) => action();
        return button;
    }

    void SetContent(View? content, string status)
    {
        _current = content;
        _fallbackHost.Content = content;
        _status.Text = status;
    }

    void ReassignCurrent()
    {
        _fallbackHost.Content = _current;
        _status.Text = _current is null
            ? "Reassigned the same null content."
            : $"Reassigned the same {_current.AutomationId} instance.";
    }

    async void RunSequence()
    {
        if (_sequenceRunning)
            return;

        _sequenceRunning = true;
        try
        {
            SetContent(_viewB, "1/5: replaced A with B.");
            await Task.Delay(750);
            SetContent(_viewB, "2/5: reassigned the same B instance.");
            await Task.Delay(750);
            SetContent(null, "3/5: cleared content.");
            await Task.Delay(750);
            SetContent(_viewA, "4/5: restored A after null.");
            await Task.Delay(750);
            SetContent(_viewB, "5/5: replaced A with B again.");
        }
        finally
        {
            _sequenceRunning = false;
        }
    }
}
