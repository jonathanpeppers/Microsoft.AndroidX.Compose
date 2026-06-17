namespace Microsoft.AndroidX.Compose.Maui.Sample.Pages;

/// <summary>
/// Vertical-text-alignment demo — exercises <see cref="TextAlignment"/>
/// on every Compose-backed text-input handler
/// (<see cref="Label"/>, <see cref="Entry"/>, <see cref="Editor"/>,
/// <see cref="SearchBar"/>, <see cref="Picker"/>) inside fixed-height
/// frames so the Start/Center/End distinction is visible.
/// </summary>
public partial class VerticalAlignmentPage : ContentPage
{
    /// <summary>Build the page.</summary>
    public VerticalAlignmentPage()
    {
        InitializeComponent();

        // Seed every Picker with the same items so each shows a
        // recognisable selection regardless of vertical alignment.
        foreach (var picker in this.GetVisualTreeDescendants().OfType<Picker>())
        {
            picker.ItemsSource = new[] { "Apple", "Banana", "Cherry" };
            picker.SelectedIndex = 0;
        }
    }
}
