using ComposeNet.Gallery.Registry;

namespace ComposeNet.Gallery.Demos.Search;

/// <summary>SearchBar + ExpandedFullScreenSearchBar pair sharing one state.</summary>
public static class SearchBarPairDemo
{
    static readonly string[] Fruits =
    {
        "Apple", "Banana", "Cherry", "Date", "Elderberry",
        "Fig", "Grape", "Kiwi", "Lemon", "Mango",
    };

    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "search-searchbar-pair",
        CategoryId:  "search",
        Title:       "SearchBar + ExpandedFullScreenSearchBar",
        Description: "Both halves share one SearchBarState; Compose toggles the popup based on focus.",
        Build:       () =>
        {
            var state = Compose.Remember(() => new SearchBarState());
            var input = Compose.Remember(() => new SearchBarTextFieldState());
            var query = Compose.Remember(() => new MutableState<string>(""));

            var matches = System.Array.FindAll(
                Fruits,
                f => string.IsNullOrEmpty(query.Value)
                     || f.Contains(query.Value, System.StringComparison.OrdinalIgnoreCase));

            var expanded = new ExpandedFullScreenSearchBar(state: state)
            {
                InputField = new SearchBarInputField(input, state)
                {
                    Placeholder = new Text("Search fruits"),
                    LeadingIcon = new Text("🔍"),
                    OnSearch    = q => query.Value = q,
                },
            };
            foreach (var f in matches)
                expanded.Add(new Text(f) { Modifier = Modifier.Companion.Padding(16, 12) });
            if (matches.Length == 0)
                expanded.Add(new Text("(no matches)") { Modifier = Modifier.Companion.Padding(16, 12) });

            return new Column
            {
                new Text("Tap the bar, type a query, then press the keyboard's 🔍 Search key to filter."),
                new Text($"Filter: \"{query}\" — {matches.Length} match{(matches.Length == 1 ? "" : "es")}"),
                new Box
                {
                    new SearchBar(state: state)
                    {
                        InputField = new SearchBarInputField(input, state)
                        {
                            Placeholder = new Text("Search fruits"),
                            LeadingIcon = new Text("🔍"),
                            OnSearch    = q => query.Value = q,
                        },
                    },
                    expanded,
                },
            };
        });
}
