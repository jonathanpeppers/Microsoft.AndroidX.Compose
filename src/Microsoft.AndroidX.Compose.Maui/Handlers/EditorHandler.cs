using AndroidX.Compose;
using AndroidX.Compose.Runtime;
using AndroidX.Compose.UI.Text;
using AndroidX.Compose.UI.Text.Input;
using Microsoft.AndroidX.Compose.Maui.Platform;
using Microsoft.Maui.Handlers;
using ComposeColor             = AndroidX.Compose.Color;
using ComposeFontWeight        = AndroidX.Compose.FontWeight;
using ComposeKeyboardType      = AndroidX.Compose.KeyboardType;
using ComposeOutlinedTextField = AndroidX.Compose.OutlinedTextField;
using ComposeText              = AndroidX.Compose.Text;
using ComposeTextAlign         = AndroidX.Compose.TextAlign;
using ComposeTextStyle         = AndroidX.Compose.TextStyle;

namespace Microsoft.AndroidX.Compose.Maui.Handlers;

/// <summary>
/// MAUI <see cref="Microsoft.Maui.Controls.Editor"/> handler that
/// renders through Jetpack Compose's Material 3
/// <c>OutlinedTextField</c> composable in multi-line mode. Replaces
/// MAUI's stock <c>AppCompatEditText</c>-based editor handler when
/// the consumer calls
/// <see cref="Hosting.AppHostBuilderExtensions.UseAndroidXCompose"/>.
/// </summary>
/// <remarks>
/// <para>Folds into the page's single composition via
/// <see cref="ComposeElementHandler{TVirtualView}"/> /
/// <see cref="IComposeHandler"/>. Same facade as
/// <see cref="EntryHandler"/> but flips
/// <c>singleLine: false</c>, omits <c>maxLines</c> so the field
/// grows with content, and applies a <c>Modifier.HeightIn(min: 96.Dp)</c>
/// so the field starts tall enough to look like a
/// <c>TextArea</c>.</para>
///
/// <para>Like <see cref="EntryHandler"/>, value/cursor/selection state
/// is bound through Compose's
/// <c>OutlinedTextField(MutableState&lt;TextFieldValue&gt;)</c>
/// overload via a custom <see cref="MutableState{T}"/> subclass that
/// enforces <see cref="ITextInput.MaxLength"/> and mirrors text +
/// caret back to MAUI's <see cref="IEditor"/>.</para>
/// </remarks>
public partial class EditorHandler : ComposeElementHandler<IEditor>
{
    /// <summary>
    /// Property mapper that forwards MAUI <see cref="IEditor"/>
    /// property changes to the Compose-backed
    /// <see cref="AndroidX.Compose.UI.Platform.ComposeView"/>.
    /// </summary>
    public static IPropertyMapper<IEditor, EditorHandler> Mapper =
        new PropertyMapper<IEditor, EditorHandler>(ViewHandler.ViewMapper)
        {
            [nameof(IText.Text)]                            = MapText,
            [nameof(ITextStyle.TextColor)]                  = MapTextColor,
            [nameof(ITextStyle.CharacterSpacing)]           = MapCharacterSpacing,
            [nameof(ITextStyle.Font)]                       = MapFont,
            [nameof(IPlaceholder.Placeholder)]              = MapPlaceholder,
            [nameof(IPlaceholder.PlaceholderColor)]         = MapPlaceholderColor,
            [nameof(ITextInput.Keyboard)]                   = MapKeyboard,
            [nameof(ITextInput.IsReadOnly)]                 = MapIsReadOnly,
            [nameof(ITextInput.IsSpellCheckEnabled)]        = MapAutoCorrect,
            [nameof(ITextInput.IsTextPredictionEnabled)]    = MapAutoCorrect,
            [nameof(ITextInput.MaxLength)]                  = MapMaxLength,
            [nameof(ITextInput.CursorPosition)]             = MapCursorPosition,
            [nameof(ITextInput.SelectionLength)]            = MapSelectionLength,
            [nameof(ITextAlignment.HorizontalTextAlignment)] = MapHorizontalTextAlignment,
            // TODO: VerticalTextAlignment — Compose's Box facade doesn't
            // expose the `contentAlignment` slot (always passes null), so
            // we can't wrap the OutlinedTextField and align it. Leaving
            // unmapped instead of wiring a no-op mapper.
            [nameof(IView.HorizontalLayoutAlignment)]       = MapHorizontalLayoutAlignment,
        };

    /// <summary>Command mapper (inherits view-level commands; no extras).</summary>
    public static CommandMapper<IEditor, EditorHandler> CommandMapper =
        new(ViewCommandMapper);

    readonly TextFieldValueState _tfv;
    readonly MutableState<long?>  _color            = new((long?)null);
    readonly MutableState<long?>  _placeholderColor = new((long?)null);
    readonly MutableState<int?>   _fontSize         = new((int?)null);
    readonly MutableState<bool>   _bold             = new(false);
    readonly MutableState<string> _placeholder      = new(string.Empty);
    readonly MutableState<int>    _keyboardType     = new(ComposeKeyboardType.Text);
    readonly MutableState<bool>   _readOnly         = new(false);
    readonly MutableState<bool>   _autoCorrect      = new(true);
    readonly MutableState<int>    _maxLength        = new(-1);
    readonly MutableState<float?> _letterSpacing    = new((float?)null);
    readonly MutableState<int>    _hTextAlign       = new((int)TextAlignment.Start);
    readonly MutableState<bool>   _fillWidth        = new(false);

    /// <summary>Construct a handler with the default mappers.</summary>
    public EditorHandler() : base(Mapper, CommandMapper)
    {
        _tfv = new TextFieldValueState(this);
    }

    /// <summary>Construct a handler with custom mappers.</summary>
    public EditorHandler(IPropertyMapper? mapper, CommandMapper? commandMapper = null)
        : base(mapper ?? Mapper, commandMapper ?? CommandMapper)
    {
        _tfv = new TextFieldValueState(this);
    }

    /// <inheritdoc/>
    public override ComposableNode BuildNode(IComposer composer)
    {
        var virtualView = VirtualView
            ?? throw new InvalidOperationException("VirtualView not set on EditorHandler.");

        var packed          = _color.Value;
        var placeholderInk  = _placeholderColor.Value;
        var size            = _fontSize.Value;
        var bold            = _bold.Value;
        var placeholder     = _placeholder.Value;
        var keyboardType    = _keyboardType.Value;
        var readOnly        = _readOnly.Value;
        var autoCorrect     = _autoCorrect.Value;
        var letterSpacing   = _letterSpacing.Value;
        var hTextAlign      = (TextAlignment)_hTextAlign.Value;
        var fill            = _fillWidth.Value;

        var field = new ComposeOutlinedTextField(_tfv)
        {
            ReadOnly   = readOnly,
            // Multi-line — the whole point of Editor.
            SingleLine = false,
        };
        if (!string.IsNullOrEmpty(placeholder))
            field.Placeholder = new ComposeText(placeholder)
            {
                Color = placeholderInk.HasValue ? new ComposeColor(placeholderInk.Value) : null,
            };
        if (packed.HasValue || size.HasValue || bold || letterSpacing.HasValue
            || hTextAlign != TextAlignment.Start)
            field.TextStyle = new ComposeTextStyle
            {
                Color         = packed.HasValue ? new ComposeColor(packed.Value) : null,
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
        // KeyboardOptions: always set so autocorrect + keyboardType
        // overrides take effect — same pattern as EntryHandler.
        var d = KeyboardOptionsCompanion.Default;
        field.KeyboardOptions = d.Copy(
            d.Capitalization, (Java.Lang.Boolean)autoCorrect,
            keyboardType, d.ImeAction,
            d.PlatformImeOptions, d.ShowKeyboardOnFocus, d.HintLocales);

        // Tall default — feels like an editor, not a one-line entry.
        // Stack on top of any caller-supplied modifier (Layout chains).
        var modifier = Modifier.HeightIn(min: new Dp(96));
        if (fill)
            modifier = modifier.FillMaxWidth();
        modifier = modifier.ApplyGestures(virtualView, MauiContext).ApplySemantics(virtualView);
        field.PrependModifier(modifier);
        return field;
    }

    /// <summary>Map <see cref="IText.Text"/> to the Compose value slot.</summary>
    public static void MapText(EditorHandler handler, IEditor editor)
    {
        var newText = editor.Text ?? string.Empty;
        var current = handler._tfv.Value;
        if (current?.Text == newText) return;
        var cursor = Math.Min(editor.CursorPosition, newText.Length);
        if (cursor < 0) cursor = newText.Length;
        handler._tfv.SetWithoutMirror(ComposeExtensions.NewTextFieldValue(
            newText, TextRangeKt.TextRange(cursor), composition: null));
    }

    /// <summary>Map <see cref="ITextStyle.TextColor"/> to the Compose <c>TextStyle.Color</c> slot.</summary>
    public static void MapTextColor(EditorHandler handler, IEditor editor) =>
        handler._color.Value = ColorMapping.ToPackedLong(editor.TextColor);

    /// <summary>Map <see cref="ITextStyle.CharacterSpacing"/> to Compose <c>letterSpacing</c>.</summary>
    public static void MapCharacterSpacing(EditorHandler handler, IEditor editor) =>
        handler._letterSpacing.Value = editor.CharacterSpacing != 0
            ? (float)editor.CharacterSpacing
            : null;

    /// <summary>Map <see cref="ITextStyle.Font"/> (size + bold) to Compose <c>TextStyle</c> slots.</summary>
    public static void MapFont(EditorHandler handler, IEditor editor)
    {
        var font = editor.Font;
        handler._fontSize.Value = font.Size > 0 ? (int)font.Size : null;
        handler._bold.Value     = (font.Weight & Microsoft.Maui.FontWeight.Bold)
            == Microsoft.Maui.FontWeight.Bold;
    }

    /// <summary>Map <see cref="IPlaceholder.Placeholder"/> to the Compose placeholder slot.</summary>
    public static void MapPlaceholder(EditorHandler handler, IEditor editor) =>
        handler._placeholder.Value = editor.Placeholder ?? string.Empty;

    /// <summary>Map <see cref="IPlaceholder.PlaceholderColor"/> to the placeholder Text's color.</summary>
    public static void MapPlaceholderColor(EditorHandler handler, IEditor editor) =>
        handler._placeholderColor.Value = ColorMapping.ToPackedLong(editor.PlaceholderColor);

    /// <summary>Map <see cref="ITextInput.Keyboard"/> to a Compose <c>KeyboardType</c> int.</summary>
    public static void MapKeyboard(EditorHandler handler, IEditor editor) =>
        handler._keyboardType.Value = KeyboardMapping.Resolve(editor.Keyboard, nameof(EditorHandler));

    /// <summary>Map <see cref="ITextInput.IsReadOnly"/> to the Compose <c>readOnly</c> slot.</summary>
    public static void MapIsReadOnly(EditorHandler handler, IEditor editor) =>
        handler._readOnly.Value = editor.IsReadOnly;

    /// <summary>
    /// Combined map for <see cref="ITextInput.IsSpellCheckEnabled"/> +
    /// <see cref="ITextInput.IsTextPredictionEnabled"/>. Compose has a
    /// single <c>autoCorrectEnabled</c> slot driving both; we AND the
    /// MAUI flags (autocorrect is only on if both checks pass).
    /// </summary>
    public static void MapAutoCorrect(EditorHandler handler, IEditor editor) =>
        handler._autoCorrect.Value = editor.IsSpellCheckEnabled && editor.IsTextPredictionEnabled;

    /// <summary>Map <see cref="ITextInput.MaxLength"/> to the truncation cap (negative = unlimited).</summary>
    public static void MapMaxLength(EditorHandler handler, IEditor editor)
    {
        handler._maxLength.Value = editor.MaxLength;
        // Re-truncate the current buffer if it's now over the cap.
        if (editor.MaxLength >= 0
            && handler._tfv.Value is { } current
            && (current.Text?.Length ?? 0) > editor.MaxLength)
        {
            handler._tfv.Value = current;
        }
    }

    /// <summary>Map <see cref="ITextInput.CursorPosition"/> to the TFV selection start.</summary>
    public static void MapCursorPosition(EditorHandler handler, IEditor editor) =>
        ApplySelection(handler, editor);

    /// <summary>Map <see cref="ITextInput.SelectionLength"/> to the TFV selection end.</summary>
    public static void MapSelectionLength(EditorHandler handler, IEditor editor) =>
        ApplySelection(handler, editor);

    static void ApplySelection(EditorHandler handler, IEditor editor)
    {
        var current = handler._tfv.Value;
        if (current is null) return;
        var text = current.Text ?? string.Empty;
        var start = Math.Clamp(editor.CursorPosition, 0, text.Length);
        var length = Math.Max(0, Math.Min(editor.SelectionLength, text.Length - start));
        var end = start + length;
        var existing = current.Selection;
        if ((int)existing == start && (int)(existing >> 32) == end)
            return;
        handler._tfv.SetWithoutMirror(current.Copy(text, TextRangeKt.TextRange(start, end), composition: null));
    }

    /// <summary>Map <see cref="ITextAlignment.HorizontalTextAlignment"/> to Compose <c>textAlign</c>.</summary>
    public static void MapHorizontalTextAlignment(EditorHandler handler, IEditor editor) =>
        handler._hTextAlign.Value = (int)editor.HorizontalTextAlignment;

    /// <summary>
    /// Map <see cref="IView.HorizontalLayoutAlignment"/> to
    /// <c>Modifier.fillMaxWidth()</c> when the editor asks to fill its
    /// slot — same rationale as <see cref="EntryHandler"/>.
    /// </summary>
    public static void MapHorizontalLayoutAlignment(EditorHandler handler, IEditor editor) =>
        handler._fillWidth.Value = editor.HorizontalLayoutAlignment
            == Microsoft.Maui.Primitives.LayoutAlignment.Fill;

    /// <summary>
    /// <see cref="MutableState{T}"/> of <see cref="TextFieldValue"/>
    /// that intercepts every write so we can enforce
    /// <see cref="ITextInput.MaxLength"/> (Compose has no built-in
    /// slot) and mirror text + caret back to MAUI's
    /// <see cref="IEditor"/>. Mirrors the
    /// <see cref="EntryHandler.TextFieldValueState"/> pattern.
    /// </summary>
    sealed class TextFieldValueState : MutableState<TextFieldValue>
    {
        readonly EditorHandler _owner;
        bool _suppressMirror;

        public TextFieldValueState(EditorHandler owner)
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
                        "TextFieldValue cannot be null on EditorHandler.TextFieldValueState.");
                if (!_suppressMirror && _owner.VirtualView is { } editor && value is not null)
                {
                    var newText = value.Text ?? string.Empty;
                    if (editor.Text != newText)
                        editor.Text = newText;
                    var caret = (int)value.Selection;
                    var selLen = (int)(value.Selection >> 32) - caret;
                    if (editor.CursorPosition != caret)
                        editor.CursorPosition = caret;
                    if (editor.SelectionLength != selLen)
                        editor.SelectionLength = selLen;
                }
            }
        }
    }
}
