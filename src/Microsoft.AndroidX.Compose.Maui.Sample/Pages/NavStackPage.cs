namespace Microsoft.AndroidX.Compose.Maui.Sample.Pages;

/// <summary>
/// A single page hosted inside the Phase 4 Slice 1 modal
/// <see cref="NavigationPage"/>. Each instance pushes a deeper
/// copy on tap so the user can verify the back arrow + hardware
/// back button pop one level at a time. The "Close modal" button
/// pops the entire modal stack to return to the gallery.
/// </summary>
/// <remarks>
/// Built entirely in code so the gallery doesn't need an extra
/// XAML pair per depth level. <see cref="ContentPage.Title"/> is
/// what Compose's <c>TopAppBar</c> reads in
/// <see cref="Handlers.NavigationPageHandler"/> — verify the
/// title swaps on every push / pop.
/// </remarks>
public sealed class NavStackPage : ContentPage
{
    /// <summary>Construct a stack page at <paramref name="depth"/>.</summary>
    /// <param name="depth">1-based depth inside the modal navigation stack.</param>
    public NavStackPage(int depth)
    {
        Title = $"Stack page {depth}";

        var header = new Label
        {
            Text     = $"Depth {depth}",
            FontSize = 28,
            FontAttributes = FontAttributes.Bold,
            HorizontalTextAlignment = TextAlignment.Center,
        };

        var caption = new Label
        {
            Text = depth == 1
                ? "You're at the root of the modal navigation stack. The top bar shows the page Title; there's no back arrow at depth 1."
                : "Tap the ← arrow in the top bar (or hardware back) to pop back to the previous page in the stack.",
            HorizontalTextAlignment = TextAlignment.Center,
            FontSize = 14,
        };

        var pushBtn = new Button
        {
            Text = "Push next",
            HorizontalOptions = LayoutOptions.Fill,
        };
        pushBtn.Clicked += async (_, _) =>
            await Navigation.PushAsync(new NavStackPage(depth + 1));

        var popBtn = new Button
        {
            Text = "Pop (Navigation.PopAsync)",
            IsEnabled = depth > 1,
            HorizontalOptions = LayoutOptions.Fill,
        };
        popBtn.Clicked += async (_, _) =>
        {
            if (Navigation.NavigationStack.Count > 1)
                await Navigation.PopAsync();
        };

        var closeBtn = new Button
        {
            Text = "Close modal",
            BackgroundColor = Color.FromArgb("#B00020"),
            TextColor = Colors.White,
            HorizontalOptions = LayoutOptions.Fill,
        };
        closeBtn.Clicked += async (_, _) =>
            await Navigation.PopModalAsync();

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(30, 30),
                Spacing = 20,
                Children =
                {
                    header,
                    caption,
                    pushBtn,
                    popBtn,
                    closeBtn,
                },
            },
        };
    }
}
