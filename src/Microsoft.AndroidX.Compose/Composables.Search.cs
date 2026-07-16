using AndroidX.Compose.Runtime;

namespace AndroidX.Compose;

public static partial class Composables
{
    /// <summary>Renders a collapsed state-based search bar with an explicit composer.</summary>
    [Composable, GenerateImplicitComposable]
    public static void SearchBar(
        IComposer composer,
        SearchBarState state,
        [ComposableContent] Action<IComposer> inputField,
        Modifier? modifier = null)
    {
        ArgumentNullException.ThrowIfNull(composer);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(inputField);

        new global::AndroidX.Compose.SearchBar(state)
        {
            Modifier = modifier,
            InputField = new Tier2InlineContent(inputField),
        }.Render(composer);
    }

    /// <summary>Renders a state-based top search bar with an explicit composer.</summary>
    [Composable, GenerateImplicitComposable]
    public static void TopSearchBar(
        IComposer composer,
        SearchBarState state,
        [ComposableContent] Action<IComposer> inputField,
        Modifier? modifier = null)
    {
        ArgumentNullException.ThrowIfNull(composer);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(inputField);

        new global::AndroidX.Compose.TopSearchBar(state)
        {
            Modifier = modifier,
            InputField = new Tier2InlineContent(inputField),
        }.Render(composer);
    }

    /// <summary>Renders a docked expanded search popup with an explicit composer.</summary>
    [Composable, GenerateImplicitComposable]
    public static void ExpandedDockedSearchBar(
        IComposer composer,
        SearchBarState state,
        [ComposableContent] Action<IComposer> inputField,
        [ComposableContent] Action<IComposer> content,
        Modifier? modifier = null)
    {
        ArgumentNullException.ThrowIfNull(composer);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(inputField);
        ArgumentNullException.ThrowIfNull(content);

        var search = new global::AndroidX.Compose.ExpandedDockedSearchBar(state)
        {
            Modifier = modifier,
            InputField = new Tier2InlineContent(inputField),
        };
        search.Add(new Tier2InlineContent(content));
        search.Render(composer);
    }

    /// <summary>Renders a full-screen expanded search popup with an explicit composer.</summary>
    [Composable, GenerateImplicitComposable]
    public static void ExpandedFullScreenSearchBar(
        IComposer composer,
        SearchBarState state,
        [ComposableContent] Action<IComposer> inputField,
        [ComposableContent] Action<IComposer> content,
        Modifier? modifier = null)
    {
        ArgumentNullException.ThrowIfNull(composer);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(inputField);
        ArgumentNullException.ThrowIfNull(content);

        var search =
            new global::AndroidX.Compose.ExpandedFullScreenSearchBar(state)
            {
                Modifier = modifier,
                InputField = new Tier2InlineContent(inputField),
            };
        search.Add(new Tier2InlineContent(content));
        search.Render(composer);
    }

    /// <summary>Renders a state-based search input field with an explicit composer.</summary>
    [Composable, GenerateImplicitComposable]
    public static void SearchBarInputField(
        IComposer composer,
        SearchBarTextFieldState textState,
        SearchBarState searchState,
        Action<string>? onSearch = null,
        Modifier? modifier = null,
        [ComposableContent] Action<IComposer>? placeholder = null,
        [ComposableContent] Action<IComposer>? leadingIcon = null,
        [ComposableContent] Action<IComposer>? trailingIcon = null)
    {
        ArgumentNullException.ThrowIfNull(composer);
        ArgumentNullException.ThrowIfNull(textState);
        ArgumentNullException.ThrowIfNull(searchState);

        new global::AndroidX.Compose.SearchBarInputField(
            textState,
            searchState)
        {
            OnSearch = onSearch,
            Modifier = modifier,
            Placeholder = Tier2InlineContent.Create(placeholder),
            LeadingIcon = Tier2InlineContent.Create(leadingIcon),
            TrailingIcon = Tier2InlineContent.Create(trailingIcon),
        }.Render(composer);
    }

    /// <summary>Renders the deprecated boolean-state docked search bar with an explicit composer.</summary>
    [Obsolete("Use the state-based SearchBar + ExpandedDockedSearchBar pair instead.")]
    [Composable, GenerateImplicitComposable]
    public static void DockedSearchBar(
        IComposer composer,
        bool expanded,
        Action<bool> onExpandedChange,
        [ComposableContent] Action<IComposer> inputField,
        [ComposableContent] Action<IComposer> content,
        Modifier? modifier = null)
    {
        ArgumentNullException.ThrowIfNull(composer);
        ArgumentNullException.ThrowIfNull(onExpandedChange);
        ArgumentNullException.ThrowIfNull(inputField);
        ArgumentNullException.ThrowIfNull(content);

        var search = new global::AndroidX.Compose.DockedSearchBar(
            expanded,
            onExpandedChange)
        {
            Modifier = modifier,
            InputField = new Tier2InlineContent(inputField),
        };
        search.Add(new Tier2InlineContent(content));
        search.Render(composer);
    }

    /// <summary>Renders the deprecated query-based docked search bar with an explicit composer.</summary>
    [Obsolete("Use the state-based SearchBar + ExpandedDockedSearchBar pair instead.")]
    [Composable, GenerateImplicitComposable]
    public static void DockedSearchBar(
        IComposer composer,
        string query,
        Action<string> onQueryChange,
        Action<string> onSearch,
        bool active,
        Action<bool> onActiveChange,
        [ComposableContent] Action<IComposer> content,
        Modifier? modifier = null,
        [ComposableContent] Action<IComposer>? placeholder = null,
        [ComposableContent] Action<IComposer>? leadingIcon = null,
        [ComposableContent] Action<IComposer>? trailingIcon = null)
    {
        ArgumentNullException.ThrowIfNull(composer);
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(onQueryChange);
        ArgumentNullException.ThrowIfNull(onSearch);
        ArgumentNullException.ThrowIfNull(onActiveChange);
        ArgumentNullException.ThrowIfNull(content);

        var search = new global::AndroidX.Compose.DockedSearchBar(
            query,
            onQueryChange,
            onSearch,
            active,
            onActiveChange)
        {
            Modifier = modifier,
            Placeholder = Tier2InlineContent.Create(placeholder),
            LeadingIcon = Tier2InlineContent.Create(leadingIcon),
            TrailingIcon = Tier2InlineContent.Create(trailingIcon),
        };
        search.Add(new Tier2InlineContent(content));
        search.Render(composer);
    }
}
