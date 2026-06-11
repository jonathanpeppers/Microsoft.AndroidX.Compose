using AndroidX.Compose;
using AndroidX.Compose.Runtime;
using Microsoft.Maui.Handlers;
using ComposeColor              = AndroidX.Compose.Color;
using ComposeFontWeight         = AndroidX.Compose.FontWeight;
using ComposeImeAction          = AndroidX.Compose.ImeAction;
using ComposeKeyboardType       = AndroidX.Compose.KeyboardType;
using ComposeOutlinedTextField  = AndroidX.Compose.OutlinedTextField;
using ComposeText               = AndroidX.Compose.Text;
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
            [nameof(IText.Text)]                      = MapText,
            [nameof(ITextStyle.TextColor)]            = MapTextColor,
            [nameof(ITextStyle.Font)]                 = MapFont,
            [nameof(IPlaceholder.Placeholder)]        = MapPlaceholder,
            [nameof(ITextInput.Keyboard)]             = MapKeyboard,
            [nameof(ITextInput.IsReadOnly)]           = MapIsReadOnly,
            [nameof(IView.HorizontalLayoutAlignment)] = MapHorizontalLayoutAlignment,
        };

    /// <summary>Command mapper (inherits view-level commands; no extras).</summary>
    public static CommandMapper<ISearchBar, SearchBarHandler> CommandMapper =
        new(ViewCommandMapper);

    readonly MutableState<string> _text         = new(string.Empty);
    readonly MutableState<long?>  _color        = new((long?)null);
    readonly MutableState<int?>   _fontSize     = new((int?)null);
    readonly MutableState<bool>   _bold         = new(false);
    readonly MutableState<string> _placeholder  = new(string.Empty);
    readonly MutableState<int>    _keyboardType = new(ComposeKeyboardType.Text);
    readonly MutableState<bool>   _readOnly     = new(false);
    readonly MutableState<bool>   _fillWidth    = new(false);

    /// <summary>Construct a handler with the default mappers.</summary>
    public SearchBarHandler() : base(Mapper, CommandMapper) { }

    /// <summary>Construct a handler with custom mappers.</summary>
    public SearchBarHandler(IPropertyMapper? mapper, CommandMapper? commandMapper = null)
        : base(mapper ?? Mapper, commandMapper ?? CommandMapper) { }

    /// <inheritdoc/>
    public override ComposableNode BuildNode(IComposer composer)
    {
        var packed       = _color.Value;
        var size         = _fontSize.Value;
        var bold         = _bold.Value;
        var placeholder  = _placeholder.Value;
        var keyboardType = _keyboardType.Value;
        var readOnly     = _readOnly.Value;
        var fill         = _fillWidth.Value;

        var field = new ComposeOutlinedTextField(_text.Value, OnValueChanged)
        {
            ReadOnly     = readOnly,
            SingleLine   = true,
            // Magnifier glass — pure Unicode keeps us off the
            // material-icons NuGet for the MAUI base layer.
            LeadingIcon  = new ComposeText("\U0001F50D"),
        };
        if (!string.IsNullOrEmpty(placeholder))
            field.Placeholder = new ComposeText(placeholder);
        if (packed.HasValue || size.HasValue || bold)
            field.TextStyle = new ComposeTextStyle
            {
                Color      = packed.HasValue ? new ComposeColor(packed.Value) : null,
                FontSize   = size.HasValue   ? new Sp(size.Value) : null,
                FontWeight = bold ? ComposeFontWeight.Bold : null,
            };

        // KeyboardOptions: route through KeyboardOptionsCompanion.Default.Copy
        // so we override imeAction without disturbing other slots.
        var d = KeyboardOptionsCompanion.Default;
        field.KeyboardOptions = d.Copy(
            d.Capitalization, d.AutoCorrectEnabled,
            keyboardType, ComposeImeAction.Search,
            d.PlatformImeOptions, d.ShowKeyboardOnFocus, d.HintLocales);
        // Wire the search-key callback. SearchButtonPressed() is
        // MAUI's standard trampoline — it fires SearchCommand and the
        // SearchButtonPressed event the way the stock handler does.
        field.KeyboardActions = KeyboardActionsHelper.Create(
            onSearch: OnSearchInvoked);

        if (fill)
            field.PrependModifier(Modifier.FillMaxWidth());
        return field;
    }

    void OnValueChanged(string newValue)
    {
        // Mirror EntryHandler's two-way pattern (issue: feedback-loop
        // guard via MutableState equality).
        _text.Value = newValue;
        if (VirtualView is { } searchBar)
            searchBar.Text = newValue;
    }

    void OnSearchInvoked()
    {
        // SearchButtonPressed is the public ISearchBar trampoline:
        // it ultimately invokes SearchCommand and raises the
        // SearchButtonPressed event on the controls type.
        VirtualView?.SearchButtonPressed();
    }

    /// <summary>Map <see cref="IText.Text"/> to the Compose value slot.</summary>
    public static void MapText(SearchBarHandler handler, ISearchBar searchBar) =>
        handler._text.Value = searchBar.Text ?? string.Empty;

    /// <summary>Map <see cref="ITextStyle.TextColor"/> to the Compose <c>TextStyle.Color</c> slot.</summary>
    public static void MapTextColor(SearchBarHandler handler, ISearchBar searchBar) =>
        handler._color.Value = ColorMapping.ToPackedLong(searchBar.TextColor);

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

    /// <summary>Map <see cref="ITextInput.Keyboard"/> to a Compose <c>KeyboardType</c> int.</summary>
    public static void MapKeyboard(SearchBarHandler handler, ISearchBar searchBar) =>
        handler._keyboardType.Value = KeyboardMapping.Resolve(searchBar.Keyboard, nameof(SearchBarHandler));

    /// <summary>Map <see cref="ITextInput.IsReadOnly"/> to the Compose <c>readOnly</c> slot.</summary>
    public static void MapIsReadOnly(SearchBarHandler handler, ISearchBar searchBar) =>
        handler._readOnly.Value = searchBar.IsReadOnly;

    /// <summary>
    /// Map <see cref="IView.HorizontalLayoutAlignment"/> to
    /// <c>Modifier.fillMaxWidth()</c> when the search bar asks to
    /// fill its slot.
    /// </summary>
    public static void MapHorizontalLayoutAlignment(SearchBarHandler handler, ISearchBar searchBar) =>
        handler._fillWidth.Value = searchBar.HorizontalLayoutAlignment
            == Microsoft.Maui.Primitives.LayoutAlignment.Fill;
}
