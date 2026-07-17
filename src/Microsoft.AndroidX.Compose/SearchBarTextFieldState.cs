using AndroidX.Compose.Foundation.Text.Input;
using AndroidX.Compose.Runtime;

namespace AndroidX.Compose;

/// <summary>
/// Caller-supplied state holder for the text inside a
/// <see cref="SearchBarInputField"/>. Mirrors <see cref="SearchBarState"/>:
/// the underlying JVM
/// <c>androidx.compose.foundation.text.input.TextFieldState</c> is created
/// lazily the first time a <see cref="SearchBarInputField"/> bound to this
/// state is rendered, via Compose's <c>rememberTextFieldState</c>.
/// </summary>
/// <remarks>
/// Both halves of the SearchBar pair — the collapsed
/// <see cref="SearchBar"/> and the expanded popup
/// (<see cref="ExpandedFullScreenSearchBar"/> /
/// <see cref="ExpandedDockedSearchBar"/>) — pass the SAME
/// <see cref="SearchBarTextFieldState"/> to their input fields so the
/// typed text is shared between them.
///
/// <para>Read <see cref="Text"/> inside composition to filter a result
/// list; it goes through to the live JVM peer's <c>text</c> property
/// (which is itself a Compose snapshot state), so typing in the field
/// triggers recomposition.</para>
///
/// <code>
/// var search = Remember(() =&gt; new SearchBarState());
/// var input  = Remember(() =&gt; new SearchBarTextFieldState());
///
/// new Box
/// {
///     new SearchBar(state: search)
///     {
///         InputField = new SearchBarInputField(input, search)
///         {
///             Placeholder = new Text("Search fruits"),
///             LeadingIcon = new Text("🔍"),
///         },
///     },
///     new ExpandedFullScreenSearchBar(state: search)
///     {
///         InputField = new SearchBarInputField(input, search),
///         new Text($"Matches: {input.Text}"),
///     },
/// }
/// </code>
/// </remarks>
public sealed class SearchBarTextFieldState
{
    string _pendingText;
    bool _selectAllPending;

    // Bound peer for the JVM androidx.compose.foundation.text.input.TextFieldState.
    // Set on first SearchBarInputField render and reused by every subsequent
    // render so collapsed + expanded halves share one TextFieldState.
    internal TextFieldState? Jvm;

    /// <summary>
    /// Creates a state holder with an optional initial text value
    /// (defaults to the empty string).
    /// </summary>
    public SearchBarTextFieldState(string initialText = "")
    {
        ArgumentNullException.ThrowIfNull(initialText);
        _pendingText = initialText;
    }

    /// <summary>
    /// Current text in the search input. Reads through to the live
    /// JVM <c>TextFieldState.text</c> (a Compose snapshot value), so
    /// reading this inside composition subscribes to recomposition.
    /// Returns the initial value before the first render binds the peer.
    /// </summary>
    public string Text => Jvm?.Text ?? _pendingText;

    /// <summary>Replaces the text and places the cursor at the end.</summary>
    public void SetText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (Jvm is null)
        {
            _pendingText = text;
            _selectAllPending = false;
            return;
        }
        TextFieldStateKt.SetTextAndPlaceCursorAtEnd(Jvm, text);
    }

    /// <summary>Replaces the text and selects the complete value.</summary>
    public void SetTextAndSelectAll(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (Jvm is null)
        {
            _pendingText = text;
            _selectAllPending = true;
            return;
        }
        TextFieldStateKt.SetTextAndSelectAll(Jvm, text);
    }

    /// <summary>Clears the text and leaves the cursor at the end.</summary>
    public void ClearText()
    {
        if (Jvm is null)
        {
            _pendingText = "";
            _selectAllPending = false;
            return;
        }
        TextFieldStateKt.ClearText(Jvm);
    }

    // Lazy-resolve the bound JVM peer. Multiple SearchBarInputField
    // siblings sharing this state hit the JNI bridge once on the FIRST
    // render and reuse the peer for every subsequent half.
    internal TextFieldState Resolve(IComposer composer)
    {
        if (Jvm is not null)
            return Jvm;

        long selection = _selectAllPending
            ? AndroidX.Compose.UI.Text.TextRangeKt.TextRange(0, _pendingText.Length)
            : 0L;
        int defaults = _selectAllPending ? 0 : 2;
        Jvm = TextFieldStateKt.RememberTextFieldState(
            _pendingText, selection, composer, 0, defaults);
        return Jvm;
    }
}
