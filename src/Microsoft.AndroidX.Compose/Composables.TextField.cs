using AndroidX.Compose.Foundation.Text;
using AndroidX.Compose.Runtime;
using AndroidX.Compose.UI.Text.Input;

namespace AndroidX.Compose;

public static partial class Composables
{
    /// <summary>Renders a filled string text field with an explicit composer.</summary>
    [Composable, GenerateImplicitComposable]
    public static void TextField(
        IComposer composer,
        string value,
        Action<string> onValueChange,
        Modifier? modifier = null,
        bool enabled = true,
        bool readOnly = false,
        bool isError = false,
        bool singleLine = false,
        int maxLines = int.MaxValue,
        int minLines = 1,
        [ComposableContent] Action<IComposer>? label = null,
        [ComposableContent] Action<IComposer>? placeholder = null,
        [ComposableContent] Action<IComposer>? leadingIcon = null,
        [ComposableContent] Action<IComposer>? trailingIcon = null,
        [ComposableContent] Action<IComposer>? prefix = null,
        [ComposableContent] Action<IComposer>? suffix = null,
        [ComposableContent] Action<IComposer>? supportingText = null,
        Shape? shape = null,
        TextStyle? textStyle = null,
        IVisualTransformation? visualTransformation = null,
        KeyboardOptions? keyboardOptions = null)
    {
        ArgumentNullException.ThrowIfNull(composer);
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(onValueChange);

        RenderTextField(
            composer,
            new global::AndroidX.Compose.TextField(
                value,
                onValueChange,
                enabled,
                readOnly,
                isError,
                singleLine,
                maxLines,
                minLines),
            modifier,
            label,
            placeholder,
            leadingIcon,
            trailingIcon,
            prefix,
            suffix,
            supportingText,
            shape,
            textStyle,
            visualTransformation,
            keyboardOptions);
    }

    /// <summary>Renders a filled state-backed string text field with an explicit composer.</summary>
    [Composable, GenerateImplicitComposable]
    public static void TextField(
        IComposer composer,
        MutableState<string> state,
        Modifier? modifier = null,
        bool enabled = true,
        bool readOnly = false,
        bool isError = false,
        bool singleLine = false,
        int maxLines = int.MaxValue,
        int minLines = 1,
        [ComposableContent] Action<IComposer>? label = null,
        [ComposableContent] Action<IComposer>? placeholder = null,
        [ComposableContent] Action<IComposer>? leadingIcon = null,
        [ComposableContent] Action<IComposer>? trailingIcon = null,
        [ComposableContent] Action<IComposer>? prefix = null,
        [ComposableContent] Action<IComposer>? suffix = null,
        [ComposableContent] Action<IComposer>? supportingText = null,
        Shape? shape = null,
        TextStyle? textStyle = null,
        IVisualTransformation? visualTransformation = null,
        KeyboardOptions? keyboardOptions = null)
    {
        ArgumentNullException.ThrowIfNull(composer);
        ArgumentNullException.ThrowIfNull(state);

        RenderTextField(
            composer,
            new global::AndroidX.Compose.TextField(
                state,
                enabled,
                readOnly,
                isError,
                singleLine,
                maxLines,
                minLines),
            modifier,
            label,
            placeholder,
            leadingIcon,
            trailingIcon,
            prefix,
            suffix,
            supportingText,
            shape,
            textStyle,
            visualTransformation,
            keyboardOptions);
    }

    /// <summary>Renders a filled selection-aware text field with an explicit composer.</summary>
    [Composable, GenerateImplicitComposable]
    public static void TextField(
        IComposer composer,
        MutableState<TextFieldValue> state,
        Modifier? modifier = null,
        bool enabled = true,
        bool readOnly = false,
        bool isError = false,
        bool singleLine = false,
        int maxLines = int.MaxValue,
        int minLines = 1,
        [ComposableContent] Action<IComposer>? label = null,
        [ComposableContent] Action<IComposer>? placeholder = null,
        [ComposableContent] Action<IComposer>? leadingIcon = null,
        [ComposableContent] Action<IComposer>? trailingIcon = null,
        [ComposableContent] Action<IComposer>? prefix = null,
        [ComposableContent] Action<IComposer>? suffix = null,
        [ComposableContent] Action<IComposer>? supportingText = null,
        Shape? shape = null,
        TextStyle? textStyle = null,
        IVisualTransformation? visualTransformation = null,
        KeyboardOptions? keyboardOptions = null)
    {
        ArgumentNullException.ThrowIfNull(composer);
        ArgumentNullException.ThrowIfNull(state);

        RenderTextField(
            composer,
            new global::AndroidX.Compose.TextField(
                state,
                enabled,
                readOnly,
                isError,
                singleLine,
                maxLines,
                minLines),
            modifier,
            label,
            placeholder,
            leadingIcon,
            trailingIcon,
            prefix,
            suffix,
            supportingText,
            shape,
            textStyle,
            visualTransformation,
            keyboardOptions);
    }

    /// <summary>Renders an outlined string text field with an explicit composer.</summary>
    [Composable, GenerateImplicitComposable]
    public static void OutlinedTextField(
        IComposer composer,
        string value,
        Action<string> onValueChange,
        Modifier? modifier = null,
        bool enabled = true,
        bool readOnly = false,
        bool isError = false,
        bool singleLine = false,
        int maxLines = int.MaxValue,
        int minLines = 1,
        [ComposableContent] Action<IComposer>? label = null,
        [ComposableContent] Action<IComposer>? placeholder = null,
        [ComposableContent] Action<IComposer>? leadingIcon = null,
        [ComposableContent] Action<IComposer>? trailingIcon = null,
        [ComposableContent] Action<IComposer>? prefix = null,
        [ComposableContent] Action<IComposer>? suffix = null,
        [ComposableContent] Action<IComposer>? supportingText = null,
        Shape? shape = null,
        TextStyle? textStyle = null,
        IVisualTransformation? visualTransformation = null,
        KeyboardOptions? keyboardOptions = null,
        KeyboardActions? keyboardActions = null)
    {
        ArgumentNullException.ThrowIfNull(composer);
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(onValueChange);

        RenderOutlinedTextField(
            composer,
            new global::AndroidX.Compose.OutlinedTextField(
                value,
                onValueChange,
                enabled,
                readOnly,
                isError,
                singleLine,
                maxLines,
                minLines),
            modifier,
            label,
            placeholder,
            leadingIcon,
            trailingIcon,
            prefix,
            suffix,
            supportingText,
            shape,
            textStyle,
            visualTransformation,
            keyboardOptions,
            keyboardActions);
    }

    /// <summary>Renders an outlined state-backed string text field with an explicit composer.</summary>
    [Composable, GenerateImplicitComposable]
    public static void OutlinedTextField(
        IComposer composer,
        MutableState<string> state,
        Modifier? modifier = null,
        bool enabled = true,
        bool readOnly = false,
        bool isError = false,
        bool singleLine = false,
        int maxLines = int.MaxValue,
        int minLines = 1,
        [ComposableContent] Action<IComposer>? label = null,
        [ComposableContent] Action<IComposer>? placeholder = null,
        [ComposableContent] Action<IComposer>? leadingIcon = null,
        [ComposableContent] Action<IComposer>? trailingIcon = null,
        [ComposableContent] Action<IComposer>? prefix = null,
        [ComposableContent] Action<IComposer>? suffix = null,
        [ComposableContent] Action<IComposer>? supportingText = null,
        Shape? shape = null,
        TextStyle? textStyle = null,
        IVisualTransformation? visualTransformation = null,
        KeyboardOptions? keyboardOptions = null,
        KeyboardActions? keyboardActions = null)
    {
        ArgumentNullException.ThrowIfNull(composer);
        ArgumentNullException.ThrowIfNull(state);

        RenderOutlinedTextField(
            composer,
            new global::AndroidX.Compose.OutlinedTextField(
                state,
                enabled,
                readOnly,
                isError,
                singleLine,
                maxLines,
                minLines),
            modifier,
            label,
            placeholder,
            leadingIcon,
            trailingIcon,
            prefix,
            suffix,
            supportingText,
            shape,
            textStyle,
            visualTransformation,
            keyboardOptions,
            keyboardActions);
    }

    /// <summary>Renders an outlined selection-aware text field with an explicit composer.</summary>
    [Composable, GenerateImplicitComposable]
    public static void OutlinedTextField(
        IComposer composer,
        MutableState<TextFieldValue> state,
        Modifier? modifier = null,
        bool enabled = true,
        bool readOnly = false,
        bool isError = false,
        bool singleLine = false,
        int maxLines = int.MaxValue,
        int minLines = 1,
        [ComposableContent] Action<IComposer>? label = null,
        [ComposableContent] Action<IComposer>? placeholder = null,
        [ComposableContent] Action<IComposer>? leadingIcon = null,
        [ComposableContent] Action<IComposer>? trailingIcon = null,
        [ComposableContent] Action<IComposer>? prefix = null,
        [ComposableContent] Action<IComposer>? suffix = null,
        [ComposableContent] Action<IComposer>? supportingText = null,
        Shape? shape = null,
        TextStyle? textStyle = null,
        IVisualTransformation? visualTransformation = null,
        KeyboardOptions? keyboardOptions = null,
        KeyboardActions? keyboardActions = null)
    {
        ArgumentNullException.ThrowIfNull(composer);
        ArgumentNullException.ThrowIfNull(state);

        RenderOutlinedTextField(
            composer,
            new global::AndroidX.Compose.OutlinedTextField(
                state,
                enabled,
                readOnly,
                isError,
                singleLine,
                maxLines,
                minLines),
            modifier,
            label,
            placeholder,
            leadingIcon,
            trailingIcon,
            prefix,
            suffix,
            supportingText,
            shape,
            textStyle,
            visualTransformation,
            keyboardOptions,
            keyboardActions);
    }

    static void RenderTextField(
        IComposer composer,
        global::AndroidX.Compose.TextField field,
        Modifier? modifier,
        Action<IComposer>? label,
        Action<IComposer>? placeholder,
        Action<IComposer>? leadingIcon,
        Action<IComposer>? trailingIcon,
        Action<IComposer>? prefix,
        Action<IComposer>? suffix,
        Action<IComposer>? supportingText,
        Shape? shape,
        TextStyle? textStyle,
        IVisualTransformation? visualTransformation,
        KeyboardOptions? keyboardOptions)
    {
        field.Modifier = modifier;
        field.Label = ComposableContentNode.Create(label);
        field.Placeholder = ComposableContentNode.Create(placeholder);
        field.LeadingIcon = ComposableContentNode.Create(leadingIcon);
        field.TrailingIcon = ComposableContentNode.Create(trailingIcon);
        field.Prefix = ComposableContentNode.Create(prefix);
        field.Suffix = ComposableContentNode.Create(suffix);
        field.SupportingText = ComposableContentNode.Create(supportingText);
        field.Shape = shape;
        field.TextStyle = textStyle;
        field.VisualTransformation = visualTransformation;
        field.KeyboardOptions = keyboardOptions;
        field.Render(composer);
    }

    static void RenderOutlinedTextField(
        IComposer composer,
        global::AndroidX.Compose.OutlinedTextField field,
        Modifier? modifier,
        Action<IComposer>? label,
        Action<IComposer>? placeholder,
        Action<IComposer>? leadingIcon,
        Action<IComposer>? trailingIcon,
        Action<IComposer>? prefix,
        Action<IComposer>? suffix,
        Action<IComposer>? supportingText,
        Shape? shape,
        TextStyle? textStyle,
        IVisualTransformation? visualTransformation,
        KeyboardOptions? keyboardOptions,
        KeyboardActions? keyboardActions)
    {
        field.Modifier = modifier;
        field.Label = ComposableContentNode.Create(label);
        field.Placeholder = ComposableContentNode.Create(placeholder);
        field.LeadingIcon = ComposableContentNode.Create(leadingIcon);
        field.TrailingIcon = ComposableContentNode.Create(trailingIcon);
        field.Prefix = ComposableContentNode.Create(prefix);
        field.Suffix = ComposableContentNode.Create(suffix);
        field.SupportingText = ComposableContentNode.Create(supportingText);
        field.Shape = shape;
        field.TextStyle = textStyle;
        field.VisualTransformation = visualTransformation;
        field.KeyboardOptions = keyboardOptions;
        field.KeyboardActions = keyboardActions;
        field.Render(composer);
    }

}
