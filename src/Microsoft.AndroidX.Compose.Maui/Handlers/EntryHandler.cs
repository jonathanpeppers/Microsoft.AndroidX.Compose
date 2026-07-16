using AndroidX.Compose;
using AndroidX.Compose.Runtime;
using AndroidX.Compose.UI.Text;
using AndroidX.Compose.UI.Text.Input;
using Microsoft.AndroidX.Compose.Maui.Platform;
using Microsoft.Maui.Handlers;
using ComposeColor          = AndroidX.Compose.Color;
using ComposeFontWeight     = AndroidX.Compose.FontWeight;
using ComposeImeAction      = AndroidX.Compose.ImeAction;
using ComposeKeyboardType   = AndroidX.Compose.KeyboardType;
using ComposeOutlinedTextField = AndroidX.Compose.OutlinedTextField;
using ComposeText           = AndroidX.Compose.Text;
using ComposeTextAlign      = AndroidX.Compose.TextAlign;
using ComposeTextStyle      = AndroidX.Compose.TextStyle;

namespace Microsoft.AndroidX.Compose.Maui.Handlers;

/// <summary>
/// MAUI <see cref="Microsoft.Maui.Controls.Entry"/> handler that renders
/// through Jetpack Compose's Material 3 <c>OutlinedTextField</c>
/// composable. Replaces MAUI's stock <c>AppCompatEditText</c>-based
/// handler when the consumer calls
/// <see cref="Hosting.AppHostBuilderExtensions.UseAndroidXCompose"/>.
/// </summary>
/// <remarks>
/// <para>Folds into the page's single composition via
/// <see cref="ComposeElementHandler{TVirtualView}"/> /
/// <see cref="IComposeHandler"/>. Uses <c>OutlinedTextField</c>
/// rather than the filled <c>TextField</c> variant because it
/// matches MAUI's stock Entry chrome more closely (clear bordered
/// outline, no shaded background fill, label that floats above the
/// border when the field gains focus).</para>
///
/// <para>The value/cursor/selection state is bound through Compose's
/// <c>OutlinedTextField(MutableState&lt;TextFieldValue&gt;)</c> overload
/// — a custom <see cref="MutableState{T}"/> subclass intercepts every
/// write so we can enforce <see cref="ITextInput.MaxLength"/>
/// (Compose has no built-in slot) and mirror text + caret back to
/// MAUI's <see cref="IEntry"/>.</para>
/// </remarks>
public partial class EntryHandler : ComposeElementHandler<IEntry>
{
    /// <summary>
    /// Property mapper that forwards MAUI <see cref="IEntry"/> property
    /// changes to the Compose-backed <see cref="ComposeView"/>.
    /// </summary>
    public static IPropertyMapper<IEntry, EntryHandler> Mapper =
        new PropertyMapper<IEntry, EntryHandler>(ViewHandler.ViewMapper)
        {
            [nameof(IText.Text)]                       = MapText,
            [nameof(ITextStyle.TextColor)]             = MapTextColor,
            [nameof(ITextStyle.CharacterSpacing)]      = MapCharacterSpacing,
            [nameof(ITextStyle.Font)]                  = MapFont,
            [nameof(IPlaceholder.Placeholder)]         = MapPlaceholder,
            [nameof(IPlaceholder.PlaceholderColor)]    = MapPlaceholderColor,
            [nameof(IEntry.IsPassword)]                = MapIsPassword,
            [nameof(IEntry.ReturnType)]                = MapReturnType,
            [nameof(IEntry.ClearButtonVisibility)]     = MapClearButtonVisibility,
            [nameof(ITextInput.Keyboard)]              = MapKeyboard,
            [nameof(ITextInput.IsReadOnly)]            = MapIsReadOnly,
            [nameof(ITextInput.IsSpellCheckEnabled)]   = MapAutoCorrect,
            [nameof(ITextInput.IsTextPredictionEnabled)] = MapAutoCorrect,
            [nameof(ITextInput.MaxLength)]             = MapMaxLength,
            [nameof(ITextInput.CursorPosition)]        = MapCursorPosition,
            [nameof(ITextInput.SelectionLength)]       = MapSelectionLength,
            [nameof(ITextAlignment.HorizontalTextAlignment)] = MapHorizontalTextAlignment,
            [nameof(ITextAlignment.VerticalTextAlignment)]   = MapVerticalTextAlignment,
            [nameof(IView.HorizontalLayoutAlignment)]  = MapHorizontalLayoutAlignment,
        };

    /// <summary>Command mapper (inherits view-level commands; no extras).</summary>
    public static CommandMapper<IEntry, EntryHandler> CommandMapper =
        new(ViewCommandMapper);

    readonly TextFieldValueState _tfv;
    readonly MutableState<long?>  _color           = new((long?)null);
    readonly MutableState<long?>  _placeholderColor = new((long?)null);
    readonly MutableState<int?>   _fontSize        = new((int?)null);
    readonly MutableState<bool>   _bold            = new(false);
    readonly MutableState<string> _placeholder     = new(string.Empty);
    readonly MutableState<bool>   _isPassword      = new(false);
    readonly MutableState<int>    _keyboardType    = new(ComposeKeyboardType.Text);
    readonly MutableState<bool>   _readOnly        = new(false);
    readonly MutableState<bool>   _autoCorrect     = new(true);
    readonly MutableState<int>    _imeAction       = new(ComposeImeAction.Default);
    readonly MutableState<float?> _letterSpacing   = new((float?)null);
    readonly MutableState<int>    _hTextAlign      = new((int)TextAlignment.Start);
    readonly MutableState<int>    _vTextAlign      = new((int)TextAlignment.Center);
    readonly MutableState<int>    _clearButtonMode = new((int)ClearButtonVisibility.Never);
    readonly MutableState<int>    _maxLength       = new(-1);
    readonly MutableState<bool>   _fillWidth       = new(false);

    /// <summary>Construct a handler with the default mappers.</summary>
    public EntryHandler() : base(Mapper, CommandMapper)
    {
        _tfv = new TextFieldValueState(this);
    }

    /// <summary>Construct a handler with custom mappers.</summary>
    public EntryHandler(IPropertyMapper? mapper, CommandMapper? commandMapper = null)
        : base(mapper ?? Mapper, commandMapper ?? CommandMapper)
    {
        _tfv = new TextFieldValueState(this);
    }

    /// <inheritdoc/>
    public override ComposableNode BuildNode(IComposer composer)
    {
        var virtualView = VirtualView
            ?? throw new InvalidOperationException("VirtualView not set on EntryHandler.");

        SubscribeToViewProperties();

        var packed          = _color.Value;
        var placeholderInk  = _placeholderColor.Value;
        var size            = _fontSize.Value;
        var bold            = _bold.Value;
        var placeholder     = _placeholder.Value;
        var isPassword      = _isPassword.Value;
        var keyboardType    = _keyboardType.Value;
        var readOnly        = _readOnly.Value;
        var autoCorrect     = _autoCorrect.Value;
        var imeAction       = _imeAction.Value;
        var letterSpacing   = _letterSpacing.Value;
        var hTextAlign      = (TextAlignment)_hTextAlign.Value;
        var clearButtonMode = (ClearButtonVisibility)_clearButtonMode.Value;
        var fill            = _fillWidth.Value;

        var field = new ComposeOutlinedTextField(
            _tfv,
            readOnly: readOnly,
            singleLine: true);
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
        if (isPassword)
            field.VisualTransformation = new PasswordVisualTransformation('•');
        // Always set KeyboardOptions so OS surfaces the right IME action +
        // honours autocorrect / keyboard-type overrides. Password buffers +
        // numeric flag both lower through `keyboardType`.
        var resolvedType = isPassword ? ComposeKeyboardType.Password : keyboardType;
        var d = KeyboardOptionsCompanion.Default;
        field.KeyboardOptions = d.Copy(
            d.Capitalization, (Java.Lang.Boolean)autoCorrect,
            resolvedType, imeAction,
            d.PlatformImeOptions, d.ShowKeyboardOnFocus, d.HintLocales);

        // Wire the IME action callback so MAUI Entry.Completed (and
        // SearchButtonPressed-style return-key handling) fires when the
        // user taps the IME's action key.
        field.KeyboardActions = KeyboardActionsHelper.Create(
            onDone:     OnCompleted,
            onGo:       OnCompleted,
            onNext:     OnCompleted,
            onSearch:   OnCompleted,
            onSend:     OnCompleted,
            onPrevious: OnCompleted);

        // Clear-X trailing icon when MAUI's ClearButtonVisibility ==
        // WhileEditing and the text is non-empty. We always render it
        // when WhileEditing because Compose's OutlinedTextField doesn't
        // expose a "focused" flag without subscribing to FocusInteraction
        // state — close enough for parity with stock MAUI on Android.
        if (clearButtonMode == ClearButtonVisibility.WhileEditing
            && !string.IsNullOrEmpty(_tfv.Value?.Text))
        {
            field.TrailingIcon = new IconButton(onClick: () =>
            {
                if (VirtualView is { } entry)
                    entry.Text = string.Empty;
            })
            {
                new ComposeText("\u2715"), // ✕
            };
        }

        // Single chained PrependModifier — combines layout-fill with
        // the cross-cutting view properties.
        var vAlign = (TextAlignment)_vTextAlign.Value;
        var outer = (fill ? Modifier.FillMaxWidth() : Modifier.Companion)
            .ApplyViewProperties(virtualView)
            .ApplyGestures(virtualView, MauiContext)
            .ApplySemantics(virtualView)
            .ApplyVerticalTextAlignment(vAlign);
        field.PrependModifier(outer);
        return field;
    }

    void OnCompleted()
    {
        if (VirtualView is Microsoft.Maui.Controls.Entry e)
            e.SendCompleted();
    }

    /// <summary>Map <see cref="IText.Text"/> to the Compose value slot.</summary>
    public static void MapText(EntryHandler handler, IEntry entry)
    {
        var newText = entry.Text ?? string.Empty;
        var current = handler._tfv.Value;
        if (current?.Text == newText) return;
        // Text changed externally — snap cursor to end so the IME
        // doesn't try to re-render at a stale offset.
        var cursor = Math.Min(entry.CursorPosition, newText.Length);
        if (cursor < 0) cursor = newText.Length;
        handler._tfv.SetWithoutMirror(ComposeExtensions.NewTextFieldValue(
            newText, cursor));
    }

    /// <summary>Map <see cref="ITextStyle.TextColor"/> to the Compose <c>TextStyle.Color</c> slot.</summary>
    public static void MapTextColor(EntryHandler handler, IEntry entry) =>
        handler._color.Value = ColorMapping.ToPackedLong(entry.TextColor);

    /// <summary>Map <see cref="ITextStyle.CharacterSpacing"/> to Compose <c>letterSpacing</c>.</summary>
    public static void MapCharacterSpacing(EntryHandler handler, IEntry entry) =>
        handler._letterSpacing.Value = entry.CharacterSpacing != 0
            ? (float)entry.CharacterSpacing
            : null;

    /// <summary>Map <see cref="ITextStyle.Font"/> (size + bold) to Compose <c>TextStyle</c> slots.</summary>
    public static void MapFont(EntryHandler handler, IEntry entry)
    {
        var font = entry.Font;
        handler._fontSize.Value = font.Size > 0 ? (int)font.Size : null;
        handler._bold.Value     = (font.Weight & Microsoft.Maui.FontWeight.Bold)
            == Microsoft.Maui.FontWeight.Bold;
    }

    /// <summary>Map <see cref="IPlaceholder.Placeholder"/> to the Compose placeholder slot.</summary>
    public static void MapPlaceholder(EntryHandler handler, IEntry entry) =>
        handler._placeholder.Value = entry.Placeholder ?? string.Empty;

    /// <summary>Map <see cref="IPlaceholder.PlaceholderColor"/> to the placeholder Text's color.</summary>
    public static void MapPlaceholderColor(EntryHandler handler, IEntry entry) =>
        handler._placeholderColor.Value = ColorMapping.ToPackedLong(entry.PlaceholderColor);

    /// <summary>Map <see cref="IEntry.IsPassword"/> to the visualTransformation + keyboardType slots.</summary>
    public static void MapIsPassword(EntryHandler handler, IEntry entry) =>
        handler._isPassword.Value = entry.IsPassword;

    /// <summary>Map <see cref="IEntry.ReturnType"/> to <c>KeyboardOptions.imeAction</c>.</summary>
    public static void MapReturnType(EntryHandler handler, IEntry entry) =>
        handler._imeAction.Value = entry.ReturnType switch
        {
            ReturnType.Done   => ComposeImeAction.Done,
            ReturnType.Go     => ComposeImeAction.Go,
            ReturnType.Next   => ComposeImeAction.Next,
            ReturnType.Search => ComposeImeAction.Search,
            ReturnType.Send   => ComposeImeAction.Send,
            _                 => ComposeImeAction.Default,
        };

    /// <summary>Map <see cref="IEntry.ClearButtonVisibility"/> to the trailing X icon toggle.</summary>
    public static void MapClearButtonVisibility(EntryHandler handler, IEntry entry) =>
        handler._clearButtonMode.Value = (int)entry.ClearButtonVisibility;

    /// <summary>Map <see cref="ITextInput.Keyboard"/> to a Compose <c>KeyboardType</c> int.</summary>
    public static void MapKeyboard(EntryHandler handler, IEntry entry) =>
        handler._keyboardType.Value = KeyboardMapping.Resolve(entry.Keyboard, nameof(EntryHandler));

    /// <summary>Map <see cref="ITextInput.IsReadOnly"/> to the Compose <c>readOnly</c> slot.</summary>
    public static void MapIsReadOnly(EntryHandler handler, IEntry entry) =>
        handler._readOnly.Value = entry.IsReadOnly;

    /// <summary>
    /// Combined map for <see cref="ITextInput.IsSpellCheckEnabled"/> +
    /// <see cref="ITextInput.IsTextPredictionEnabled"/>. Compose has a single
    /// <c>autoCorrectEnabled</c> slot driving both; we AND the MAUI flags
    /// (autocorrect is only on if both checks pass).
    /// </summary>
    public static void MapAutoCorrect(EntryHandler handler, IEntry entry) =>
        handler._autoCorrect.Value = entry.IsSpellCheckEnabled && entry.IsTextPredictionEnabled;

    /// <summary>Map <see cref="ITextInput.MaxLength"/> to the truncation cap (negative = unlimited).</summary>
    public static void MapMaxLength(EntryHandler handler, IEntry entry)
    {
        handler._maxLength.Value = entry.MaxLength;
        // Re-truncate the current buffer if it's now over the cap.
        if (entry.MaxLength >= 0
            && handler._tfv.Value is { } current
            && (current.Text?.Length ?? 0) > entry.MaxLength)
        {
            handler._tfv.Value = current;  // override re-applies truncation
        }
    }

    /// <summary>Map <see cref="ITextInput.CursorPosition"/> to the TFV selection start.</summary>
    public static void MapCursorPosition(EntryHandler handler, IEntry entry) =>
        ApplySelection(handler, entry);

    /// <summary>Map <see cref="ITextInput.SelectionLength"/> to the TFV selection end.</summary>
    public static void MapSelectionLength(EntryHandler handler, IEntry entry) =>
        ApplySelection(handler, entry);

    static void ApplySelection(EntryHandler handler, IEntry entry)
    {
        var current = handler._tfv.Value;
        if (current is null) return;
        var text = current.Text ?? string.Empty;
        var start = Math.Clamp(entry.CursorPosition, 0, text.Length);
        var length = Math.Max(0, Math.Min(entry.SelectionLength, text.Length - start));
        var end = start + length;
        // Skip writes that wouldn't actually change Compose's selection —
        // re-entrance from OnSelectionChanged hits this constantly.
        var existing = current.Selection;
        if ((int)existing == start && (int)(existing >> 32) == end)
            return;
        handler._tfv.SetWithoutMirror(current.Copy(text, TextRangeKt.TextRange(start, end), composition: null));
    }

    /// <summary>Map <see cref="ITextAlignment.HorizontalTextAlignment"/> to Compose <c>textAlign</c>.</summary>
    public static void MapHorizontalTextAlignment(EntryHandler handler, IEntry entry) =>
        handler._hTextAlign.Value = (int)entry.HorizontalTextAlignment;

    /// <summary>
    /// Map <see cref="ITextAlignment.VerticalTextAlignment"/> to a
    /// <c>Modifier.wrapContentHeight(Alignment.Vertical)</c> on the
    /// outer modifier. Single-line entries clamp to a fixed line
    /// height and only honour this when the entry's allocated height
    /// exceeds the natural one (e.g. an explicit <c>HeightRequest</c>),
    /// matching stock MAUI behaviour.
    /// </summary>
    public static void MapVerticalTextAlignment(EntryHandler handler, IEntry entry) =>
        handler._vTextAlign.Value = (int)entry.VerticalTextAlignment;

    /// <summary>
    /// Map <see cref="IView.HorizontalLayoutAlignment"/> to
    /// <c>Modifier.fillMaxWidth()</c> when the entry asks to fill its
    /// slot. Compose's <c>OutlinedTextField</c> hugs its content by
    /// default — an entry with <c>HorizontalOptions="Fill"</c> renders
    /// as a tiny pill on the left without this.
    /// </summary>
    public static void MapHorizontalLayoutAlignment(EntryHandler handler, IEntry entry) =>
        handler._fillWidth.Value = entry.HorizontalLayoutAlignment
            == Microsoft.Maui.Primitives.LayoutAlignment.Fill;

    /// <summary>
    /// <see cref="MutableState{T}"/> of <see cref="TextFieldValue"/>
    /// that intercepts every write so we can enforce
    /// <see cref="ITextInput.MaxLength"/> (Compose has no built-in
    /// slot) and mirror the resulting text + caret position back to
    /// MAUI's <see cref="IEntry"/>. Compose's
    /// <c>OutlinedTextField(MutableState&lt;TextFieldValue&gt;)</c>
    /// ctor writes <c>state.Value = newTfv</c> from its
    /// <c>onValueChange</c> lambda, so overriding the
    /// <see cref="Value"/> setter intercepts user edits as well as
    /// programmatic <see cref="MapText"/> writes.
    /// </summary>
    sealed class TextFieldValueState : MutableState<TextFieldValue>
    {
        readonly EntryHandler _owner;
        bool _suppressMirror;

        public TextFieldValueState(EntryHandler owner)
            : base(ComposeExtensions.NewTextFieldValue())
        {
            _owner = owner;
        }

        // Programmatic write that updates the Compose state without
        // bouncing the new value back through MAUI's IEntry.Text setter
        // (which would feed-back into MapText). Used by MapText and
        // ApplySelection where the value is already authoritative on
        // the MAUI side.
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
                    // Clamp existing selection within the new bounds.
                    var sel = value.Selection;
                    var start = Math.Min((int)sel, max);
                    var end = Math.Min((int)(sel >> 32), max);
                    value = ComposeExtensions.NewTextFieldValue(
                        trunc, start, end);
                }
                base.Value = value
                    ?? throw new InvalidOperationException(
                        "TextFieldValue cannot be null on EntryHandler.TextFieldValueState.");
                if (!_suppressMirror && _owner.VirtualView is { } entry && value is not null)
                {
                    var newText = value.Text ?? string.Empty;
                    if (entry.Text != newText)
                        entry.Text = newText;
                    var caret = (int)value.Selection;
                    var selLen = (int)(value.Selection >> 32) - caret;
                    if (entry.CursorPosition != caret)
                        entry.CursorPosition = caret;
                    if (entry.SelectionLength != selLen)
                        entry.SelectionLength = selLen;
                }
            }
        }
    }
}
