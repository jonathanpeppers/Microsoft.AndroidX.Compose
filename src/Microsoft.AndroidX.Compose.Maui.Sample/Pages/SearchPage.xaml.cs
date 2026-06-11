namespace Microsoft.AndroidX.Compose.Maui.Sample.Pages;

/// <summary>
/// SearchBar demo — filters a static list of fruit names as the user
/// types and reports the IME-Search-pressed event in a label so the
/// hardware action's wire-up can be confirmed by eye.
/// </summary>
public partial class SearchPage : ContentPage
{
    static readonly string[] Fruits = new[]
    {
        "Apple", "Apricot", "Avocado", "Banana", "Blackberry", "Blueberry",
        "Cherry", "Coconut", "Cranberry", "Date", "Dragonfruit", "Elderberry",
        "Fig", "Grape", "Grapefruit", "Guava", "Kiwi", "Lemon", "Lime",
        "Lychee", "Mango", "Mulberry", "Nectarine", "Orange", "Papaya",
        "Passionfruit", "Peach", "Pear", "Persimmon", "Pineapple", "Plum",
        "Pomegranate", "Quince", "Raspberry", "Strawberry", "Tangerine",
        "Watermelon",
    };

    /// <summary>Build the page.</summary>
    public SearchPage()
    {
        InitializeComponent();
        Refresh(string.Empty);
    }

    void OnSearchTextChanged(object? sender, TextChangedEventArgs e) =>
        Refresh(e.NewTextValue ?? string.Empty);

    void OnSearchButtonPressed(object? sender, EventArgs e)
    {
        LastImeLabel.Text = string.IsNullOrWhiteSpace(Search.Text)
            ? "(IME-Search pressed with empty query.)"
            : $"IME-Search pressed: \u201C{Search.Text}\u201D";
    }

    void Refresh(string query)
    {
        Results.Clear();
        var filtered = string.IsNullOrWhiteSpace(query)
            ? Fruits
            : Fruits.Where(f => f.Contains(query, StringComparison.OrdinalIgnoreCase)).ToArray();

        if (filtered.Length == 0)
        {
            Results.Add(new Label { Text = "(no matches)", FontAttributes = FontAttributes.Italic });
            return;
        }

        foreach (var fruit in filtered)
            Results.Add(new Label { Text = fruit });
    }
}
