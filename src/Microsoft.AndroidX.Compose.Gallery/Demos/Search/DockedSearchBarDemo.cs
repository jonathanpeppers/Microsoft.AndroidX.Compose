using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.Search;

/// <summary>DockedSearchBar — deprecated boolean-state variant; popup docks under the field.</summary>
public static class DockedSearchBarDemo
{
    static readonly string[] Fruits =
    {
        "Apple", "Banana", "Cherry", "Date", "Elderberry",
        "Fig", "Grape", "Kiwi", "Lemon", "Mango",
    };

    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "search-docked",
        CategoryId:  "search",
        Title:       "DockedSearchBar",
        Description: "Deprecated boolean-state variant of SearchBar; popup docks under the input.",
        Build:       c =>
        {
            var open  = c.Remember(() => new MutableState<bool>(false));
            var query = c.Remember(() => new MutableState<string>(""));

            var matches = Array.FindAll(
                Fruits,
                f => string.IsNullOrEmpty(query.Value)
                     || f.Contains(query.Value, StringComparison.OrdinalIgnoreCase));

#pragma warning disable CS0618 // DockedSearchBar is intentionally exercised here
            var docked = new DockedSearchBar(
                expanded:         open.Value,
                onExpandedChange: v => open.Value = v)
            {
                InputField = new Row
                {
                    new TextField(query),
                    new IconButton(onClick: () => open.Value = !open.Value)
                    {
                        new Text(open.Value ? "▲" : "▼"),
                    },
                },
            };
#pragma warning restore CS0618
            foreach (var f in matches)
                docked.Add(new Text(f) { Modifier = Modifier.Padding(16, 12) });
            if (matches.Length == 0)
                docked.Add(new Text("(no matches)") { Modifier = Modifier.Padding(16, 12) });

            return new Column
            {
                new Text("Type in the field, tap the ▼/▲ button to toggle the docked results popup."),
                docked,
            };
        });
}
