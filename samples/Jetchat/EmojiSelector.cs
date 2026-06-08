using AndroidX.Compose.Material3;
using ComposeNet;

namespace ComposeNet.Samples.Jetchat;

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
    /// <summary>Columns per grid row — matches upstream's <c>EMOJI_COLUMNS</c>.</summary>
    public const int EmojiColumns = 10;

    /// <summary>Rows rendered in the grid — matches upstream's hard-coded <c>repeat(4)</c>.</summary>
    public const int EmojiRows = 4;

    // Verbatim from upstream's `private val emojis = listOf(...)`. We
    // expose the first EmojiColumns * EmojiRows entries via the grid;
    // the rest are kept available for parity with the Kotlin source.
    static readonly string[] Emojis = new[]
    {
        "\U0001F600", // Grinning Face
        "\U0001F601", // Grinning Face With Smiling Eyes
        "\U0001F602", // Face With Tears of Joy
        "\U0001F603", // Smiling Face With Open Mouth
        "\U0001F604", // Smiling Face With Open Mouth and Smiling Eyes
        "\U0001F605", // Smiling Face With Open Mouth and Cold Sweat
        "\U0001F606", // Smiling Face With Open Mouth and Tightly-Closed Eyes
        "\U0001F609", // Winking Face
        "\U0001F60A", // Smiling Face With Smiling Eyes
        "\U0001F60B", // Face Savouring Delicious Food
        "\U0001F60E", // Smiling Face With Sunglasses
        "\U0001F60D", // Smiling Face With Heart-Shaped Eyes
        "\U0001F618", // Face Throwing a Kiss
        "\U0001F617", // Kissing Face
        "\U0001F619", // Kissing Face With Smiling Eyes
        "\U0001F61A", // Kissing Face With Closed Eyes
        "\u263A",     // White Smiling Face
        "\U0001F642", // Slightly Smiling Face
        "\U0001F917", // Hugging Face
        "\U0001F607", // Smiling Face With Halo
        "\U0001F913", // Nerd Face
        "\U0001F914", // Thinking Face
        "\U0001F610", // Neutral Face
        "\U0001F611", // Expressionless Face
        "\U0001F636", // Face Without Mouth
        "\U0001F644", // Face With Rolling Eyes
        "\U0001F60F", // Smirking Face
        "\U0001F623", // Persevering Face
        "\U0001F625", // Disappointed but Relieved Face
        "\U0001F62E", // Face With Open Mouth
        "\U0001F910", // Zipper-Mouth Face
        "\U0001F62F", // Hushed Face
        "\U0001F62A", // Sleepy Face
        "\U0001F62B", // Tired Face
        "\U0001F634", // Sleeping Face
        "\U0001F60C", // Relieved Face
        "\U0001F61B", // Face With Stuck-Out Tongue
        "\U0001F61C", // Face With Stuck-Out Tongue and Winking Eye
        "\U0001F61D", // Face With Stuck-Out Tongue and Tightly-Closed Eyes
        "\U0001F612", // Unamused Face
    };

    /// <summary>Build the emoji selector panel.</summary>
    /// <param name="input">Shared text-field state; tapped emojis are appended to <c>input.Value</c>.</param>
    /// <param name="scheme">Active Material 3 color scheme — used for the panel background + tab colors.</param>
    public static ComposableNode Build(MutableState<string> input, ColorScheme scheme) =>
        new Composed(c =>
        {
            var selected = Compose.Remember(() => new MutableState<int>(0));
            return new Column
            {
                Modifier.Companion
                    .FillMaxWidth()
                    .Height(320)
                    .Background(new Color(scheme.SurfaceVariant)),
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

    static Column BuildEmojiGrid(MutableState<string> input, ColorScheme scheme)
    {
        var grid = new Column
        {
            Modifier.Companion.FillMaxWidth().Padding(8),
        };
        for (int row = 0; row < EmojiRows; row++)
        {
            var rowNode = new Row(Arrangement.SpaceEvenly)
            {
                Modifier.Companion.FillMaxWidth(),
            };
            for (int col = 0; col < EmojiColumns; col++)
            {
                string emoji = Emojis[row * EmojiColumns + col];
                rowNode.Add(new Text(emoji)
                {
                    FontSize = 18,
                    Color    = new Color(scheme.OnSurface),
                    Modifier = Modifier.Companion
                        .Clickable(() => input.Value = (input.Value ?? string.Empty) + emoji)
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
            Modifier.Companion.FillMaxWidth().Padding(horizontal: 16, vertical: 48),
            new Text("Stickers not yet implemented in this port.")
            {
                FontSize = 14,
                Color    = new Color(scheme.OnSurfaceVariant),
            },
        };
}
