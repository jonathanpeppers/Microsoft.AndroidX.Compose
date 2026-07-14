using Android.Runtime;
using AndroidX.Compose.Runtime;
using AndroidX.Compose.UI.Text.Input;

namespace AndroidX.Compose;

/// <summary>
/// Material 3 <c>OutlinedTextField</c>. Same construction modes and
/// slot properties as <see cref="TextField"/>; differs only in visual
/// treatment (outline border instead of filled background).
/// </summary>
/// <remarks>
/// Hand-written for the same reason as <see cref="TextField"/> — see
/// its <c>&lt;remarks&gt;</c> for the ctor-driven bridge-selection
/// rationale.
/// </remarks>
public sealed class OutlinedTextField : ComposableNode
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
    /// <summary>Optional shape applied to the field's outlined container (Kotlin <c>shape</c>).</summary>
    public Shape?          Shape          { get; set; }
    /// <summary>Optional <c>TextStyle</c> override (Kotlin <c>textStyle</c>) — controls text color, size, weight, etc.</summary>
    public TextStyle? TextStyle { get; set; }
    /// <summary>Optional visual transformation (Kotlin <c>visualTransformation</c>) — use <c>PasswordVisualTransformation</c> for password fields.</summary>
    public AndroidX.Compose.UI.Text.Input.IVisualTransformation? VisualTransformation { get; set; }
    /// <summary>Optional keyboard options (Kotlin <c>keyboardOptions</c>) — controls IME type, capitalization, autocorrect.</summary>
    public AndroidX.Compose.Foundation.Text.KeyboardOptions? KeyboardOptions { get; set; }
    /// <summary>Optional keyboard-action callbacks (Kotlin <c>keyboardActions</c>) — fires <c>onSearch</c>/<c>onDone</c>/<c>onSend</c>/etc. when the user taps the IME action key.</summary>
    public AndroidX.Compose.Foundation.Text.KeyboardActions? KeyboardActions { get; set; }

    /// <summary>String-overload ctor — pass the current value and a callback.</summary>
    public OutlinedTextField(
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
    /// Bind this <c>OutlinedTextField</c> to a <see cref="MutableState{T}"/>
    /// of <see cref="string"/> so user edits trigger recomposition automatically.
    /// </summary>
    public OutlinedTextField(
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
    /// Bind this <c>OutlinedTextField</c> to a <see cref="MutableState{T}"/>
    /// of <see cref="TextFieldValue"/>. Uses Kotlin's
    /// <c>OutlinedTextField(TextFieldValue, (TextFieldValue) -&gt; Unit, …)</c>
    /// overload so caller-supplied selection state is honoured.
    /// </summary>
    public OutlinedTextField(
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
            ?? throw new InvalidOperationException($"{nameof(OutlinedTextField)} string value was not initialized.");
        var onValueChange = _onValueChange
            ?? throw new InvalidOperationException($"{nameof(OutlinedTextField)} value-change callback was not initialized.");
        var __onValueChange = composer.RememberAction((Java.Lang.Object? v) =>
            onValueChange(v?.ToString() ?? string.Empty));
        var __label          = Label          is null ? null : ComposableLambdas.Wrap2(composer, c => Label.Render(c));
        var __placeholder    = Placeholder    is null ? null : ComposableLambdas.Wrap2(composer, c => Placeholder.Render(c));
        var __leadingIcon    = LeadingIcon    is null ? null : ComposableLambdas.Wrap2(composer, c => LeadingIcon.Render(c));
        var __trailingIcon   = TrailingIcon   is null ? null : ComposableLambdas.Wrap2(composer, c => TrailingIcon.Render(c));
        var __prefix         = Prefix         is null ? null : ComposableLambdas.Wrap2(composer, c => Prefix.Render(c));
        var __suffix         = Suffix         is null ? null : ComposableLambdas.Wrap2(composer, c => Suffix.Render(c));
        var __supportingText = SupportingText is null ? null : ComposableLambdas.Wrap2(composer, c => SupportingText.Render(c));
        var __modifierKey = BuildModifierStructuralKey();
        // First 10 user params (Kotlin packs 10 per $changed int).
        // Remaining 12 params overflow to a second $changed int we
        // don't model — left at 0 (Uncertain), same as the generator.
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
        ComposeBridges.OutlinedTextField(value, __onValueChange, BuildModifier(),
            _enabled, _readOnly, TextStyle?.Build(), __label, __placeholder, __leadingIcon, __trailingIcon,
            __prefix, __suffix, __supportingText, _isError,
            VisualTransformation, KeyboardOptions, KeyboardActions,
            _singleLine, _maxLines, _minLines,
            Shape,
            composer, _changed: __changed);
    }

    void RenderWithSelection(IComposer composer)
    {
        var state = _tfvState
            ?? throw new InvalidOperationException($"{nameof(OutlinedTextField)} text-field-value state was not initialized.");
        var current = state.Value
            ?? throw new InvalidOperationException(
                $"{nameof(OutlinedTextField)}: MutableState<TextFieldValue>.Value is null. " +
                $"Seed with {nameof(Compose)}.{nameof(ComposeExtensions.NewTextFieldValue)}() before first render.");

        var __onValueChange = composer.RememberAction((Java.Lang.Object? v) =>
        {
            var peer = v
                ?? throw new InvalidOperationException($"{nameof(OutlinedTextField)} received a null TextFieldValue peer.");
            state.Value = Java.Lang.Object.GetObject<TextFieldValue>(
                peer.Handle, JniHandleOwnership.DoNotTransfer)
                ?? throw new InvalidOperationException($"{nameof(OutlinedTextField)} could not resolve the TextFieldValue peer.");
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
        ComposeBridges.OutlinedTextFieldWithValue(current, __onValueChange, BuildModifier(),
            _enabled, _readOnly, TextStyle?.Build(), __label, __placeholder, __leadingIcon, __trailingIcon,
            __prefix, __suffix, __supportingText, _isError,
            VisualTransformation, KeyboardOptions, KeyboardActions,
            _singleLine, _maxLines, _minLines,
            Shape,
            composer, _changed: __changed);
    }
}
