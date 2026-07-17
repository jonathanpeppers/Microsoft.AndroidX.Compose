using AndroidX.Compose.Runtime;

namespace AndroidX.Compose;

public static partial class Composables
{
    /// <summary>Renders a collapsed state-based search bar with an explicit composer.</summary>
    [Composable, GenerateImplicitComposable]
    internal static void SearchBar(
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
            InputField = new ComposableContentNode(inputField),
        }.Render(composer);
    }

    /// <summary>Renders a state-based top search bar with an explicit composer.</summary>
    [Composable, GenerateImplicitComposable]
    internal static void TopSearchBar(
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
            InputField = new ComposableContentNode(inputField),
        }.Render(composer);
    }

    /// <summary>Renders a docked expanded search popup with an explicit composer.</summary>
    [Composable, GenerateImplicitComposable]
    internal static void ExpandedDockedSearchBar(
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
            InputField = new ComposableContentNode(inputField),
        };
        search.Add(new ComposableContentNode(content));
        search.Render(composer);
    }

    /// <summary>Renders a full-screen expanded search popup with an explicit composer.</summary>
    [Composable, GenerateImplicitComposable]
    internal static void ExpandedFullScreenSearchBar(
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
                InputField = new ComposableContentNode(inputField),
            };
        search.Add(new ComposableContentNode(content));
        search.Render(composer);
    }

    /// <summary>Renders a state-based search input field with an explicit composer.</summary>
    [Composable, GenerateImplicitComposable]
    internal static void SearchBarInputField(
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
            Placeholder = ComposableContentNode.Create(placeholder),
            LeadingIcon = ComposableContentNode.Create(leadingIcon),
            TrailingIcon = ComposableContentNode.Create(trailingIcon),
        }.Render(composer);
    }

}
