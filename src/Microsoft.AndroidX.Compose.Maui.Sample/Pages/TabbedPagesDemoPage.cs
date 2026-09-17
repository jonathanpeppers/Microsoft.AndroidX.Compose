using AndroidSpecific = Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific;

namespace Microsoft.AndroidX.Compose.Maui.Sample.Pages;

/// <summary>
/// Launches top- and bottom-placement <see cref="TabbedPage"/> instances that
/// exercise swipe selection, programmatic selection, tab metadata, and dynamic
/// child changes through the Compose-backed tab handler.
/// </summary>
public sealed class TabbedPagesDemoPage : ContentPage
{
    /// <summary>Construct the tabbed-page acceptance demo.</summary>
    public TabbedPagesDemoPage()
    {
        Title = "Tabbed pages";
        Content = new VerticalStackLayout
        {
            Padding = new Thickness(24),
            Spacing = 16,
            Children =
            {
                new Label
                {
                    Text = "Open either placement, then tap or swipe between pages. " +
                           "Each tab can also change the selected page and mutate the child collection.",
                },
                new Button
                {
                    Text = "Open top tabs",
                    Command = new Command(async () => await OpenTabsAsync(bottom: false)),
                },
                new Button
                {
                    Text = "Open bottom tabs",
                    Command = new Command(async () => await OpenTabsAsync(bottom: true)),
                },
            },
        };
    }

    async Task OpenTabsAsync(bool bottom)
    {
        var tabs = BuildTabs(bottom);
        await Navigation.PushModalAsync(tabs);
    }

    static TabbedPage BuildTabs(bool bottom)
    {
        var tabs = new TabbedPage
        {
            Title = bottom ? "Bottom tabs" : "Top tabs",
            BarBackgroundColor = Color.FromArgb("#E8DEF8"),
            SelectedTabColor = Color.FromArgb("#4F378B"),
            UnselectedTabColor = Color.FromArgb("#49454F"),
        };
        AndroidSpecific.TabbedPage.SetToolbarPlacement(
            tabs,
            bottom ? AndroidSpecific.ToolbarPlacement.Bottom : AndroidSpecific.ToolbarPlacement.Top);

        ContentPage? dynamicPage = null;
        var status = new Label { Text = "Selected: Home" };
        var settings = new ContentPage
        {
            Title = "Settings",
            IconImageSource = "dotnet_bot.png",
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(24),
                Spacing = 12,
                Children =
                {
                    new Label { Text = "Swipe or tap Home to return." },
                    new Button
                    {
                        Text = "Select Home programmatically",
                        Command = new Command(() => tabs.CurrentPage = tabs.Children[0]),
                    },
                },
            },
        };
        var home = new ContentPage
        {
            Title = "Home",
            IconImageSource = "dotnet_bot.png",
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(24),
                Spacing = 12,
                Children =
                {
                    new Label { Text = bottom ? "Bottom NavigationBar" : "Top TabRow" },
                    status,
                    new Button
                    {
                        Text = "Select Settings programmatically",
                        Command = new Command(() => tabs.CurrentPage = tabs.Children[1]),
                    },
                    new Button
                    {
                        Text = "Add dynamic tab",
                        Command = new Command(ToggleDynamicTab),
                    },
                    new Button
                    {
                        Text = "Rename Settings tab",
                        Command = new Command(() =>
                            settings.Title = settings.Title == "Settings" ? "Preferences" : "Settings"),
                    },
                    new Button
                    {
                        Text = "Close",
                        Command = new Command(async () => await tabs.Navigation.PopModalAsync()),
                    },
                },
            },
        };
        var disabled = new ContentPage
        {
            Title = "Events: 000",
            IsEnabled = false,
            Content = new Label
            {
                Text = "This page starts disabled and cannot be selected from its tab.",
                Margin = new Thickness(24),
            },
        };

        tabs.Children.Add(home);
        tabs.Children.Add(settings);
        tabs.Children.Add(disabled);
        int selectionChangeCount = 0;
        tabs.CurrentPageChanged += (_, _) =>
        {
            selectionChangeCount++;
            status.Text = $"Selected: {tabs.CurrentPage?.Title ?? "(none)"}";
            disabled.Title = $"Events: {selectionChangeCount:D3}";
        };
        return tabs;

        void ToggleDynamicTab()
        {
            if (dynamicPage is not null && tabs.Children.Contains(dynamicPage))
            {
                tabs.Children.Remove(dynamicPage);
                dynamicPage = null;
                return;
            }

            dynamicPage = CreateDynamicTab();
            tabs.Children.Add(dynamicPage);
        }

        ContentPage CreateDynamicTab()
        {
            var page = new ContentPage { Title = "Dynamic" };
            page.Content = new VerticalStackLayout
            {
                Padding = new Thickness(24),
                Children =
                {
                    new Label { Text = "Added after the handler was connected." },
                    new Button
                    {
                        Text = "Rename tab",
                        Command = new Command(() =>
                            page.Title = page.Title == "Dynamic" ? "Renamed" : "Dynamic"),
                    },
                },
            };
            return page;
        }
    }
}
