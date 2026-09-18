using AndroidX.Compose.Foundation.Text.Input;

namespace AndroidX.Compose;

/// <summary>
/// Caller-supplied state holder for the text inside a
/// <see cref="SearchBarInputField"/>. Mirrors <see cref="SearchBarState"/>:
/// the underlying JVM <c>androidx.compose.foundation.text.input.TextFieldState</c>
/// is owned by the active composition and shared by every rendered input
/// field using this wrapper.
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
///
/// Removing every input field retires the native peer while retaining the
/// exact text and packed selection. Re-entry creates a new composition-owned
/// peer initialized from those retained values.
/// </remarks>
public sealed class SearchBarTextFieldState
{
    string _pendingText;
    long _pendingSelection;

    internal TextFieldState? Jvm;

    /// <summary>
    /// Creates a state holder with an optional initial text value
    /// (defaults to the empty string).
    /// </summary>
    public SearchBarTextFieldState(string initialText = "")
    {
        ArgumentNullException.ThrowIfNull(initialText);
        _pendingText = initialText;
        _pendingSelection = AndroidX.Compose.UI.Text.TextRangeKt.TextRange(
            initialText.Length, initialText.Length);
    }

    /// <summary>
    /// Current text in the search input. Reads through to the live
    /// JVM <c>TextFieldState.text</c> (a Compose snapshot value), so
    /// reading this inside composition subscribes to recomposition.
    /// While unbound, returns the latest retained value from construction,
    /// a text mutation method, or native-owner retirement.
    /// </summary>
    public string Text => Jvm?.Text ?? _pendingText;

    /// <summary>Replaces the text and places the cursor at the end.</summary>
    public void SetText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (Jvm is null)
        {
            _pendingText = text;
            _pendingSelection = AndroidX.Compose.UI.Text.TextRangeKt.TextRange(
                text.Length, text.Length);
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
            _pendingSelection = AndroidX.Compose.UI.Text.TextRangeKt.TextRange(
                0, text.Length);
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
            _pendingSelection = 0L;
            return;
        }
        TextFieldStateKt.ClearText(Jvm);
    }

    internal string RememberText => _pendingText;
    internal long RememberSelection => _pendingSelection;

    internal void BindJvm(TextFieldState jvm) => Jvm = jvm;

    internal void UnbindJvm()
    {
        if (Jvm is not { } jvm)
            return;
        try
        {
            string retainedText = jvm.Text ?? "";
            long retainedSelection = jvm.Selection;
            _pendingText = retainedText;
            _pendingSelection = retainedSelection;
        }
        finally
        {
            Jvm = null;
        }
    }
}
