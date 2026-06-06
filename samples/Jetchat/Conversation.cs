using System.Collections.Generic;
using AndroidX.Compose.UI.Graphics;
using ComposeNet;

namespace ComposeNet.Samples.Jetchat;

/// <summary>
/// Builds the Jetchat conversation tree. A simplified port of upstream's
/// <c>ConversationContent</c> + <c>JetchatDrawerContent</c>. See the
/// sample's <c>README.md</c> ("Implementation notes") for the deviations
/// from the original Kotlin sample.
/// </summary>
public static class Conversation
{
    public const string MyName = "me";

    static readonly long MeBubbleColor       = ColorKt.Color(red: 0xD0, green: 0xE4, blue: 0xFF, alpha: 0xFF);
    static readonly long OtherBubbleColor    = ColorKt.Color(red: 0xED, green: 0xED, blue: 0xED, alpha: 0xFF);
    static readonly long DrawerSelectedColor = ColorKt.Color(red: 0xD0, green: 0xE4, blue: 0xFF, alpha: 0xFF);

    /// <summary>Materialize the conversation tree for one composition pass.</summary>
    public static ComposableNode Build(ConversationUiState ui, MutableState<string> input, ScrollState drawerScroll) =>
        new MaterialTheme
        {
            new ModalNavigationDrawer
            {
                Drawer  = BuildDrawer(ui, drawerScroll),
                Content = new Scaffold
                {
                    TopBar = BuildTopBar(ui),
                    Body   = BuildBody(ui, input),
                },
            },
        };

    static CenterAlignedTopAppBar BuildTopBar(ConversationUiState ui) =>
        new()
        {
            Title = new Column
            {
                new Text($"#{ui.CurrentChannel}")
                {
                    FontSize   = 16,
                    FontWeight = FontWeight.Medium,
                },
                new Text($"{ui.ChannelMembers} members")
                {
                    FontSize   = 12,
                    FontWeight = FontWeight.Normal,
                    Modifier   = Modifier.Companion.Padding(top: 2, bottom: 0, start: 0, end: 0),
                },
            },
            Actions = new Row
            {
                new IconButton(onClick: NoOp)
                {
                    new Icon(Resource.Drawable.ic_search, "Search"),
                },
                new IconButton(onClick: NoOp)
                {
                    new Icon(Resource.Drawable.ic_info, "Info"),
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
        // Pre-compute streak flags so each LazyColumn item callback only
        // needs the message + isStreak, not the previous neighbor.
        var rows       = new List<(Message Message, bool IsStreak)>(ui.Messages.Count);
        string? lastAuthor = null;
        foreach (var m in ui.Messages)
        {
            rows.Add((m, m.Author == lastAuthor));
            lastAuthor = m.Author;
        }

        return new Column
        {
            Modifier.Companion.FillMaxWidth().Weight(1f, fill: true),
            BuildDaySeparator("Today"),
            new LazyColumn<(Message Message, bool IsStreak)>(
                items:       rows,
                itemContent: row => BuildMessageRow(row.Message, row.IsStreak))
            {
                Modifier = Modifier.Companion.FillMaxWidth().Weight(1f, fill: true).Padding(horizontal: 8, vertical: 0),
            },
        };
    }

    static Row BuildDaySeparator(string label) =>
        new()
        {
            Modifier.Companion.FillMaxWidth().Padding(horizontal: 8, vertical: 12),
            new HorizontalDivider { Modifier = Modifier.Companion.Weight(1f) },
            new Text(label)
            {
                FontSize      = 11,
                FontWeight    = FontWeight.Medium,
                LetterSpacing = 1,
                Modifier      = Modifier.Companion.Padding(horizontal: 12, vertical: 0),
            },
            new HorizontalDivider { Modifier = Modifier.Companion.Weight(1f) },
        };

    static Row BuildMessageRow(Message m, bool isStreak)
    {
        bool isMe = m.Author == MyName;
        return isMe
            ? BuildMyMessageRow(m, isStreak)
            : BuildOtherMessageRow(m, isStreak);
    }

    static Row BuildMyMessageRow(Message m, bool isStreak) =>
        new(Arrangement.End)
        {
            Modifier.Companion.FillMaxWidth().Padding(start: 8, end: 8, top: isStreak ? 4 : 8, bottom: 0),
            new Column
            {
                BuildAuthorAndTimestamp(m, alignEnd: true),
                new Text(m.Content)
                {
                    Modifier = Modifier.Companion
                        .Padding(top: 4, bottom: 0, start: 0, end: 0)
                        .Clip(12)
                        .Background(MeBubbleColor)
                        .Padding(horizontal: 12, vertical: 8),
                },
            },
        };

    static Row BuildOtherMessageRow(Message m, bool isStreak)
    {
        var row = new Row
        {
            Modifier.Companion.FillMaxWidth().Padding(start: 8, end: 8, top: isStreak ? 4 : 8, bottom: 0),
        };
        if (isStreak)
        {
            row.Add(new Spacer(Modifier.Companion.Width(72)));
        }
        else
        {
            row.Add(new Image(m.AuthorAvatarRes, "Profile photo")
            {
                Modifier = Modifier.Companion.Padding(horizontal: 16, vertical: 0).Size(40).Clip(20),
            });
        }
        var contentCol = new Column();
        if (!isStreak)
            contentCol.Add(BuildAuthorAndTimestamp(m, alignEnd: false));
        contentCol.Add(new Text(m.Content)
        {
            Modifier = Modifier.Companion
                .Padding(top: 4, bottom: 0, start: 0, end: 16)
                .Clip(12)
                .Background(OtherBubbleColor)
                .Padding(horizontal: 12, vertical: 8),
        });
        row.Add(contentCol);
        return row;
    }

    static Row BuildAuthorAndTimestamp(Message m, bool alignEnd)
    {
        // alignEnd rows size to content so the outer Arrangement.End
        // Row can push the whole "me" stack to the right edge.
        var row = alignEnd
            ? new Row()
            : new Row { Modifier.Companion.FillMaxWidth() };
        row.Add(new Text(m.Author)
        {
            FontSize   = 16,
            FontWeight = FontWeight.Medium,
        });
        row.Add(new Spacer(Modifier.Companion.Width(8)));
        row.Add(new Text(m.Timestamp)
        {
            FontSize   = 12,
            FontWeight = FontWeight.Normal,
        });
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
                Modifier.Companion.Padding(start: 8, top: 0, end: 0, bottom: 0),
                new Text("➤"),
            },
        };

    static ModalDrawerSheet BuildDrawer(ConversationUiState ui, ScrollState scroll) =>
        new()
        {
            new Column
            {
                Modifier.Companion.FillMaxWidth().VerticalScroll(scroll),
                BuildDrawerHeader(),
                new HorizontalDivider(),
                BuildDrawerSectionHeader("Chats"),
                BuildChatItem(ui, "composers"),
                BuildChatItem(ui, "droidcon-nyc"),
                new HorizontalDivider
                {
                    Modifier = Modifier.Companion.Padding(horizontal: 28, vertical: 0),
                },
                BuildDrawerSectionHeader("Recent Profiles"),
                BuildProfileItem("Ali Conors (you)", Resource.Drawable.avatar_ali),
                BuildProfileItem("Taylor Brooks",     Resource.Drawable.avatar_taylor),
            },
        };

    static Row BuildDrawerHeader() =>
        new()
        {
            Modifier.Companion.FillMaxWidth().Padding(16),
            new Icon(Resource.Drawable.ic_jetchat, "Jetchat logo")
            {
                Modifier = Modifier.Companion.Size(24),
            },
            new Spacer(Modifier.Companion.Width(8)),
            new Text("Jetchat")
            {
                FontSize   = 18,
                FontWeight = FontWeight.SemiBold,
            },
        };

    static Box BuildDrawerSectionHeader(string label) =>
        new()
        {
            Modifier.Companion.FillMaxWidth().Padding(horizontal: 28, vertical: 16),
            new Text(label)
            {
                FontSize   = 14,
                FontWeight = FontWeight.Medium,
            },
        };

    static Row BuildChatItem(ConversationUiState ui, string channel)
    {
        bool selected = ui.CurrentChannel == channel;
        var modifier = Modifier.Companion
            .FillMaxWidth()
            .Height(56)
            .Padding(horizontal: 12, vertical: 0)
            .Clip(28)
            .Clickable(() => ui.CurrentChannel = channel);
        if (selected)
            modifier = modifier.Background(DrawerSelectedColor);

        return new Row
        {
            modifier,
            new Icon(Resource.Drawable.ic_jetchat, "Channel")
            {
                Modifier = Modifier.Companion.Padding(16),
            },
            new Spacer(Modifier.Companion.Width(12)),
            new Text(channel)
            {
                FontSize   = 16,
                FontWeight = selected ? FontWeight.SemiBold : FontWeight.Normal,
                Modifier   = Modifier.Companion.Padding(top: 16, bottom: 16, start: 0, end: 0),
            },
        };
    }

    static Row BuildProfileItem(string name, int avatarRes) =>
        new()
        {
            Modifier.Companion
                .FillMaxWidth()
                .Height(56)
                .Padding(horizontal: 12, vertical: 0)
                .Clip(28)
                .Clickable(NoOp),
            new Image(avatarRes, "Profile photo")
            {
                Modifier = Modifier.Companion.Padding(16).Size(24).Clip(12),
            },
            new Spacer(Modifier.Companion.Width(12)),
            new Text(name)
            {
                FontSize = 16,
                Modifier = Modifier.Companion.Padding(top: 16, bottom: 16, start: 0, end: 0),
            },
        };

    static void Send(ConversationUiState ui, MutableState<string> input)
    {
        var text = input.Value ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text)) return;
        ui.AddMessage(MyName, text, Resource.Drawable.avatar_ali, "now");
        input.Value = string.Empty;
    }

    static void NoOp() { }
}
