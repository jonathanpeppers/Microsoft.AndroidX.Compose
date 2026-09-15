using AndroidX.Compose.Gallery.Registry;
using AndroidX.Compose.UI.Text;

namespace AndroidX.Compose.Gallery.Demos.TextInputs;

/// <summary>Foundation editor decoration, selection, cursor styling and IME Send.</summary>
public static class BasicTextFieldDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id: "text-basic-text-field",
        CategoryId: "text-inputs",
        Title: "BasicTextField",
        Description: "Native inner-editor decoration, selection and IME Send without Material field chrome.",
        Build: c =>
        {
            var input = c.MutableStateOf(ComposeExtensions.NewTextFieldValue());
            var text = c.MutableStateOf("String overload");
            var sent = c.MutableStateOf("Nothing sent");
            var options = c.Remember(() => KeyboardOptionsCompanion.Default.Copy(
                capitalization: KeyboardOptionsCompanion.Default.Capitalization,
                autoCorrectEnabled: null,
                keyboardType: KeyboardType.Text,
                imeAction: ImeAction.Send,
                platformImeOptions: null,
                showKeyboardOnFocus: null,
                hintLocales: null));
            var actions = c.Remember(() => KeyboardActionsHelper.Create(onSend: () =>
            {
                sent.Value = input.Value.Text;
                input.Value = ComposeExtensions.NewTextFieldValue();
            }));
            return new Column
            {
                new Text("Select text, insert an emoji, then use the keyboard Send action."),
                new BasicTextField(input.Value, value => input.Value = value, maxLines: 3)
                {
                    Modifier = Modifier.FillMaxWidth().Padding(12).Semantics("Basic value editor"),
                    TextStyle = new TextStyle { Color = Color.FromPacked(c.ColorScheme().OnSurface), FontSize = 18 },
                    CursorBrush = Brush.SolidColor(Color.FromPacked(c.ColorScheme().Primary)),
                    KeyboardOptions = options,
                    KeyboardActions = actions,
                    DecorationBox = inner => new Column
                    {
                        new Text("Custom decoration"),
                        inner,
                        new HorizontalDivider(),
                    },
                },
                new Button(() =>
                {
                    var value = input.Value;
                    int start = Math.Min((int)(value.Selection >> 32), (int)value.Selection);
                    int end = Math.Max((int)(value.Selection >> 32), (int)value.Selection);
                    string text = string.Concat(value.Text.AsSpan(0, start), "\U0001F680", value.Text.AsSpan(end));
                    input.Value = value.Copy(text, TextRangeKt.TextRange(start + 2), null);
                }) { new Text("Insert rocket at selection") },
                new Text($"Sent: {sent.Value}"),
                new Text("String-only state; Foundation retains the cursor and IME composition."),
                new BasicTextField(text.Value, value => text.Value = value, singleLine: true)
                {
                    Modifier = Modifier.FillMaxWidth().Padding(12).Semantics("Basic string editor"),
                    TextStyle = new TextStyle { Color = Color.FromPacked(c.ColorScheme().OnSurface), FontSize = 18 },
                    CursorBrush = Brush.SolidColor(Color.FromPacked(c.ColorScheme().Primary)),
                    DecorationBox = inner => new Column { inner, new HorizontalDivider() },
                },
            };
        });
}
