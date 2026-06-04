using Android.Runtime;
using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>SearchBar</c> (state-based). The always-visible
/// collapsed input field that pairs with an
/// <see cref="ExpandedFullScreenSearchBar"/> or
/// <see cref="ExpandedDockedSearchBar"/> popup sharing the same
/// <see cref="SearchBarState"/>.
/// </summary>
/// <remarks>
/// <see cref="InputField"/> is required (the Kotlin parameter has no
/// default) — the user supplies any composable that renders the search
/// text field, typically a <see cref="ComposeNet.TextField"/>.
///
/// <code>
/// var search = Remember(() =&gt; new SearchBarState());
/// var query  = Remember(() =&gt; new MutableState&lt;string&gt;(""));
///
/// new SearchBar(state: search)
/// {
///     InputField = new TextField(query.Value, q =&gt; query.Value = q)
///         { Modifier = Modifier.Companion.FillMaxWidth() },
/// }
/// </code>
/// </remarks>
public sealed class SearchBar : ComposableNode
{
    readonly SearchBarState _state;

    public SearchBar(SearchBarState state) => _state = state;

    /// <summary>Required: composable that renders the search input field.</summary>
    public ComposableNode? InputField { get; set; }

    internal override void Render(IComposer composer)
    {
        if (InputField is null)
            throw new System.InvalidOperationException(
                "SearchBar.InputField is required (the Kotlin parameter has no default).");

        var stateHandle = ResolveStateHandle(_state, composer);
        var inputField  = new ComposableLambda2(c => InputField.Render(c));
        ComposeBridges.SearchBar(stateHandle, inputField, BuildModifier(), composer);
    }

    // Lazy-resolve the bound JVM peer for the shared SearchBarState. The
    // first SearchBar/Expanded*SearchBar bound to a given state calls
    // rememberSearchBarState and caches the resulting Java.Lang.Object;
    // subsequent renders reuse the handle from that peer. Shared with
    // TopSearchBar, ExpandedDockedSearchBar, ExpandedFullScreenSearchBar.
    internal static IntPtr ResolveStateHandle(SearchBarState state, IComposer composer)
    {
        var handle = ComposeBridges.RememberSearchBarState(composer);
        if (state.Jvm is null)
            state.Jvm = Java.Lang.Object.GetObject<Java.Lang.Object>(handle, JniHandleOwnership.DoNotTransfer)!;
        return handle;
    }
}
