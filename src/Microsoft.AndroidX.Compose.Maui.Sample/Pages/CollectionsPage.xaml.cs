using System.Collections.ObjectModel;

namespace Microsoft.AndroidX.Compose.Maui.Sample.Pages;

/// <summary>
/// Sample data row for the <see cref="CollectionsPage"/> grid /
/// list demos.
/// </summary>
/// <param name="Name">Display name shown on each row.</param>
/// <param name="Origin">Free-form second line under the name.</param>
/// <param name="Calories">Trailing kcal stat.</param>
/// <param name="Accent">Accent colour used for the row's side bar.</param>
public sealed record Fruit(string Name, string Origin, int Calories, Color Accent);

/// <summary>
/// Collections demo — exercises
/// <see cref="Microsoft.AndroidX.Compose.Maui.Handlers.CollectionViewHandler"/>:
/// vertical, horizontal, and grid CollectionViews backed by a shared
/// <see cref="ObservableCollection{T}"/> with Add / Remove / Clear
/// mutations + an empty-view demo.
/// </summary>
public partial class CollectionsPage : ContentPage
{
    static readonly Fruit[] s_seed =
    {
        new("Apple",      "USA",        95,  Colors.IndianRed),
        new("Banana",     "Ecuador",   105,  Colors.Gold),
        new("Cherry",     "Turkey",     50,  Colors.Crimson),
        new("Date",       "Iran",      282,  Colors.SaddleBrown),
        new("Elderberry", "Europe",     73,  Colors.Purple),
        new("Fig",        "Greece",     74,  Colors.Plum),
        new("Grape",      "Italy",      67,  Colors.MediumPurple),
    };

    int _addCursor;

    /// <summary>Backing collection driving all three CollectionViews on this page.</summary>
    public ObservableCollection<Fruit> Fruits { get; } = new(s_seed);

    /// <summary>Build the page.</summary>
    public CollectionsPage()
    {
        InitializeComponent();
        _addCursor = Fruits.Count;
    }

    void OnAddFruit(object? sender, EventArgs e)
    {
        // Cycle through the seed so the list keeps gaining distinct items
        // rather than always re-appending the same one.
        var template = s_seed[_addCursor % s_seed.Length];
        Fruits.Add(template with { Name = $"{template.Name} #{_addCursor + 1}" });
        _addCursor++;
    }

    void OnRemoveFruit(object? sender, EventArgs e)
    {
        if (Fruits.Count > 0)
            Fruits.RemoveAt(Fruits.Count - 1);
    }

    void OnClearFruits(object? sender, EventArgs e) => Fruits.Clear();

    void OnToggleEmpty(object? sender, EventArgs e)
    {
        if (EmptyDemo.ItemsSource is null)
        {
            EmptyDemo.ItemsSource = new[]
            {
                "First", "Second", "Third", "Fourth",
            };
        }
        else
        {
            EmptyDemo.ItemsSource = null;
        }
    }
}
