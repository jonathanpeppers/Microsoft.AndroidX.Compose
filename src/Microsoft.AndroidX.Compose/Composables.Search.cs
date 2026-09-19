using AndroidX.Compose.Runtime;

namespace AndroidX.Compose;

public static partial class Composables
{
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
