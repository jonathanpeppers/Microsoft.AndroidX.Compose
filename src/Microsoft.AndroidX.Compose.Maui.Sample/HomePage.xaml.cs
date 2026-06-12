namespace Microsoft.AndroidX.Compose.Maui.Sample;

/// <summary>
/// One catalog entry surfaced on <see cref="HomePage"/>. Bound directly
/// from XAML via <c>x:DataType</c> so each row resolves <c>Title</c>,
/// <c>Subtitle</c>, <c>Accent</c>, and <c>Route</c> at compile time.
/// </summary>
/// <param name="Title">Bold row title (e.g. "Counter").</param>
/// <param name="Subtitle">Single-line description shown beneath the title.</param>
/// <param name="Accent">Color for the leading 6dp accent strip.</param>
/// <param name="Route">Shell route (e.g. <c>counter</c>) registered in
/// <c>AppShell.xaml.cs</c> via <c>Routing.RegisterRoute</c>.</param>
public sealed record DemoEntry(
    string Title,
    string Subtitle,
    Color Accent,
    string Route);

/// <summary>
/// Gallery home page — a flat list of every demo wired into the sample.
/// Tapping a row pushes the corresponding page onto Shell's nav stack via
/// <see cref="Shell.GoToAsync(string)"/>.
/// </summary>
public partial class HomePage : ContentPage
{
    /// <summary>Build the page and seed the demo list.</summary>
    public HomePage()
    {
        InitializeComponent();

        DemoList.ItemsSource = new[]
        {
            new DemoEntry(
                "Counter",
                "Button + Label, dynamic text update on click.",
                Color.FromArgb("#512BD4"),
                "counter"),
            new DemoEntry(
                "Buttons",
                "Default vs themed colors, hug vs fill width.",
                Color.FromArgb("#673AB7"),
                "buttons"),
            new DemoEntry(
                "Labels",
                "Color, size, bold, alignment, multi-line.",
                Color.FromArgb("#3F51B5"),
                "labels"),
            new DemoEntry(
                "Entries",
                "Plain, password, IMEs, styled, read-only.",
                Color.FromArgb("#2196F3"),
                "entries"),
            new DemoEntry(
                "Image: Aspects",
                "AspectFit / AspectFill / Fill / Center side-by-side.",
                Color.FromArgb("#009688"),
                "image-aspects"),
            new DemoEntry(
                "Image: Sources",
                "File / Uri / Stream / Font image source types.",
                Color.FromArgb("#E91E63"),
                "image-sources"),
            new DemoEntry(
                "Modifiers",
                "Opacity, Scale, Rotation, IsVisible, Translation, Clip, Shadow on a single Image.",
                Color.FromArgb("#FF5722"),
                "modifiers"),
            new DemoEntry(
                "Toggles",
                "CheckBox / Switch / RadioButton with two-way binding.",
                Color.FromArgb("#FF9800"),
                "toggles"),
            new DemoEntry(
                "Pickers",
                "Picker, DatePicker, TimePicker (state-holder dialogs).",
                Color.FromArgb("#FF5722"),
                "pickers"),
            new DemoEntry(
                "Theme",
                "Light / Dark / Unspecified — flips MaterialTheme via UserAppTheme.",
                Color.FromArgb("#795548"),
                "theme"),
            new DemoEntry(
                "Sliders & steppers",
                "Slider + Stepper, two-way binding, custom colors.",
                Color.FromArgb("#03A9F4"),
                "sliders"),
            new DemoEntry(
                "Progress & activity",
                "ProgressBar driven by a slider; ActivityIndicator toggle.",
                Color.FromArgb("#FFC107"),
                "progress"),
            new DemoEntry(
                "Editor",
                "Multi-line text input, word counter, read-only, max length.",
                Color.FromArgb("#FFC107"),
                "editor"),
            new DemoEntry(
                "Search",
                "SearchBar + filtered list + IME-Search event.",
                Color.FromArgb("#8D6E63"),
                "search"),
            new DemoEntry(
                "Image buttons",
                "File / Uri / Font sources, Pressed -> Clicked -> Released log.",
                Color.FromArgb("#607D8B"),
                "image-buttons"),
            new DemoEntry(
                "Visuals",
                "Border, BoxView, ContentView modifier-chain demos.",
                Color.FromArgb("#4CAF50"),
                "visuals"),
            new DemoEntry(
                "Alerts",
                "DisplayAlert / DisplayActionSheet / DisplayPromptAsync over Compose dialogs.",
                Color.FromArgb("#FF5722"),
                "alerts"),
            new DemoEntry(
                "Gestures",
                "Tap / Pan / Pinch / Swipe on Compose-folded leaves.",
                Color.FromArgb("#009688"),
                "gestures"),
        };
    }

    async void OnDemoSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not DemoEntry entry)
            return;

        // Clear selection immediately so the row doesn't stay highlighted
        // when the user navigates back.
        DemoList.SelectedItem = null;

        await Shell.Current.GoToAsync(entry.Route);
    }
}
