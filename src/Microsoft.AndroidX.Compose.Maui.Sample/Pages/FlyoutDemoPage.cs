namespace Microsoft.AndroidX.Compose.Maui.Sample.Pages;

/// <summary>
/// Launches a <see cref="FlyoutPage"/> that exercises every real
/// <see cref="FlyoutLayoutBehavior"/>, programmatic and gesture presentation,
/// adaptive width, child replacement, and nested navigation content.
/// </summary>
public sealed class FlyoutDemoPage : ContentPage
{
    readonly Picker _behavior;
    readonly Label _status;

    /// <summary>Construct the FlyoutPage acceptance launcher.</summary>
    public FlyoutDemoPage()
    {
        Title = "FlyoutPage";

        _behavior = new Picker
        {
            Title = "FlyoutLayoutBehavior",
            ItemsSource = Enum.GetValues<FlyoutLayoutBehavior>(),
            SelectedItem = FlyoutLayoutBehavior.Default,
        };
        _status = new Label
        {
            Text = "Default adapts to device idiom and orientation.",
            FontSize = 13,
        };
        _behavior.SelectedIndexChanged += (_, _) =>
        {
            if (_behavior.SelectedItem is FlyoutLayoutBehavior behavior)
                _status.Text = Describe(behavior);
        };

        var launch = new Button
        {
            Text = "Launch Compose FlyoutPage",
            HorizontalOptions = LayoutOptions.Fill,
        };
        launch.Clicked += OnLaunchClicked;

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(30),
                Spacing = 18,
                Children =
                {
                    new Label
                    {
                        Text = "Compose-backed FlyoutPage",
                        FontSize = 28,
                        FontAttributes = FontAttributes.Bold,
                    },
                    new Label
                    {
                        Text = "Choose a real MAUI FlyoutLayoutBehavior. MAUI resolves it to modal Flyout or permanent Locked before the Compose handler renders.",
                    },
                    _behavior,
                    _status,
                    launch,
                },
            },
        };
    }

    async void OnLaunchClicked(object? sender, EventArgs e)
    {
        var behavior = _behavior.SelectedItem is FlyoutLayoutBehavior selected
            ? selected
            : FlyoutLayoutBehavior.Default;
        await Navigation.PushModalAsync(BuildFlyout(behavior));
    }

    static FlyoutPage BuildFlyout(FlyoutLayoutBehavior behavior)
    {
        var host = new FlyoutPage
        {
            FlyoutLayoutBehavior = behavior,
            IsGestureEnabled = true,
        };

        var presentation = new Label { Text = "IsPresented: false" };
        host.IsPresentedChanged += (_, _) =>
            presentation.Text = $"IsPresented: {host.IsPresented}";

        var open = new Button { Text = "Open flyout (IsPresented = true)" };
        open.Clicked += (_, _) => host.IsPresented = true;

        var close = new Button { Text = "Close flyout (IsPresented = false)" };
        close.Clicked += (_, _) =>
        {
            if (((IFlyoutView)host).FlyoutBehavior == FlyoutBehavior.Locked)
                return;
            host.IsPresented = false;
        };

        var reverse = new Button { Text = "Open then close immediately" };
        reverse.Clicked += (_, _) =>
        {
            host.IsPresented = true;
            host.IsPresented = false;
        };

        var gestures = new Switch { IsToggled = true };
        gestures.Toggled += (_, e) => host.IsGestureEnabled = e.Value;

        var push = new Button { Text = "Push nested detail page" };
        push.Clicked += async (_, _) =>
            await ((NavigationPage)host.Detail).PushAsync(new ContentPage
            {
                Title = "Nested detail",
                Content = new VerticalStackLayout
                {
                    Padding = new Thickness(30),
                    Children =
                    {
                        new Label { Text = "Nested NavigationPage content remains hosted by its own MAUI handler." },
                        new Button
                        {
                            Text = "Open flyout from nested page",
                            Command = new Command(() => host.IsPresented = true),
                        },
                    },
                },
            });

        var replaceDetail = new Button { Text = "Replace detail child page" };
        replaceDetail.Clicked += (_, _) =>
            host.Detail = new NavigationPage(new ContentPage
            {
                Title = "Replacement detail",
                Content = new VerticalStackLayout
                {
                    Padding = new Thickness(30),
                    Spacing = 14,
                    Children =
                    {
                        new Label { Text = "The Detail mapper replaced the hosted child without rebuilding the outer drawer." },
                        new Button
                        {
                            Text = "Open flyout",
                            Command = new Command(() => host.IsPresented = true),
                        },
                    },
                },
            });

        host.Detail = new NavigationPage(new ContentPage
        {
            Title = "Flyout detail",
            Content = new ScrollView
            {
                Content = new VerticalStackLayout
                {
                    Padding = new Thickness(24),
                    Spacing = 14,
                    Children =
                    {
                        new Label
                        {
                            Text = $"Detail: {behavior}",
                            FontSize = 24,
                            FontAttributes = FontAttributes.Bold,
                        },
                        presentation,
                        open,
                        close,
                        reverse,
                        new HorizontalStackLayout
                        {
                            Spacing = 12,
                            Children =
                            {
                                new Label { Text = "Edge swipe enabled", VerticalOptions = LayoutOptions.Center },
                                gestures,
                            },
                        },
                        push,
                        replaceDetail,
                        new Button
                        {
                            Text = "Close modal sample",
                            Command = new Command(async () => await host.Navigation.PopModalAsync()),
                        },
                    },
                },
            },
        });

        host.Flyout = BuildMenu(host, "Primary menu");
        return host;
    }

    static ContentPage BuildMenu(FlyoutPage host, string title)
    {
        var replace = new Button { Text = "Replace flyout content" };
        replace.Clicked += (_, _) =>
            host.Flyout = BuildMenu(host, "Replacement menu");

        var select = new Button { Text = "Select item and close" };
        select.Clicked += (_, _) =>
        {
            ((NavigationPage)host.Detail).Navigation.InsertPageBefore(
                new ContentPage
                {
                    Title = "Selected detail",
                    Content = new Label
                    {
                        Text = "Flyout selection replaced the nested root content.",
                        Margin = new Thickness(30),
                    },
                },
                ((NavigationPage)host.Detail).RootPage);
            _ = ((NavigationPage)host.Detail).PopToRootAsync(animated: false);
            if (((IFlyoutView)host).FlyoutBehavior != FlyoutBehavior.Locked)
                host.IsPresented = false;
        };

        var reverse = new Button { Text = "Close then open immediately" };
        reverse.Clicked += (_, _) =>
        {
            host.IsPresented = false;
            host.IsPresented = true;
        };

        return new ContentPage
        {
            Title = title,
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(24),
                Spacing = 14,
                Children =
                {
                    new Label
                    {
                        Text = title,
                        FontSize = 24,
                        FontAttributes = FontAttributes.Bold,
                    },
                    new Label { Text = "The handler applies MAUI's computed phone/tablet flyout width to this Material 3 sheet." },
                    select,
                    reverse,
                    replace,
                },
            },
        };
    }

    static string Describe(FlyoutLayoutBehavior behavior) => behavior switch
    {
        FlyoutLayoutBehavior.Default =>
            "Default: split on eligible non-phone landscape displays; popover otherwise.",
        FlyoutLayoutBehavior.SplitOnLandscape =>
            "SplitOnLandscape: permanent on eligible non-phone landscape displays.",
        FlyoutLayoutBehavior.Split =>
            "Split: permanent on non-phone displays; MAUI keeps phones modal.",
        FlyoutLayoutBehavior.Popover =>
            "Popover: always modal with edge swipe controlled by IsGestureEnabled.",
        FlyoutLayoutBehavior.SplitOnPortrait =>
            "SplitOnPortrait: permanent on eligible non-phone portrait displays.",
        _ => behavior.ToString(),
    };
}
