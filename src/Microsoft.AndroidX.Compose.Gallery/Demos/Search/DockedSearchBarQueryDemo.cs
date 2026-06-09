using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.Search;

/// <summary>DockedSearchBar — deprecated query-based variant with built-in input field.</summary>
public static class DockedSearchBarQueryDemo
{
    static readonly string[] Fruits =
    {
        "Apple", "Banana", "Cherry", "Date", "Elderberry",
        "Fig", "Grape", "Kiwi", "Lemon", "Mango",
    };

    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "search-docked-query",
        CategoryId:  "search",
        Title:       "DockedSearchBar (query-based)",
        Description: "Deprecated query/active overload — the bar renders its own input field.",
        Build:       c =>
        {
            var query  = c.Remember(() => new MutableState<string>(""));
            var active = c.Remember(() => new MutableState<bool>(false));

            var matches = Array.FindAll(
                Fruits,
                f => string.IsNullOrEmpty(query.Value)
                     || f.Contains(query.Value, StringComparison.OrdinalIgnoreCase));

#pragma warning disable CS0618 // DockedSearchBar is intentionally exercised here
            var docked = new DockedSearchBar(
                query:          query.Value,
                onQueryChange:  v => query.Value = v,
                onSearch:       _ => active.Value = false,
                active:         active.Value,
                onActiveChange: v => active.Value = v)
            {
                Placeholder  = new Text("Search fruits"),
                LeadingIcon  = new Text("🔍"),
                TrailingIcon = active.Value
                    ? new IconButton(onClick: () =>
                      {
                          query.Value  = "";
                          active.Value = false;
                      })
                      {
                          new Text("✕"),
                      }
                    : null,
            };
#pragma warning restore CS0618

            foreach (var f in matches)
                docked.Add(new Text(f) { Modifier = Modifier.Companion.Padding(16, 12) });
            if (matches.Length == 0)
                docked.Add(new Text("(no matches)") { Modifier = Modifier.Companion.Padding(16, 12) });

            return new Column
            {
                new Text("Tap the field to expand the docked results list."),
                docked,
            };
        });
}
