using AndroidX.Compose;
using AndroidX.Compose.Runtime;
using Microsoft.AndroidX.Compose.Maui.Platform;
using Microsoft.Maui.Handlers;
using ComposeColor             = AndroidX.Compose.Color;
using ComposeFontWeight        = AndroidX.Compose.FontWeight;
using ComposeKeyboardType      = AndroidX.Compose.KeyboardType;
using ComposeOutlinedTextField = AndroidX.Compose.OutlinedTextField;
using ComposeText              = AndroidX.Compose.Text;
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
/// <para><see cref="IEditor.MaxLength"/> is enforced inside
/// <see cref="OnValueChanged(string)"/> by truncating the new text
/// — Compose's <c>OutlinedTextField</c> doesn't expose a
/// <c>maxLength</c> slot directly. <see cref="Editor.AutoSize"/>
/// is a no-op: Compose's multi-line <c>OutlinedTextField</c>
/// already grows with content when no <c>maxLines</c> is set.</para>
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
            [nameof(IText.Text)]                      = MapText,
            [nameof(ITextStyle.TextColor)]            = MapTextColor,
            [nameof(ITextStyle.Font)]                 = MapFont,
            [nameof(IPlaceholder.Placeholder)]        = MapPlaceholder,
            [nameof(ITextInput.Keyboard)]             = MapKeyboard,
            [nameof(ITextInput.IsReadOnly)]           = MapIsReadOnly,
            [nameof(ITextInput.MaxLength)]            = MapMaxLength,
            [nameof(IView.HorizontalLayoutAlignment)] = MapHorizontalLayoutAlignment,
        };

    /// <summary>Command mapper (inherits view-level commands; no extras).</summary>
    public static CommandMapper<IEditor, EditorHandler> CommandMapper =
        new(ViewCommandMapper);

    readonly MutableState<string> _text         = new(string.Empty);
    readonly MutableState<long?>  _color        = new((long?)null);
    readonly MutableState<int?>   _fontSize     = new((int?)null);
    readonly MutableState<bool>   _bold         = new(false);
    readonly MutableState<string> _placeholder  = new(string.Empty);
    readonly MutableState<int>    _keyboardType = new(ComposeKeyboardType.Text);
    readonly MutableState<bool>   _readOnly     = new(false);
    readonly MutableState<int>    _maxLength    = new(-1);
    readonly MutableState<bool>   _fillWidth    = new(false);

    /// <summary>Construct a handler with the default mappers.</summary>
    public EditorHandler() : base(Mapper, CommandMapper) { }

    /// <summary>Construct a handler with custom mappers.</summary>
    public EditorHandler(IPropertyMapper? mapper, CommandMapper? commandMapper = null)
        : base(mapper ?? Mapper, commandMapper ?? CommandMapper) { }

    /// <inheritdoc/>
    public override ComposableNode BuildNode(IComposer composer)
    {
        var virtualView = VirtualView
            ?? throw new InvalidOperationException("VirtualView not set on EditorHandler.");

        var packed       = _color.Value;
        var size         = _fontSize.Value;
        var bold         = _bold.Value;
        var placeholder  = _placeholder.Value;
        var keyboardType = _keyboardType.Value;
        var readOnly     = _readOnly.Value;
        var fill         = _fillWidth.Value;

        var field = new ComposeOutlinedTextField(_text.Value, OnValueChanged)
        {
            ReadOnly   = readOnly,
            // Multi-line — the whole point of Editor.
            SingleLine = false,
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
        if (keyboardType != ComposeKeyboardType.Text)
        {
            var d = KeyboardOptionsCompanion.Default;
            field.KeyboardOptions = d.Copy(
                d.Capitalization, d.AutoCorrectEnabled,
                keyboardType, d.ImeAction,
                d.PlatformImeOptions, d.ShowKeyboardOnFocus, d.HintLocales);
        }

        // Tall default — feels like an editor, not a one-line entry.
        // Stack on top of any caller-supplied modifier (Layout chains).
        var modifier = Modifier.HeightIn(min: new Dp(96));
        if (fill)
            modifier = modifier.FillMaxWidth();
        modifier = modifier.ApplyGestures(virtualView, MauiContext).ApplySemantics(virtualView);
        field.PrependModifier(modifier);
        return field;
    }

    void OnValueChanged(string newValue)
    {
        // MAUI Editor.MaxLength: truncate before push-back so the user
        // can't sneak past the cap. Compose's OutlinedTextField has no
        // built-in maxLength slot.
        var max = _maxLength.Value;
        if (max >= 0 && newValue.Length > max)
            newValue = newValue.Substring(0, max);

        // Update Compose state synchronously so the rendered value stays
        // pinned to what the user just typed (mirrors EntryHandler).
        _text.Value = newValue;
        if (VirtualView is { } editor)
            editor.Text = newValue;
    }

    /// <summary>Map <see cref="IText.Text"/> to the Compose value slot.</summary>
    public static void MapText(EditorHandler handler, IEditor editor) =>
        handler._text.Value = editor.Text ?? string.Empty;

    /// <summary>Map <see cref="ITextStyle.TextColor"/> to the Compose <c>TextStyle.Color</c> slot.</summary>
    public static void MapTextColor(EditorHandler handler, IEditor editor) =>
        handler._color.Value = ColorMapping.ToPackedLong(editor.TextColor);

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

    /// <summary>Map <see cref="ITextInput.Keyboard"/> to a Compose <c>KeyboardType</c> int.</summary>
    public static void MapKeyboard(EditorHandler handler, IEditor editor) =>
        handler._keyboardType.Value = KeyboardMapping.Resolve(editor.Keyboard, nameof(EditorHandler));

    /// <summary>Map <see cref="ITextInput.IsReadOnly"/> to the Compose <c>readOnly</c> slot.</summary>
    public static void MapIsReadOnly(EditorHandler handler, IEditor editor) =>
        handler._readOnly.Value = editor.IsReadOnly;

    /// <summary>Map <see cref="ITextInput.MaxLength"/> to the truncation cap (negative = unlimited).</summary>
    public static void MapMaxLength(EditorHandler handler, IEditor editor) =>
        handler._maxLength.Value = editor.MaxLength;

    /// <summary>
    /// Map <see cref="IView.HorizontalLayoutAlignment"/> to
    /// <c>Modifier.fillMaxWidth()</c> when the editor asks to fill its
    /// slot — same rationale as <see cref="EntryHandler"/>.
    /// </summary>
    public static void MapHorizontalLayoutAlignment(EditorHandler handler, IEditor editor) =>
        handler._fillWidth.Value = editor.HorizontalLayoutAlignment
            == Microsoft.Maui.Primitives.LayoutAlignment.Fill;
}
