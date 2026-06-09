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
    /// <summary>Whether the field accepts focus and input.</summary>
    public bool?           Enabled        { get; set; }
    /// <summary>Whether the field is read-only.</summary>
    public bool?           ReadOnly       { get; set; }
    /// <summary>Whether the field is in an error state.</summary>
    public bool?           IsError        { get; set; }
    /// <summary>Whether the field is constrained to one line.</summary>
    public bool?           SingleLine     { get; set; }
    /// <summary>Maximum number of visible lines.</summary>
    public int?            MaxLines       { get; set; }
    /// <summary>Minimum number of visible lines.</summary>
    public int?            MinLines       { get; set; }

    /// <summary>String-overload ctor — pass the current value and a callback.</summary>
    public OutlinedTextField(string value, Action<string> onValueChange)
    {
        _value = value;
        _onValueChange = onValueChange;
    }

    /// <summary>
    /// Bind this <c>OutlinedTextField</c> to a <see cref="MutableState{T}"/>
    /// of <see cref="string"/> so user edits trigger recomposition automatically.
    /// </summary>
    public OutlinedTextField(MutableState<string> state)
        : this(state.Value ?? string.Empty, v => state.Value = v)
    {
    }

    /// <summary>
    /// Bind this <c>OutlinedTextField</c> to a <see cref="MutableState{T}"/>
    /// of <see cref="TextFieldValue"/>. Uses Kotlin's
    /// <c>OutlinedTextField(TextFieldValue, (TextFieldValue) -&gt; Unit, …)</c>
    /// overload so caller-supplied selection state is honoured.
    /// </summary>
    public OutlinedTextField(MutableState<TextFieldValue> state)
    {
        ArgumentNullException.ThrowIfNull(state);
        _tfvState = state;
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
        var __onValueChange = new ComposableLambda1(v => _onValueChange!(v?.ToString() ?? string.Empty));
        var __label          = Label          is null ? null : ComposableLambdas.Wrap2(composer, c => Label.Render(c));
        var __placeholder    = Placeholder    is null ? null : ComposableLambdas.Wrap2(composer, c => Placeholder.Render(c));
        var __leadingIcon    = LeadingIcon    is null ? null : ComposableLambdas.Wrap2(composer, c => LeadingIcon.Render(c));
        var __trailingIcon   = TrailingIcon   is null ? null : ComposableLambdas.Wrap2(composer, c => TrailingIcon.Render(c));
        var __prefix         = Prefix         is null ? null : ComposableLambdas.Wrap2(composer, c => Prefix.Render(c));
        var __suffix         = Suffix         is null ? null : ComposableLambdas.Wrap2(composer, c => Suffix.Render(c));
        var __supportingText = SupportingText is null ? null : ComposableLambdas.Wrap2(composer, c => SupportingText.Render(c));
        ComposeBridges.OutlinedTextField(_value!, __onValueChange, BuildModifier(),
            Enabled, ReadOnly, __label, __placeholder, __leadingIcon, __trailingIcon,
            __prefix, __suffix, __supportingText, IsError, SingleLine, MaxLines, MinLines,
            composer);
    }

    void RenderWithSelection(IComposer composer)
    {
        var state = _tfvState!;
        var current = state.Value
            ?? throw new InvalidOperationException(
                $"{nameof(OutlinedTextField)}: MutableState<TextFieldValue>.Value is null. " +
                $"Seed with {nameof(Compose)}.{nameof(ComposeExtensions.NewTextFieldValue)}() before first render.");

        var __onValueChange = new ComposableLambda1(v =>
        {
            state.Value = Java.Lang.Object.GetObject<TextFieldValue>(
                v!.Handle, JniHandleOwnership.DoNotTransfer)!;
        });
        var __label          = Label          is null ? null : ComposableLambdas.Wrap2(composer, c => Label.Render(c));
        var __placeholder    = Placeholder    is null ? null : ComposableLambdas.Wrap2(composer, c => Placeholder.Render(c));
        var __leadingIcon    = LeadingIcon    is null ? null : ComposableLambdas.Wrap2(composer, c => LeadingIcon.Render(c));
        var __trailingIcon   = TrailingIcon   is null ? null : ComposableLambdas.Wrap2(composer, c => TrailingIcon.Render(c));
        var __prefix         = Prefix         is null ? null : ComposableLambdas.Wrap2(composer, c => Prefix.Render(c));
        var __suffix         = Suffix         is null ? null : ComposableLambdas.Wrap2(composer, c => Suffix.Render(c));
        var __supportingText = SupportingText is null ? null : ComposableLambdas.Wrap2(composer, c => SupportingText.Render(c));
        ComposeBridges.OutlinedTextFieldWithValue(current, __onValueChange, BuildModifier(),
            Enabled, ReadOnly, __label, __placeholder, __leadingIcon, __trailingIcon,
            __prefix, __suffix, __supportingText, IsError, SingleLine, MaxLines, MinLines,
            composer);
    }
}
