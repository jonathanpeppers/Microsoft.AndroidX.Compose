using Android.Runtime;
using AndroidX.Compose.Runtime;
using AndroidX.Compose.UI.Text.Input;

namespace AndroidX.Compose;

/// <summary>
/// Material 3 <c>TextField</c> (filled variant). Three construction
/// modes:
/// <list type="bullet">
/// <item>Plain <c>value</c> + <c>onValueChange</c> pair — string-overload
/// of Kotlin's <c>TextField</c>.</item>
/// <item><see cref="MutableState{T}"/> of <see cref="string"/> — convenience
/// that wires a <see cref="MutableState{T}"/> directly so user edits
/// trigger recomposition.</item>
/// <item><see cref="MutableState{T}"/> of <see cref="TextFieldValue"/> —
/// uses Kotlin's <c>TextField(TextFieldValue, …)</c> overload so callers
/// can place the caret explicitly after programmatic edits (e.g. append
/// an emoji and move the cursor to the end of the buffer). Build values
/// via <see cref="ComposeExtensions.NewTextFieldValue(string, long, AndroidX.Compose.UI.Text.TextRange?)"/>
/// and <c>TextFieldValue.Copy(...)</c>. See issue #204.</item>
/// </list>
/// </summary>
/// <remarks>
/// Hand-written rather than <c>[ComposeFacade]</c>-generated because the
/// facade routes between two distinct <c>[ComposeBridge]</c> methods
/// (<c>TextField</c> string overload vs <c>TextFieldWithValue</c>) based
/// on which ctor was used. The current generator's <c>BranchOn</c> mode
/// only models slot-presence branching, not ctor-driven bridge
/// selection. <see cref="Icon"/> is the existing precedent for the same
/// pattern.
/// </remarks>
public sealed class TextField : ComposableNode
{
    readonly string?                       _value;
    readonly Action<string>?               _onValueChange;
    readonly MutableState<TextFieldValue>? _tfvState;
    readonly bool                          _enabled;
    readonly bool                          _readOnly;
    readonly bool                          _isError;
    readonly bool                          _singleLine;
    readonly int                           _maxLines;
    readonly int                           _minLines;

    /// <summary>Floating label (e.g. <c>new Text("Email")</c>).</summary>
    public ComposableNode? Label          { get; set; }
    /// <summary>Hint shown while the field is empty.</summary>
    public ComposableNode? Placeholder    { get; set; }
    /// <summary>Icon rendered before the value text.</summary>
    public ComposableNode? LeadingIcon    { get; set; }
    /// <summary>Icon rendered after the value text.</summary>
    public ComposableNode? TrailingIcon   { get; set; }
    /// <summary>Static prefix rendered before the value text.</summary>
    public ComposableNode? Prefix         { get; set; }
    /// <summary>Static suffix rendered after the value text.</summary>
    public ComposableNode? Suffix         { get; set; }
    /// <summary>Helper text rendered below the field.</summary>
    public ComposableNode? SupportingText { get; set; }
    /// <summary>Optional shape applied to the field's container (Kotlin <c>shape</c>).</summary>
    public Shape?          Shape          { get; set; }
    /// <summary>Optional <c>TextStyle</c> override (Kotlin <c>textStyle</c>) — controls text color, size, weight, etc.</summary>
    public TextStyle? TextStyle { get; set; }
    /// <summary>Optional visual transformation (Kotlin <c>visualTransformation</c>) — use <c>PasswordVisualTransformation</c> for password fields.</summary>
    public AndroidX.Compose.UI.Text.Input.IVisualTransformation? VisualTransformation { get; set; }
    /// <summary>Optional keyboard options (Kotlin <c>keyboardOptions</c>) — controls IME type, capitalization, autocorrect.</summary>
    public AndroidX.Compose.Foundation.Text.KeyboardOptions? KeyboardOptions { get; set; }

    /// <summary>String-overload ctor — pass the current value and a callback.</summary>
    public TextField(
        string value,
        Action<string> onValueChange,
        bool enabled = true,
        bool readOnly = false,
        bool isError = false,
        bool singleLine = false,
        int maxLines = int.MaxValue,
        int minLines = 1)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(onValueChange);
        _value = value;
        _onValueChange = onValueChange;
        _enabled = enabled;
        _readOnly = readOnly;
        _isError = isError;
        _singleLine = singleLine;
        _maxLines = maxLines;
        _minLines = minLines;
    }

    /// <summary>
    /// Bind this <c>TextField</c> to a <see cref="MutableState{T}"/> of
    /// <see cref="string"/> so user edits trigger recomposition automatically.
    /// </summary>
    public TextField(
        MutableState<string> state,
        bool enabled = true,
        bool readOnly = false,
        bool isError = false,
        bool singleLine = false,
        int maxLines = int.MaxValue,
        int minLines = 1)
        : this(
            CurrentValue(state),
            v => state.Value = v,
            enabled,
            readOnly,
            isError,
            singleLine,
            maxLines,
            minLines)
    {
    }

    /// <summary>
    /// Bind this <c>TextField</c> to a <see cref="MutableState{T}"/> of
    /// <see cref="TextFieldValue"/>. Uses Kotlin's
    /// <c>TextField(TextFieldValue, (TextFieldValue) -&gt; Unit, …)</c>
    /// overload so caller-supplied selection state is honoured (e.g. for
    /// "place caret at end after inserting" behaviour).
    /// </summary>
    public TextField(
        MutableState<TextFieldValue> state,
        bool enabled = true,
        bool readOnly = false,
        bool isError = false,
        bool singleLine = false,
        int maxLines = int.MaxValue,
        int minLines = 1)
    {
        ArgumentNullException.ThrowIfNull(state);
        _tfvState = state;
        _enabled = enabled;
        _readOnly = readOnly;
        _isError = isError;
        _singleLine = singleLine;
        _maxLines = maxLines;
        _minLines = minLines;
    }

    static string CurrentValue(MutableState<string> state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.Value ?? string.Empty;
    }

    /// <inheritdoc/>
    public override void Render(IComposer composer)
    {
        if (_tfvState is not null)
            RenderWithSelection(composer);
        else
            RenderString(composer);
    }

    void RenderString(IComposer composer)
    {
        var value = _value
            ?? throw new InvalidOperationException($"{nameof(TextField)} string value was not initialized.");
        var onValueChange = _onValueChange
            ?? throw new InvalidOperationException($"{nameof(TextField)} value-change callback was not initialized.");
        var __onValueChange = composer.RememberAction((Java.Lang.Object? v) =>
            onValueChange(v?.ToString() ?? string.Empty));
        var __label          = Label          is null ? null : ComposableLambdas.Wrap2(composer, c => Label.Render(c));
        var __placeholder    = Placeholder    is null ? null : ComposableLambdas.Wrap2(composer, c => Placeholder.Render(c));
        var __leadingIcon    = LeadingIcon    is null ? null : ComposableLambdas.Wrap2(composer, c => LeadingIcon.Render(c));
        var __trailingIcon   = TrailingIcon   is null ? null : ComposableLambdas.Wrap2(composer, c => TrailingIcon.Render(c));
        var __prefix         = Prefix         is null ? null : ComposableLambdas.Wrap2(composer, c => Prefix.Render(c));
        var __suffix         = Suffix         is null ? null : ComposableLambdas.Wrap2(composer, c => Suffix.Render(c));
        var __supportingText = SupportingText is null ? null : ComposableLambdas.Wrap2(composer, c => SupportingText.Render(c));
        // Snapshot the modifier structural key before BuildModifier
        // consumes _prepended/_appended/_contentPadding.
        var __modifierKey = BuildModifierStructuralKey();
        // First 10 user params (Kotlin packs 10 per $changed int).
        // Params beyond bit 28 land in the next $changed int which we
        // leave at 0 (Uncertain) — same as ComposeFacadeGenerator.
        int __changed = 0;
        __changed |= composer.DiffSlot(value, ComposeExtensions.DiffSlotShift(0));
        __changed |= (int)ChangedBits.Static << ComposeExtensions.DiffSlotShift(1);
        __changed |= composer.DiffSlot(__modifierKey, ComposeExtensions.DiffSlotShift(2));
        __changed |= composer.DiffSlot(_enabled, ComposeExtensions.DiffSlotShift(3));
        __changed |= composer.DiffSlot(_readOnly, ComposeExtensions.DiffSlotShift(4));
        __changed |= composer.DiffSlot<object?>(TextStyle, ComposeExtensions.DiffSlotShift(5));
        __changed |= composer.DiffSlot<object?>(__label, ComposeExtensions.DiffSlotShift(6));
        __changed |= composer.DiffSlot<object?>(__placeholder, ComposeExtensions.DiffSlotShift(7));
        __changed |= composer.DiffSlot<object?>(__leadingIcon, ComposeExtensions.DiffSlotShift(8));
        __changed |= composer.DiffSlot<object?>(__trailingIcon, ComposeExtensions.DiffSlotShift(9));
        ComposeBridges.TextField(value, __onValueChange, BuildModifier(),
            _enabled, _readOnly, TextStyle?.Build(), __label, __placeholder, __leadingIcon, __trailingIcon,
            __prefix, __suffix, __supportingText, _isError,
            VisualTransformation, KeyboardOptions,
            _singleLine, _maxLines, _minLines,
            Shape,
            composer, _changed: __changed);
    }

    void RenderWithSelection(IComposer composer)
    {
        var state = _tfvState
            ?? throw new InvalidOperationException($"{nameof(TextField)} text-field-value state was not initialized.");
        var current = state.Value
            ?? throw new InvalidOperationException(
                $"{nameof(TextField)}: MutableState<TextFieldValue>.Value is null. " +
                $"Seed with {nameof(Compose)}.{nameof(ComposeExtensions.NewTextFieldValue)}() before first render.");

        var __onValueChange = composer.RememberAction((Java.Lang.Object? v) =>
        {
            var peer = v
                ?? throw new InvalidOperationException($"{nameof(TextField)} received a null TextFieldValue peer.");
            state.Value = Java.Lang.Object.GetObject<TextFieldValue>(
                peer.Handle, JniHandleOwnership.DoNotTransfer)
                ?? throw new InvalidOperationException($"{nameof(TextField)} could not resolve the TextFieldValue peer.");
        });
        var __label          = Label          is null ? null : ComposableLambdas.Wrap2(composer, c => Label.Render(c));
        var __placeholder    = Placeholder    is null ? null : ComposableLambdas.Wrap2(composer, c => Placeholder.Render(c));
        var __leadingIcon    = LeadingIcon    is null ? null : ComposableLambdas.Wrap2(composer, c => LeadingIcon.Render(c));
        var __trailingIcon   = TrailingIcon   is null ? null : ComposableLambdas.Wrap2(composer, c => TrailingIcon.Render(c));
        var __prefix         = Prefix         is null ? null : ComposableLambdas.Wrap2(composer, c => Prefix.Render(c));
        var __suffix         = Suffix         is null ? null : ComposableLambdas.Wrap2(composer, c => Suffix.Render(c));
        var __supportingText = SupportingText is null ? null : ComposableLambdas.Wrap2(composer, c => SupportingText.Render(c));
        var __modifierKey = BuildModifierStructuralKey();
        int __changed = 0;
        // value bit reuses the TextFieldValue peer's identity. Its
        // .Handle stays the same as long as the state holder hasn't
        // been replaced; user keystrokes allocate a fresh peer so
        // typing reads as Different — exactly what we want.
        __changed |= composer.DiffSlot<object?>(current, ComposeExtensions.DiffSlotShift(0));
        __changed |= (int)ChangedBits.Static << ComposeExtensions.DiffSlotShift(1);
        __changed |= composer.DiffSlot(__modifierKey, ComposeExtensions.DiffSlotShift(2));
        __changed |= composer.DiffSlot(_enabled, ComposeExtensions.DiffSlotShift(3));
        __changed |= composer.DiffSlot(_readOnly, ComposeExtensions.DiffSlotShift(4));
        __changed |= composer.DiffSlot<object?>(TextStyle, ComposeExtensions.DiffSlotShift(5));
        __changed |= composer.DiffSlot<object?>(__label, ComposeExtensions.DiffSlotShift(6));
        __changed |= composer.DiffSlot<object?>(__placeholder, ComposeExtensions.DiffSlotShift(7));
        __changed |= composer.DiffSlot<object?>(__leadingIcon, ComposeExtensions.DiffSlotShift(8));
        __changed |= composer.DiffSlot<object?>(__trailingIcon, ComposeExtensions.DiffSlotShift(9));
        ComposeBridges.TextFieldWithValue(current, __onValueChange, BuildModifier(),
            _enabled, _readOnly, TextStyle?.Build(), __label, __placeholder, __leadingIcon, __trailingIcon,
            __prefix, __suffix, __supportingText, _isError,
            VisualTransformation, KeyboardOptions,
            _singleLine, _maxLines, _minLines,
            Shape,
            composer, _changed: __changed);
    }
}
