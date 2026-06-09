using ComposeNet.Gallery.Registry;

namespace ComposeNet.Gallery.Screens;

/// <summary>
/// Full-screen search route. Opens a Material 3
/// <see cref="ExpandedFullScreenSearchBar"/> over the gallery's surface
/// with results from <see cref="Catalog.Search(string?)"/>. Submitting a
/// query updates a remembered <see cref="MutableState{T}"/> (because
/// <see cref="SearchBarTextFieldState.Text"/> doesn't subscribe to
/// snapshot tracking from C# build code); tapping a result navigates
/// to <c>demo/{id}</c> and pops the search route off the back stack so
/// the user lands directly on the demo.
/// </summary>
public sealed class SearchScreen : ComposableNode
{
    readonly NavController _nav;

    /// <summary>Construct a search screen bound to <paramref name="nav"/>.</summary>
    public SearchScreen(NavController nav) => _nav = nav;

    public override void Render(AndroidX.Compose.Runtime.IComposer composer)
    {
        var searchState = Compose.Remember(() => new SearchBarState());
        var inputState  = Compose.Remember(() => new SearchBarTextFieldState());
        var query       = Compose.Remember(() => new MutableState<string>(""));

        var matches = Catalog.Search(query.Value).ToList();

        // The full-screen variant pops a Dialog on top of the gallery
        // chrome — the system back gesture closes the popup, which
        // matches the user's expectation when the search action
        // navigated to a dedicated route.
        var expanded = new ExpandedFullScreenSearchBar(state: searchState)
        {
            InputField = new SearchBarInputField(inputState, searchState)
            {
                Placeholder = new Text("Search demos"),
                LeadingIcon = new Text("🔍"),
                OnSearch    = q => query.Value = q,
            },
        };

        if (matches.Count == 0)
        {
            expanded.Add(new ListItem
            {
                Headline   = new Text("No matches"),
                Supporting = new Text(string.IsNullOrEmpty(query.Value)
                    ? "Type to search demos by name, description, or category."
                    : $"Nothing matches '{query.Value}'."),
            });
        }
        else
        {
            foreach (var demo in matches)
            {
                var d = demo;
                var category = Catalog.FindCategory(d.CategoryId);
                expanded.Add(new ListItem
                {
                    Modifier = Modifier.Companion
                        .FillMaxWidth()
                        .Clickable(() =>
                        {
                            _nav.PopBackStack();
                            _nav.Navigate($"demo/{d.Id}");
                        }),
                    Headline   = new Text(d.Title),
                    Supporting = new Text($"{category?.Glyph ?? "•"}  {category?.Title ?? "?"}  ·  {d.Description}"),
                });
            }
        }

        // ExpandedFullScreenSearchBar requires a sibling SearchBar in
        // the same composition that shares the state — without it the
        // popup has no anchor and the input field never gets focus
        // when the route opens. Render the collapsed SearchBar as the
        // first child of this Column so it still takes layout but
        // stays off the user's mind.
        new Column
        {
            Modifier.Companion.FillMaxSize(),
            new SearchBar(state: searchState)
            {
                InputField = new SearchBarInputField(inputState, searchState)
                {
                    Placeholder = new Text("Search demos"),
                    LeadingIcon = new Text("🔍"),
                    OnSearch    = q => query.Value = q,
                },
            },
            expanded,
        }.Render(composer);
    }
}
