using System.Diagnostics;
using AndroidX.Compose;
using AndroidX.Compose.UI.Platform;
using AndroidX.Compose.UI.Text.Input;
using Microsoft.Maui.Handlers;
using ComposeColor          = AndroidX.Compose.Color;
using ComposeFontWeight     = AndroidX.Compose.FontWeight;
using ComposeKeyboardType   = AndroidX.Compose.KeyboardType;
using ComposeOutlinedTextField = AndroidX.Compose.OutlinedTextField;
using ComposeText           = AndroidX.Compose.Text;
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
/// Uses <c>OutlinedTextField</c> rather than the filled <c>TextField</c>
/// variant because it matches MAUI's stock Entry chrome more closely
/// (clear bordered outline, no shaded background fill, label that
/// floats above the border when the field gains focus).
/// </remarks>
public partial class EntryHandler : ViewHandler<IEntry, ComposeView>
{
    /// <summary>
    /// Property mapper that forwards MAUI <see cref="IEntry"/> property
    /// changes to the Compose-backed <see cref="ComposeView"/>.
    /// </summary>
    public static IPropertyMapper<IEntry, EntryHandler> Mapper =
        new PropertyMapper<IEntry, EntryHandler>(ViewHandler.ViewMapper)
        {
            [nameof(IText.Text)]                      = MapText,
            [nameof(ITextStyle.TextColor)]            = MapTextColor,
            [nameof(ITextStyle.Font)]                 = MapFont,
            [nameof(IPlaceholder.Placeholder)]        = MapPlaceholder,
            [nameof(IEntry.IsPassword)]               = MapIsPassword,
            [nameof(ITextInput.Keyboard)]             = MapKeyboard,
            [nameof(ITextInput.IsReadOnly)]           = MapIsReadOnly,
            [nameof(IView.HorizontalLayoutAlignment)] = MapHorizontalLayoutAlignment,
        };

    /// <summary>Command mapper (inherits view-level commands; no extras).</summary>
    public static CommandMapper<IEntry, EntryHandler> CommandMapper =
        new(ViewCommandMapper);

    readonly MutableState<string> _text         = new(string.Empty);
    readonly MutableState<long?>  _color        = new((long?)null);
    readonly MutableState<int?>   _fontSize     = new((int?)null);
    readonly MutableState<bool>   _bold         = new(false);
    readonly MutableState<string> _placeholder  = new(string.Empty);
    readonly MutableState<bool>   _isPassword   = new(false);
    readonly MutableState<int>    _keyboardType = new(ComposeKeyboardType.Text);
    readonly MutableState<bool>   _readOnly     = new(false);
    readonly MutableState<bool>   _fillWidth    = new(false);

    /// <summary>Construct a handler with the default mappers.</summary>
    public EntryHandler() : base(Mapper, CommandMapper) { }

    /// <summary>Construct a handler with custom mappers.</summary>
    public EntryHandler(IPropertyMapper? mapper, CommandMapper? commandMapper = null)
        : base(mapper ?? Mapper, commandMapper ?? CommandMapper) { }

    /// <inheritdoc/>
    protected override ComposeView CreatePlatformView()
    {
        var view = new ComposeView(Context);
        view.SetContent(_ =>
        {
            var packed       = _color.Value;
            var size         = _fontSize.Value;
            var bold         = _bold.Value;
            var placeholder  = _placeholder.Value;
            var isPassword   = _isPassword.Value;
            var keyboardType = _keyboardType.Value;
            var readOnly     = _readOnly.Value;
            var fill         = _fillWidth.Value;

            var field = new ComposeOutlinedTextField(_text.Value, OnValueChanged)
            {
                ReadOnly   = readOnly,
                SingleLine = true,
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
            if (isPassword)
                field.VisualTransformation = new PasswordVisualTransformation('•');
            // Always set KeyboardOptions when the resolved type isn't the
            // default `Text`, so the OS surfaces the right IME (digits,
            // email, etc.). Password buffers + numeric flag both lower
            // through `keyboardType`.
            var resolvedType = isPassword ? ComposeKeyboardType.Password : keyboardType;
            if (resolvedType != ComposeKeyboardType.Text)
            {
                var d = KeyboardOptionsCompanion.Default;
                field.KeyboardOptions = d.Copy(
                    d.Capitalization, d.AutoCorrectEnabled,
                    resolvedType, d.ImeAction,
                    d.PlatformImeOptions, d.ShowKeyboardOnFocus, d.HintLocales);
            }

            if (fill)
                field.PrependModifier(Modifier.FillMaxWidth());
            return field;
        });
        return view;
    }

    /// <inheritdoc/>
    protected override void DisconnectHandler(ComposeView platformView)
    {
        platformView.DisposeComposition();
        base.DisconnectHandler(platformView);
    }

    void OnValueChanged(string newValue)
    {
        // Update Compose state synchronously so the rendered value stays
        // pinned to what the user just typed (Compose snaps `value`
        // back on the next recompose; lagging here drops keystrokes).
        // Updating VirtualView.Text after triggers MAUI's standard
        // property pipeline (data binding, behaviors, validation) which
        // re-enters MapText with the same string — that's a no-op on
        // MutableState<string>, so no feedback loop.
        _text.Value = newValue;
        if (VirtualView is { } entry)
            entry.Text = newValue;
    }

    /// <summary>Map <see cref="IText.Text"/> to the Compose value slot.</summary>
    public static void MapText(EntryHandler handler, IEntry entry) =>
        handler._text.Value = entry.Text ?? string.Empty;

    /// <summary>Map <see cref="ITextStyle.TextColor"/> to the Compose <c>TextStyle.Color</c> slot.</summary>
    public static void MapTextColor(EntryHandler handler, IEntry entry) =>
        handler._color.Value = ColorMapping.ToPackedLong(entry.TextColor);

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

    /// <summary>Map <see cref="IEntry.IsPassword"/> to the visualTransformation + keyboardType slots.</summary>
    public static void MapIsPassword(EntryHandler handler, IEntry entry) =>
        handler._isPassword.Value = entry.IsPassword;

    /// <summary>Map <see cref="ITextInput.Keyboard"/> to a Compose <c>KeyboardType</c> int.</summary>
    public static void MapKeyboard(EntryHandler handler, IEntry entry) =>
        handler._keyboardType.Value = ResolveKeyboardType(entry.Keyboard);

    /// <summary>Map <see cref="ITextInput.IsReadOnly"/> to the Compose <c>readOnly</c> slot.</summary>
    public static void MapIsReadOnly(EntryHandler handler, IEntry entry) =>
        handler._readOnly.Value = entry.IsReadOnly;

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
    /// Resolve a MAUI <see cref="Keyboard"/> to its Compose
    /// <see cref="ComposeKeyboardType"/> int. Compose's
    /// <c>@JvmInline value class KeyboardType(Int)</c> lowers each
    /// constant to a stable sequential int; the named properties on
    /// <see cref="ComposeKeyboardType"/> read the same Companion
    /// getters the Kotlin compiler emits.
    /// </summary>
    static int ResolveKeyboardType(Keyboard? keyboard)
    {
        if (keyboard is null) return ComposeKeyboardType.Text;
        if (keyboard == Keyboard.Numeric)   return ComposeKeyboardType.Number;
        if (keyboard == Keyboard.Telephone) return ComposeKeyboardType.Phone;
        if (keyboard == Keyboard.Url)       return ComposeKeyboardType.Uri;
        if (keyboard == Keyboard.Email)     return ComposeKeyboardType.Email;
        if (keyboard == Keyboard.Default
            || keyboard == Keyboard.Text
            || keyboard == Keyboard.Chat) return ComposeKeyboardType.Text;
        // CustomKeyboard / Plain / unknown — fall through to text so the
        // user still gets a working IME instead of a blank surface.
        Debug.WriteLine(
            $"[EntryHandler] Unmapped Keyboard '{keyboard}'; falling back to Text.");
        return ComposeKeyboardType.Text;
    }
}
