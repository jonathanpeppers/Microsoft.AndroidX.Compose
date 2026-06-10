using AndroidX.Compose.UI.Text;
using AndroidX.Compose.UI.Text.Input;
using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.TextInputs;

/// <summary>
/// <see cref="TextField(MutableState{TextFieldValue})"/> — programmatic
/// cursor placement via the <see cref="TextFieldValue"/> overload. The
/// caret moves to the end of the buffer after each emoji tap, the same
/// pattern Jetchat uses for its emoji selector. See issue #204.
/// </summary>
public static class TextFieldCursorPlacementDemo
{
    static readonly string[] Emojis = ["😀", "🎉", "🚀", "🐱", "🌈", "✨"];

    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "text-textfield-cursor-placement",
        CategoryId:  "text-inputs",
        Title:       "TextField cursor placement",
        Description: "TextFieldValue overload — append text and pin the caret to the end.",
        Build:       c =>
        {
            var input = c.MutableStateOf(c.NewTextFieldValue());
            return new Column(verticalArrangement: Arrangement.SpacedBy(12))
            {
                new Text("Tap an emoji — it appends to the field and the caret moves to the end so the next keystroke lands after it.")
                {
                    FontWeight = FontWeight.Medium,
                },
                new TextField(input)
                {
                    Label       = new Text("Message"),
                    Placeholder = new Text("Type something…"),
                    SingleLine  = true,
                },
                new Row(horizontalArrangement: Arrangement.SpacedBy(8))
                {
                    BuildEmojiRow(input),
                },
                new Text($"Caret position: {(int)(input.Value?.Selection ?? 0L)}"),
            };
        });

    static Row BuildEmojiRow(MutableState<TextFieldValue> input)
    {
        var row = new Row(horizontalArrangement: Arrangement.SpacedBy(8));
        foreach (var emoji in Emojis)
        {
            row.Add(new Button(onClick: () =>
            {
                var current = input.Value ?? ComposeExtensions.NewTextFieldValue();
                var newText = current.Text + emoji;
                // Copy(text, selection, composition) keeps annotations
                // intact and lets us pin the caret. TextRangeKt.TextRange
                // packs (start, end) into the long Compose's selection
                // field expects.
                input.Value = current.Copy(newText, TextRangeKt.TextRange(newText.Length), composition: null);
            })
            {
                new Text(emoji),
            });
        }
        return row;
    }
}
