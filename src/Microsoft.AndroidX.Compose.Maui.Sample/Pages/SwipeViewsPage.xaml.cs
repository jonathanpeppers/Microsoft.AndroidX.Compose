using System.Collections.ObjectModel;
using Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific;
using MauiAndroid = Microsoft.Maui.Controls.PlatformConfiguration.Android;

namespace Microsoft.AndroidX.Compose.Maui.Sample.Pages;

/// <summary>
/// Acceptance page for the Compose-backed SwipeView and swipe-item handlers.
/// </summary>
public partial class SwipeViewsPage : ContentPage
{
    int _addedItems;
    int _contentTaps;

    /// <summary>Rows used by the nested CollectionView gesture-arbitration sample.</summary>
    public ObservableCollection<SwipeRow> Rows { get; } =
    [
        new("Alpha", "Swipe horizontally; drag vertically to scroll."),
        new("Bravo", "Open this row, then scroll to close it."),
        new("Charlie", "Variable text keeps row templates non-identical."),
        new("Delta", "CollectionView publishes real Compose scroll deltas."),
        new("Echo", "Actions remain independently invokable."),
        new("Foxtrot", "Last row for a full viewport scroll."),
    ];

    /// <summary>Build the page and select Android's Drag transition.</summary>
    public SwipeViewsPage()
    {
        InitializeComponent();
        DragSwipe.On<MauiAndroid>().SetSwipeTransitionMode(SwipeTransitionMode.Drag);
    }

    void OnOpenLeft(object? sender, EventArgs e) =>
        FourDirectionSwipe.Open(OpenSwipeItem.LeftItems);

    void OnOpenBottom(object? sender, EventArgs e) =>
        FourDirectionSwipe.Open(OpenSwipeItem.BottomItems);

    void OnClose(object? sender, EventArgs e) =>
        FourDirectionSwipe.Close();

    void OnAddLeftItem(object? sender, EventArgs e)
    {
        _addedItems++;
        FourDirectionSwipe.LeftItems.Add(new SwipeItem
        {
            Text = $"New {_addedItems}",
            BackgroundColor = Colors.Indigo,
            Command = new Command(() => StatusLabel.Text = $"Invoked dynamic {_addedItems}"),
        });
        StatusLabel.Text = $"LeftItems count: {FourDirectionSwipe.LeftItems.Count}";
    }

    void OnToggleItem(object? sender, EventArgs e)
    {
        DynamicItem.IsVisible = !DynamicItem.IsVisible;
        DynamicItem.Text = DynamicItem.IsVisible ? "Archive updated" : "Archive hidden";
        DynamicItem.BackgroundColor = DynamicItem.IsVisible ? Colors.DarkBlue : Colors.Gray;
        StatusLabel.Text = DynamicItem.IsVisible
            ? "Updated Archive text/background and made it visible"
            : "Archive item hidden";
    }

    void OnRemoveLeftItem(object? sender, EventArgs e)
    {
        if (FourDirectionSwipe.LeftItems.Count > 0)
            FourDirectionSwipe.LeftItems.RemoveAt(
                FourDirectionSwipe.LeftItems.Count - 1);
        StatusLabel.Text =
            $"LeftItems count: {FourDirectionSwipe.LeftItems.Count}";
    }

    void OnClearLeftItems(object? sender, EventArgs e)
    {
        FourDirectionSwipe.LeftItems.Clear();
        StatusLabel.Text = "LeftItems cleared";
    }

    void OnRestoreLeftItems(object? sender, EventArgs e)
    {
        if (FourDirectionSwipe.LeftItems.Count != 0)
            return;
        FourDirectionSwipe.LeftItems.Add(new SwipeItem
        {
            Text = "Restored",
            BackgroundColor = Colors.DarkBlue,
            Command = new Command(() =>
                StatusLabel.Text = "Invoked restored action"),
        });
        StatusLabel.Text = "LeftItems restored";
    }

    void OnSwipeStarted(object? sender, SwipeStartedEventArgs e) =>
        StatusLabel.Text = $"Started: {e.SwipeDirection}";

    void OnSwipeChanging(object? sender, SwipeChangingEventArgs e) =>
        StatusLabel.Text = $"Changing: {e.SwipeDirection}, {e.Offset:F1}dp";

    void OnSwipeEnded(object? sender, SwipeEndedEventArgs e) =>
        StatusLabel.Text = $"Ended: {e.SwipeDirection}, open={e.IsOpen}";

    void OnItemInvoked(object? sender, EventArgs e) =>
        StatusLabel.Text = $"Invoked: {(sender as SwipeItem)?.Text ?? "item"}";

    void OnCustomItemInvoked(object? sender, EventArgs e) =>
        StatusLabel.Text = "Invoked custom SwipeItemView";

    void OnDisabledItemInvoked(object? sender, EventArgs e) =>
        StatusLabel.Text = "ERROR: disabled item invoked";

    void OnExecuteInvoked(object? sender, EventArgs e) =>
        StatusLabel.Text = "Execute-mode action invoked at threshold";

    void OnContentTapped(object? sender, EventArgs e)
    {
        _contentTaps++;
        ContentTapButton.Text = $"Content taps: {_contentTaps}";
    }
}
