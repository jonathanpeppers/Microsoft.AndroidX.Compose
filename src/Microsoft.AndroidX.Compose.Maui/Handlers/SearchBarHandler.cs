using AndroidX.Compose;
using AndroidX.Compose.Runtime;
using AndroidX.Compose.UI.Text;
using AndroidX.Compose.UI.Text.Input;
using Microsoft.AndroidX.Compose.Maui.Platform;
using Microsoft.Maui.Handlers;
using ComposeColor              = AndroidX.Compose.Color;
using ComposeFontWeight         = AndroidX.Compose.FontWeight;
using ComposeImeAction          = AndroidX.Compose.ImeAction;
using ComposeKeyboardType       = AndroidX.Compose.KeyboardType;
using ComposeOutlinedTextField  = AndroidX.Compose.OutlinedTextField;
using ComposeText               = AndroidX.Compose.Text;
using ComposeTextAlign          = AndroidX.Compose.TextAlign;
using ComposeTextStyle          = AndroidX.Compose.TextStyle;

namespace Microsoft.AndroidX.Compose.Maui.Handlers;

/// <summary>
/// MAUI <see cref="Microsoft.Maui.Controls.SearchBar"/> handler.
/// Renders through Jetpack Compose's Material 3
/// <c>OutlinedTextField</c> with a leading magnifier icon and an
/// IME action set to <c>ImeAction.Search</c>; tapping the IME's
/// search key invokes the <see cref="ISearchBar.SearchButtonPressed"/>
/// trampoline (which routes <see cref="Microsoft.Maui.Controls.SearchBar.SearchCommand"/>
/// + <see cref="Microsoft.Maui.Controls.SearchBar.SearchButtonPressed"/>
/// just like the stock handler).
/// </summary>
/// <remarks>
/// <para>Compose's first-party <c>SearchBar</c> facade is too
/// opinionated for the MAUI shape — it's state-based (requires a
/// <c>SearchBarState</c> + <c>SearchBarTextFieldState</c> allocated
/// inside the composition) and self-manages an expand-popup
/// behaviour driven by tap gestures. MAUI's <c>SearchBar</c> is just a
/// styled text input with a search-icon leading slot, so we render
/// the simpler <c>OutlinedTextField</c> shape and wire the search
/// action through <c>keyboardActions.onSearch</c>.</para>
///
/// <para>Like <see cref="EntryHandler"/>, value/cursor/selection state
/// is bound through Compose's
/// <c>OutlinedTextField(MutableState&lt;TextFieldValue&gt;)</c>
/// overload via a custom <see cref="MutableState{T}"/> subclass that
/// enforces <see cref="ITextInput.MaxLength"/> and mirrors text +
/// caret back to MAUI's <see cref="ISearchBar"/>.</para>
/// </remarks>
public partial class SearchBarHandler : ComposeElementHandler<ISearchBar>
{
    /// <summary>
    /// Property mapper that forwards MAUI <see cref="ISearchBar"/>
    /// property changes to the Compose-backed
    /// <see cref="AndroidX.Compose.UI.Platform.ComposeView"/>.
    /// </summary>
    public static IPropertyMapper<ISearchBar, SearchBarHandler> Mapper =
        new PropertyMapper<ISearchBar, SearchBarHandler>(ViewHandler.ViewMapper)
        {
            [nameof(IText.Text)]                            = MapText,
            [nameof(ITextStyle.TextColor)]                  = MapTextColor,
            [nameof(ITextStyle.CharacterSpacing)]           = MapCharacterSpacing,
            [nameof(ITextStyle.Font)]                       = MapFont,
            [nameof(IPlaceholder.Placeholder)]              = MapPlaceholder,
            [nameof(IPlaceholder.PlaceholderColor)]         = MapPlaceholderColor,
            [nameof(ISearchBar.SearchIconColor)]            = MapSearchIconColor,
            [nameof(ISearchBar.CancelButtonColor)]          = MapCancelButtonColor,
            [nameof(ISearchBar.ReturnType)]                 = MapReturnType,
            [nameof(ITextInput.Keyboard)]                   = MapKeyboard,
            [nameof(ITextInput.IsReadOnly)]                 = MapIsReadOnly,
            [nameof(ITextInput.IsSpellCheckEnabled)]        = MapAutoCorrect,
            [nameof(ITextInput.IsTextPredictionEnabled)]    = MapAutoCorrect,
            [nameof(ITextInput.MaxLength)]                  = MapMaxLength,
            [nameof(ITextInput.CursorPosition)]             = MapCursorPosition,
            [nameof(ITextInput.SelectionLength)]            = MapSelectionLength,
            [nameof(ITextAlignment.HorizontalTextAlignment)] = MapHorizontalTextAlignment,
            [nameof(ITextAlignment.VerticalTextAlignment)]   = MapVerticalTextAlignment,
            [nameof(IView.HorizontalLayoutAlignment)]       = MapHorizontalLayoutAlignment,
        };

    /// <summary>Command mapper (inherits view-level commands; no extras).</summary>
    public static CommandMapper<ISearchBar, SearchBarHandler> CommandMapper =
        new(ViewCommandMapper);

    readonly TextFieldValueState _tfv;
    readonly MutableState<long?>  _color             = new((long?)null);
    readonly MutableState<long?>  _placeholderColor  = new((long?)null);
    readonly MutableState<long?>  _searchIconColor   = new((long?)null);
    readonly MutableState<long?>  _cancelButtonColor = new((long?)null);
    readonly MutableState<int?>   _fontSize          = new((int?)null);
    readonly MutableState<bool>   _bold              = new(false);
    readonly MutableState<string> _placeholder       = new(string.Empty);
    readonly MutableState<int>    _keyboardType      = new(ComposeKeyboardType.Text);
    readonly MutableState<bool>   _readOnly          = new(false);
    readonly MutableState<bool>   _autoCorrect       = new(true);
    readonly MutableState<int>    _imeAction         = new(ComposeImeAction.Search);
    readonly MutableState<int>    _maxLength         = new(-1);
    readonly MutableState<float?> _letterSpacing     = new((float?)null);
    readonly MutableState<int>    _hTextAlign        = new((int)TextAlignment.Start);
    readonly MutableState<int>    _vTextAlign        = new((int)TextAlignment.Center);
    readonly MutableState<bool>   _fillWidth         = new(false);

    /// <summary>Construct a handler with the default mappers.</summary>
    public SearchBarHandler() : base(Mapper, CommandMapper)
    {
        _tfv = new TextFieldValueState(this);
    }

    /// <summary>Construct a handler with custom mappers.</summary>
    public SearchBarHandler(IPropertyMapper? mapper, CommandMapper? commandMapper = null)
        : base(mapper ?? Mapper, commandMapper ?? CommandMapper)
    {
        _tfv = new TextFieldValueState(this);
    }

    /// <inheritdoc/>
    public override ComposableNode BuildNode(IComposer composer)
    {
        var virtualView = VirtualView
            ?? throw new InvalidOperationException("VirtualView not set on SearchBarHandler.");

        var packed             = _color.Value;
        var placeholderInk     = _placeholderColor.Value;
        var searchIconInk      = _searchIconColor.Value;
        var cancelButtonInk    = _cancelButtonColor.Value;
        var size               = _fontSize.Value;
        var bold               = _bold.Value;
        var placeholder        = _placeholder.Value;
        var keyboardType       = _keyboardType.Value;
        var readOnly           = _readOnly.Value;
        var autoCorrect        = _autoCorrect.Value;
        var imeAction          = _imeAction.Value;
        var letterSpacing      = _letterSpacing.Value;
        var hTextAlign         = (TextAlignment)_hTextAlign.Value;
        var vTextAlign         = (TextAlignment)_vTextAlign.Value;
        var fill               = _fillWidth.Value;

        var field = new ComposeOutlinedTextField(
            _tfv,
            readOnly: readOnly,
            singleLine: true)
        {
            // Magnifier glass — pure Unicode keeps us off the
            // material-icons NuGet for the MAUI base layer. Coloured
            // via SearchIconColor when set.
            LeadingIcon  = new ComposeText("\U0001F50D")
            {
                Color = searchIconInk.HasValue ? ComposeColor.FromPacked(searchIconInk.Value) : null,
            },
        };
        if (!string.IsNullOrEmpty(placeholder))
            field.Placeholder = new ComposeText(placeholder)
            {
                Color = placeholderInk.HasValue ? ComposeColor.FromPacked(placeholderInk.Value) : null,
            };
        if (packed.HasValue || size.HasValue || bold || letterSpacing.HasValue
            || hTextAlign != TextAlignment.Start)
            field.TextStyle = new ComposeTextStyle
            {
                Color         = packed.HasValue ? ComposeColor.FromPacked(packed.Value) : null,
                FontSize      = size.HasValue   ? new Sp(size.Value) : null,
                FontWeight    = bold ? ComposeFontWeight.Bold : null,
                LetterSpacing = letterSpacing.HasValue ? new Sp(1) * letterSpacing.Value : null,
                TextAlign     = hTextAlign switch
                {
                    TextAlignment.Center => ComposeTextAlign.Center,
                    TextAlignment.End    => ComposeTextAlign.End,
                    _                    => null,
                },
            };

        // Cancel-X trailing IconButton, visible only when the field has
        // content — tapping clears the buffer. CancelButtonColor tints
        // the X glyph when set.
        if (!string.IsNullOrEmpty(_tfv.Value?.Text))
        {
            field.TrailingIcon = new IconButton(onClick: () =>
            {
                if (VirtualView is { } searchBar)
                    searchBar.Text = string.Empty;
            })
            {
                new ComposeText("\u2715")
                {
                    Color = cancelButtonInk.HasValue ? ComposeColor.FromPacked(cancelButtonInk.Value) : null,
                },
            };
        }

        // KeyboardOptions: route through KeyboardOptionsCompanion.Default.Copy
        // so we override imeAction (typically Search) without disturbing
        // other slots — same pattern as EntryHandler.
        var d = KeyboardOptionsCompanion.Default;
        field.KeyboardOptions = d.Copy(
            d.Capitalization, (Java.Lang.Boolean)autoCorrect,
            keyboardType, imeAction,
            d.PlatformImeOptions, d.ShowKeyboardOnFocus, d.HintLocales);
        // Wire the search-key callback. SearchButtonPressed() is
        // MAUI's standard trampoline — it fires SearchCommand and the
        // SearchButtonPressed event the way the stock handler does.
        field.KeyboardActions = KeyboardActionsHelper.Create(
            onSearch: OnSearchInvoked,
            onDone:   OnSearchInvoked,
            onGo:     OnSearchInvoked,
            onSend:   OnSearchInvoked);

        var modifier = (fill ? Modifier.FillMaxWidth() : Modifier.Companion)
            .ApplyGestures(virtualView, MauiContext)
            .ApplySemantics(virtualView)
            .ApplyVerticalTextAlignment(vTextAlign);
        field.PrependModifier(modifier);
        return field;
    }

    void OnSearchInvoked()
    {
        // SearchButtonPressed is the public ISearchBar trampoline:
        // it ultimately invokes SearchCommand and raises the
        // SearchButtonPressed event on the controls type.
        VirtualView?.SearchButtonPressed();
    }

    /// <summary>Map <see cref="IText.Text"/> to the Compose value slot.</summary>
    public static void MapText(SearchBarHandler handler, ISearchBar searchBar)
    {
        var newText = searchBar.Text ?? string.Empty;
        var current = handler._tfv.Value;
        if (current?.Text == newText) return;
        var cursor = Math.Min(searchBar.CursorPosition, newText.Length);
        if (cursor < 0) cursor = newText.Length;
        handler._tfv.SetWithoutMirror(ComposeExtensions.NewTextFieldValue(
            newText, TextRangeKt.TextRange(cursor), composition: null));
    }

    /// <summary>Map <see cref="ITextStyle.TextColor"/> to the Compose <c>TextStyle.Color</c> slot.</summary>
    public static void MapTextColor(SearchBarHandler handler, ISearchBar searchBar) =>
        handler._color.Value = ColorMapping.ToPackedLong(searchBar.TextColor);

    /// <summary>Map <see cref="ITextStyle.CharacterSpacing"/> to Compose <c>letterSpacing</c>.</summary>
    public static void MapCharacterSpacing(SearchBarHandler handler, ISearchBar searchBar) =>
        handler._letterSpacing.Value = searchBar.CharacterSpacing != 0
            ? (float)searchBar.CharacterSpacing
            : null;

    /// <summary>Map <see cref="ITextStyle.Font"/> (size + bold) to Compose <c>TextStyle</c> slots.</summary>
    public static void MapFont(SearchBarHandler handler, ISearchBar searchBar)
    {
        var font = searchBar.Font;
        handler._fontSize.Value = font.Size > 0 ? (int)font.Size : null;
        handler._bold.Value     = (font.Weight & Microsoft.Maui.FontWeight.Bold)
            == Microsoft.Maui.FontWeight.Bold;
    }

    /// <summary>Map <see cref="IPlaceholder.Placeholder"/> to the Compose placeholder slot.</summary>
    public static void MapPlaceholder(SearchBarHandler handler, ISearchBar searchBar) =>
        handler._placeholder.Value = searchBar.Placeholder ?? string.Empty;

    /// <summary>Map <see cref="IPlaceholder.PlaceholderColor"/> to the placeholder Text's color.</summary>
    public static void MapPlaceholderColor(SearchBarHandler handler, ISearchBar searchBar) =>
        handler._placeholderColor.Value = ColorMapping.ToPackedLong(searchBar.PlaceholderColor);

    /// <summary>Map <see cref="ISearchBar.SearchIconColor"/> to the leading magnifier's color.</summary>
    public static void MapSearchIconColor(SearchBarHandler handler, ISearchBar searchBar) =>
        handler._searchIconColor.Value = ColorMapping.ToPackedLong(searchBar.SearchIconColor);

    /// <summary>Map <see cref="ISearchBar.CancelButtonColor"/> to the trailing X's color.</summary>
    public static void MapCancelButtonColor(SearchBarHandler handler, ISearchBar searchBar) =>
        handler._cancelButtonColor.Value = ColorMapping.ToPackedLong(searchBar.CancelButtonColor);

    /// <summary>Map <see cref="ISearchBar.ReturnType"/> to <c>KeyboardOptions.imeAction</c>.</summary>
    public static void MapReturnType(SearchBarHandler handler, ISearchBar searchBar) =>
        handler._imeAction.Value = searchBar.ReturnType switch
        {
            ReturnType.Done   => ComposeImeAction.Done,
            ReturnType.Go     => ComposeImeAction.Go,
            ReturnType.Next   => ComposeImeAction.Next,
            ReturnType.Send   => ComposeImeAction.Send,
            // Default for SearchBar is the magnifier (Search) — even
            // for ReturnType.Default — so the IME button matches the
            // leading icon.
            _                 => ComposeImeAction.Search,
        };

    /// <summary>Map <see cref="ITextInput.Keyboard"/> to a Compose <c>KeyboardType</c> int.</summary>
    public static void MapKeyboard(SearchBarHandler handler, ISearchBar searchBar) =>
        handler._keyboardType.Value = KeyboardMapping.Resolve(searchBar.Keyboard, nameof(SearchBarHandler));

    /// <summary>Map <see cref="ITextInput.IsReadOnly"/> to the Compose <c>readOnly</c> slot.</summary>
    public static void MapIsReadOnly(SearchBarHandler handler, ISearchBar searchBar) =>
        handler._readOnly.Value = searchBar.IsReadOnly;

    /// <summary>
    /// Combined map for <see cref="ITextInput.IsSpellCheckEnabled"/> +
    /// <see cref="ITextInput.IsTextPredictionEnabled"/>. Compose has a
    /// single <c>autoCorrectEnabled</c> slot; we AND the MAUI flags.
    /// </summary>
    public static void MapAutoCorrect(SearchBarHandler handler, ISearchBar searchBar) =>
        handler._autoCorrect.Value = searchBar.IsSpellCheckEnabled && searchBar.IsTextPredictionEnabled;

    /// <summary>Map <see cref="ITextInput.MaxLength"/> to the truncation cap (negative = unlimited).</summary>
    public static void MapMaxLength(SearchBarHandler handler, ISearchBar searchBar)
    {
        handler._maxLength.Value = searchBar.MaxLength;
        if (searchBar.MaxLength >= 0
            && handler._tfv.Value is { } current
            && (current.Text?.Length ?? 0) > searchBar.MaxLength)
        {
            handler._tfv.Value = current;
        }
    }

    /// <summary>Map <see cref="ITextInput.CursorPosition"/> to the TFV selection start.</summary>
    public static void MapCursorPosition(SearchBarHandler handler, ISearchBar searchBar) =>
        ApplySelection(handler, searchBar);

    /// <summary>Map <see cref="ITextInput.SelectionLength"/> to the TFV selection end.</summary>
    public static void MapSelectionLength(SearchBarHandler handler, ISearchBar searchBar) =>
        ApplySelection(handler, searchBar);

    static void ApplySelection(SearchBarHandler handler, ISearchBar searchBar)
    {
        var current = handler._tfv.Value;
        if (current is null) return;
        var text = current.Text ?? string.Empty;
        var start = Math.Clamp(searchBar.CursorPosition, 0, text.Length);
        var length = Math.Max(0, Math.Min(searchBar.SelectionLength, text.Length - start));
        var end = start + length;
        var existing = current.Selection;
        if ((int)existing == start && (int)(existing >> 32) == end)
            return;
        handler._tfv.SetWithoutMirror(current.Copy(text, TextRangeKt.TextRange(start, end), composition: null));
    }

    /// <summary>Map <see cref="ITextAlignment.HorizontalTextAlignment"/> to Compose <c>textAlign</c>.</summary>
    public static void MapHorizontalTextAlignment(SearchBarHandler handler, ISearchBar searchBar) =>
        handler._hTextAlign.Value = (int)searchBar.HorizontalTextAlignment;

    /// <summary>
    /// Map <see cref="ITextAlignment.VerticalTextAlignment"/> to a
    /// <c>Modifier.wrapContentHeight(Alignment.Vertical)</c> on the
    /// outer modifier — useful when the search bar's allocated height
    /// exceeds its natural row height.
    /// </summary>
    public static void MapVerticalTextAlignment(SearchBarHandler handler, ISearchBar searchBar) =>
        handler._vTextAlign.Value = (int)searchBar.VerticalTextAlignment;

    /// <summary>
    /// Map <see cref="IView.HorizontalLayoutAlignment"/> to
    /// <c>Modifier.fillMaxWidth()</c> when the search bar asks to
    /// fill its slot.
    /// </summary>
    public static void MapHorizontalLayoutAlignment(SearchBarHandler handler, ISearchBar searchBar) =>
        handler._fillWidth.Value = searchBar.HorizontalLayoutAlignment
            == Microsoft.Maui.Primitives.LayoutAlignment.Fill;

    /// <summary>
    /// <see cref="MutableState{T}"/> of <see cref="TextFieldValue"/>
    /// that intercepts every write so we can enforce
    /// <see cref="ITextInput.MaxLength"/> and mirror text + caret back
    /// to MAUI's <see cref="ISearchBar"/>. Mirrors the
    /// <see cref="EntryHandler.TextFieldValueState"/> pattern.
    /// </summary>
    sealed class TextFieldValueState : MutableState<TextFieldValue>
    {
        readonly SearchBarHandler _owner;
        bool _suppressMirror;

        public TextFieldValueState(SearchBarHandler owner)
            : base(ComposeExtensions.NewTextFieldValue())
        {
            _owner = owner;
        }

        public void SetWithoutMirror(TextFieldValue value)
        {
            _suppressMirror = true;
            try { Value = value; }
            finally { _suppressMirror = false; }
        }

        public override TextFieldValue Value
        {
            get => base.Value;
            set
            {
                var max = _owner._maxLength.Value;
                if (max >= 0 && value?.Text is { } text && text.Length > max)
                {
                    var trunc = text[..max];
                    var sel = value.Selection;
                    var start = Math.Min((int)sel, max);
                    var end = Math.Min((int)(sel >> 32), max);
                    value = ComposeExtensions.NewTextFieldValue(
                        trunc, TextRangeKt.TextRange(start, end), composition: null);
                }
                base.Value = value
                    ?? throw new InvalidOperationException(
                        "TextFieldValue cannot be null on SearchBarHandler.TextFieldValueState.");
                if (!_suppressMirror && _owner.VirtualView is { } searchBar && value is not null)
                {
                    var newText = value.Text ?? string.Empty;
                    if (searchBar.Text != newText)
                        searchBar.Text = newText;
                    var caret = (int)value.Selection;
                    var selLen = (int)(value.Selection >> 32) - caret;
                    if (searchBar.CursorPosition != caret)
                        searchBar.CursorPosition = caret;
                    if (searchBar.SelectionLength != selLen)
                        searchBar.SelectionLength = selLen;
                }
            }
        }
    }
}
