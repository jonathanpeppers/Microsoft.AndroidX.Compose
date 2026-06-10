using AndroidX.Compose.Material3;
using AndroidX.Compose.UI.Text;
using AndroidX.Compose.UI.Text.Input;

namespace AndroidX.Compose.Samples.Jetchat;

/// <summary>
/// Two-tab emoji / sticker panel that opens below the input row when
/// the <c>ic_mood</c> selector button is toggled. C# port of upstream
/// Jetchat's <c>EmojiSelector</c> composable + supporting
/// <c>ExtendedSelectorInnerButton</c> / <c>EmojiTable</c> helpers.
/// </summary>
/// <remarks>
/// Tapping a glyph in the Emojis tab appends it to the shared input
/// state — same behaviour as upstream's <c>onTextAdded</c> callback
/// that does <c>textState.addText(it)</c>. The Stickers tab renders a
/// "not implemented" placeholder, matching upstream's intent (the real
/// app's Stickers tab pops a "not available" dialog).
/// </remarks>
public static class EmojiSelector
{
    /// <summary>Columns per grid row.</summary>
    public const int EmojiColumns = 10;

    /// <summary>Rows rendered in the grid.</summary>
    public const int EmojiRows = 4;

    static readonly string[] Emojis = new[]
    {
        "😀", // Grinning Face
        "😁", // Grinning Face With Smiling Eyes
        "😂", // Face With Tears of Joy
        "😃", // Smiling Face With Open Mouth
        "😄", // Smiling Face With Open Mouth and Smiling Eyes
        "😅", // Smiling Face With Open Mouth and Cold Sweat
        "😆", // Smiling Face With Open Mouth and Tightly-Closed Eyes
        "😉", // Winking Face
        "😊", // Smiling Face With Smiling Eyes
        "😋", // Face Savouring Delicious Food
        "😎", // Smiling Face With Sunglasses
        "😍", // Smiling Face With Heart-Shaped Eyes
        "😘", // Face Throwing a Kiss
        "😗", // Kissing Face
        "😙", // Kissing Face With Smiling Eyes
        "😚", // Kissing Face With Closed Eyes
        "☺",     // White Smiling Face
        "🙂", // Slightly Smiling Face
        "🤗", // Hugging Face
        "😇", // Smiling Face With Halo
        "🤓", // Nerd Face
        "🤔", // Thinking Face
        "😐", // Neutral Face
        "😑", // Expressionless Face
        "😶", // Face Without Mouth
        "🙄", // Face With Rolling Eyes
        "😏", // Smirking Face
        "😣", // Persevering Face
        "😥", // Disappointed but Relieved Face
        "😮", // Face With Open Mouth
        "🤐", // Zipper-Mouth Face
        "😯", // Hushed Face
        "😪", // Sleepy Face
        "😫", // Tired Face
        "😴", // Sleeping Face
        "😌", // Relieved Face
        "😛", // Face With Stuck-Out Tongue
        "😜", // Face With Stuck-Out Tongue and Winking Eye
        "😝", // Face With Stuck-Out Tongue and Tightly-Closed Eyes
        "😒", // Unamused Face
    };

    /// <summary>Build the emoji selector panel.</summary>
    /// <param name="input">Shared text-field state; tapped emojis are appended to <c>input.Value.Text</c> and the caret is moved to the end.</param>
    /// <param name="scheme">Active Material 3 color scheme — used for the panel background + tab colors.</param>
    public static ComposableNode Build(MutableState<TextFieldValue> input, ColorScheme scheme) =>
        new Composed(c =>
        {
            var selected = c.MutableStateOf(0);
            return new Column
            {
                Modifier
                    .FillMaxWidth()
                    .Background(scheme.SurfaceVariant),
                new PrimaryTabRow(selectedTabIndex: selected.Value)
                {
                    new Tab(selected: selected.Value == 0, onClick: () => selected.Value = 0)
                    {
                        Text = new Text("Emojis"),
                    },
                    new Tab(selected: selected.Value == 1, onClick: () => selected.Value = 1)
                    {
                        Text = new Text("Stickers"),
                    },
                },
                selected.Value == 0
                    ? BuildEmojiGrid(input, scheme)
                    : BuildStickerPlaceholder(scheme),
            };
        });

    static Column BuildEmojiGrid(MutableState<TextFieldValue> input, ColorScheme scheme)
    {
        var grid = new Column
        {
            Modifier.FillMaxWidth().Padding(8),
        };
        for (int row = 0; row < EmojiRows; row++)
        {
            var rowNode = new Row(Arrangement.SpaceEvenly)
            {
                Modifier.FillMaxWidth(),
            };
            for (int col = 0; col < EmojiColumns; col++)
            {
                string emoji = Emojis[row * EmojiColumns + col];
                rowNode.Add(new Text(emoji)
                {
                    FontSize = 18,
                    Color    = scheme.OnSurface,
                    Modifier = Modifier
                        .Clickable(() =>
                        {
                            // Match upstream Jetchat's TextFieldState.addText:
                            // append the glyph and move the cursor to the
                            // end of the new buffer so the next keystroke
                            // lands after the emoji. Use the bound
                            // TextFieldValue.Copy(string, long, TextRange?)
                            // overload so onValueChange round-trips a real
                            // Compose peer.
                            var current = input.Value ?? ComposeExtensions.NewTextFieldValue();
                            var newText = current.Text + emoji;
                            input.Value = current.Copy(newText, TextRangeKt.TextRange(newText.Length), composition: null);
                        })
                        .SizeIn(minWidth: 42, minHeight: 42)
                        .Padding(8),
                });
            }
            grid.Add(rowNode);
        }
        return grid;
    }

    static Column BuildStickerPlaceholder(ColorScheme scheme) =>
        new()
        {
            Modifier.FillMaxWidth().Padding(horizontal: 16, vertical: 48),
            new Text("Stickers not yet implemented in this port.")
            {
                FontSize = 14,
                Color    = scheme.OnSurfaceVariant,
            },
        };
}
