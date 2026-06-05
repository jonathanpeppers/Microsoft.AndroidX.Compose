using System.Collections.Generic;
using AndroidX.Compose.UI.Graphics;
using ComposeNet;

namespace ComposeNet.Samples.Jetchat;

/// <summary>
/// Builds the Jetchat conversation tree. A simplified port of
/// upstream's <c>ConversationContent</c>: a <see cref="Scaffold"/>
/// inside a <see cref="MaterialTheme"/>, with a centered top bar
/// showing the channel name and member count on two lines, a vertical
/// list of message bubbles taking the remaining height (via
/// <c>Modifier.Weight(1f)</c>), and a sticky input row at the bottom
/// for typing and sending new messages.
///
/// Implemented as a static builder rather than a
/// <see cref="ComposableNode"/> subclass because
/// <see cref="ComposableNode.Render"/> is internal to the facade
/// assembly. Each recomposition calls <see cref="Build"/> to allocate
/// a fresh tree — that's the Tier 1.5 per-composition cost.
/// </summary>
public static class Conversation
{
    public const string MyName = "me";
    const string MyAvatar = "🙂";

    // Material 3 surface-variant-ish greys, picked to approximate
    // Jetchat's bubble palette without binding MaterialTheme.colorScheme
    // (issue #61). "Me" bubbles get the primary-container shade, others
    // get the surface-variant shade. Stored as packed Compose Color
    // longs (see AndroidX.Compose.UI.Graphics.ColorKt.Color).
    static readonly long MeBubbleColor    = ColorKt.Color(red: 0xD0, green: 0xE4, blue: 0xFF, alpha: 0xFF);
    static readonly long OtherBubbleColor = ColorKt.Color(red: 0xED, green: 0xED, blue: 0xED, alpha: 0xFF);
    static readonly long AvatarTileColor  = ColorKt.Color(red: 0xE0, green: 0xE0, blue: 0xE0, alpha: 0xFF);


    /// <summary>Materialize the conversation tree for one composition pass.</summary>
    public static ComposableNode Build(ConversationUiState ui, MutableState<string> input)
    {
        // MaterialTheme injects M3 default colors / typography into the
        // composition so child Scaffold/TopAppBar/TextField pick up
        // proper surface colors instead of falling back to undefined
        // defaults.
        return new MaterialTheme
        {
            new Scaffold
            {
                TopBar = BuildTopBar(ui),
                Body   = BuildBody(ui, input),
            },
        };
    }

    static CenterAlignedTopAppBar BuildTopBar(ConversationUiState ui) =>
        new()
        {
            // Two-line title: channel name on top, member count below.
            // Upstream uses different typography weights/sizes for each
            // — we render both at default size since `Text` doesn't
            // expose fontWeight/style yet (issue #58).
            Title = new Column
            {
                new Text($"#{ui.ChannelName}"),
                new Text($"{ui.ChannelMembers} members")
                {
                    Modifier = Modifier.Companion.Padding(topDp: 2, bottomDp: 0, startDp: 0, endDp: 0),
                },
            },
        };

    static Column BuildBody(ConversationUiState ui, MutableState<string> input) =>
        new()
        {
            Modifier.Companion.FillMaxSize(),
            BuildMessages(ui),
            BuildInputRow(ui, input),
        };

    static ComposableNode BuildMessages(ConversationUiState ui)
    {
        // Pre-compute streak flags so each item's content callback only
        // needs the message + isStreak, not the previous neighbor.
        // Enumerating ui.Messages here also subscribes the current
        // composition to the ObservableList tick.
        var rows       = new List<(Message Message, bool IsStreak)>(ui.Messages.Count);
        string? lastAuthor = null;
        foreach (var m in ui.Messages)
        {
            rows.Add((m, m.Author == lastAuthor));
            lastAuthor = m.Author;
        }

        // Day separator pinned above the lazy list; LazyColumn itself
        // takes the remaining vertical space via Weight(1f).
        return new Column
        {
            Modifier.Companion.FillMaxWidth().Weight(1f, fill: true),
            BuildDaySeparator("Today"),
            new LazyColumn<(Message Message, bool IsStreak)>(
                items:       rows,
                itemContent: row => BuildMessageRow(row.Message, row.IsStreak))
            {
                Modifier = Modifier.Companion.FillMaxWidth().Weight(1f, fill: true).Padding(horizontalDp: 8, verticalDp: 0),
            },
        };
    }

    static Row BuildDaySeparator(string label) =>
        new()
        {
            Modifier.Companion.FillMaxWidth().Padding(horizontalDp: 8, verticalDp: 12),
            new HorizontalDivider { Modifier = Modifier.Companion.Weight(1f) },
            new Text(label)
            {
                Modifier = Modifier.Companion.Padding(horizontalDp: 12, verticalDp: 0),
            },
            new HorizontalDivider { Modifier = Modifier.Companion.Weight(1f) },
        };

    static Row BuildMessageRow(Message m, bool isStreak)
    {
        bool isMe = m.Author == MyName;
        return isMe
            ? BuildMyMessageRow(m)
            : BuildOtherMessageRow(m, isStreak);
    }

    // "me" messages: right-aligned via a leading Spacer().Weight(1f),
    // no avatar tile, blueish bubble. Upstream uses a primary-color
    // bubble with no avatar on the "me" side — same idea, different
    // exact color since we don't have MaterialTheme.colorScheme reads
    // (issue #61).
    static Row BuildMyMessageRow(Message m) =>
        new()
        {
            Modifier.Companion.FillMaxWidth().Padding(horizontalDp: 8, verticalDp: 4),
            // Push everything to the right edge. Workaround for missing
            // Row(horizontalArrangement=Arrangement.End) — issue #70.
            new Spacer(Modifier.Companion.Weight(1f)),
            new Column
            {
                BuildAuthorAndTimestamp(m, alignEnd: true),
                new Text(m.Content)
                {
                    Modifier = Modifier.Companion
                        .Padding(topDp: 4, bottomDp: 0, startDp: 0, endDp: 0)
                        .Clip(12)
                        .Background(MeBubbleColor)
                        .Padding(horizontalDp: 12, verticalDp: 8),
                },
            },
        };

    // Others: avatar tile on the left, then author/timestamp row +
    // bubble. On a streak (same author as previous message), hide the
    // avatar with a same-width Spacer so the message body stays
    // visually aligned with the previous one — matches upstream's
    // "first message in a chain only" behavior.
    static Row BuildOtherMessageRow(Message m, bool isStreak)
    {
        var row = new Row
        {
            Modifier.Companion.FillMaxWidth().Padding(horizontalDp: 8, verticalDp: 4),
        };
        if (isStreak)
        {
            row.Add(new Spacer(Modifier.Companion.Size(40)));
        }
        else
        {
            row.Add(new Box
            {
                Modifier.Companion.Size(40).Clip(20).Background(AvatarTileColor),
                new Text(m.AuthorAvatar) { Modifier = Modifier.Companion.Padding(8) },
            });
        }
        var contentCol = new Column
        {
            Modifier.Companion.Padding(startDp: 12, topDp: 0, endDp: 0, bottomDp: 0),
        };
        if (!isStreak)
            contentCol.Add(BuildAuthorAndTimestamp(m, alignEnd: false));
        contentCol.Add(new Text(m.Content)
        {
            Modifier = Modifier.Companion
                .Padding(topDp: 4, bottomDp: 0, startDp: 0, endDp: 0)
                .Clip(12)
                .Background(OtherBubbleColor)
                .Padding(horizontalDp: 12, verticalDp: 8),
        });
        row.Add(contentCol);
        return row;
    }

    // Author + timestamp on one line, separated by a small spacer.
    // alignEnd uses a leading Spacer-weight trick to push the row's
    // content to the right (workaround for missing
    // Row(horizontalArrangement=Arrangement.End) — issue #70).
    static Row BuildAuthorAndTimestamp(Message m, bool alignEnd)
    {
        var row = new Row
        {
            Modifier.Companion.FillMaxWidth(),
        };
        if (alignEnd)
            row.Add(new Spacer(Modifier.Companion.Weight(1f)));
        row.Add(new Text(m.Author));
        row.Add(new Spacer(Modifier.Companion.Width(8)));
        row.Add(new Text(m.Timestamp));
        return row;
    }

    static Row BuildInputRow(ConversationUiState ui, MutableState<string> input) =>
        new()
        {
            Modifier.Companion.FillMaxWidth().Padding(8),
            new TextField(input)
            {
                Modifier = Modifier.Companion.Weight(1f, fill: true),
            },
            new IconButton(() => Send(ui, input))
            {
                Modifier.Companion.Padding(startDp: 8, topDp: 0, endDp: 0, bottomDp: 0),
                new Text("➤"),
            },
        };

    static void Send(ConversationUiState ui, MutableState<string> input)
    {
        var text = input.Value ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text)) return;
        ui.AddMessage(MyName, text, MyAvatar, FormatNow());
        input.Value = string.Empty;
    }

    static string FormatNow()
    {
        var now = System.DateTime.Now;
        return now.ToString("h:mm tt", System.Globalization.CultureInfo.InvariantCulture);
    }
}
