using System.Collections.Generic;
using AndroidX.Compose.UI.Graphics;
using ComposeNet;

namespace ComposeNet.Samples.Jetchat;

/// <summary>
/// Builds the Jetchat conversation tree. A simplified port of
/// upstream's <c>ConversationContent</c> + <c>JetchatDrawerContent</c>:
/// a <see cref="ModalNavigationDrawer"/> wraps a <see cref="Scaffold"/>
/// inside a <see cref="MaterialTheme"/>. The Scaffold has a
/// <see cref="CenterAlignedTopAppBar"/> showing the channel name and
/// member count plus search / info action icons, a vertical list of
/// message bubbles taking the remaining height (via
/// <c>Modifier.Weight(1f)</c>), and a sticky input row at the bottom
/// for typing and sending new messages. Edge-swipe from the left opens
/// the drawer; tapping a channel updates the top-bar title.
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

    // Material 3 surface-variant-ish greys, picked to approximate
    // Jetchat's bubble palette without binding MaterialTheme.colorScheme
    // (issue #61). "Me" bubbles get the primary-container shade, others
    // get the surface-variant shade. Stored as packed Compose Color
    // longs (see AndroidX.Compose.UI.Graphics.ColorKt.Color).
    static readonly long MeBubbleColor       = ColorKt.Color(red: 0xD0, green: 0xE4, blue: 0xFF, alpha: 0xFF);
    static readonly long OtherBubbleColor    = ColorKt.Color(red: 0xED, green: 0xED, blue: 0xED, alpha: 0xFF);
    static readonly long DrawerSelectedColor = ColorKt.Color(red: 0xD0, green: 0xE4, blue: 0xFF, alpha: 0xFF);

    /// <summary>Materialize the conversation tree for one composition pass.</summary>
    public static ComposableNode Build(ConversationUiState ui, MutableState<string> input, ScrollState drawerScroll)
    {
        // MaterialTheme injects M3 default colors / typography into the
        // composition so child Scaffold/TopAppBar/TextField pick up
        // proper surface colors instead of falling back to undefined
        // defaults. The drawer has to be inside the theme so its
        // ModalDrawerSheet's secondaryContainer fallback resolves.
        return new MaterialTheme
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
    }

    static CenterAlignedTopAppBar BuildTopBar(ConversationUiState ui) =>
        new()
        {
            // Two-line title: channel name on top, member count below.
            // Upstream uses different typography weights/sizes for each
            // — we render both at default size since `Text` doesn't
            // expose fontWeight/style yet (issue #58). The channel name
            // reads from the mutable CurrentChannel so drawer taps
            // update the displayed title.
            Title = new Column
            {
                new Text($"#{ui.CurrentChannel}"),
                new Text($"{ui.ChannelMembers} members")
                {
                    Modifier = Modifier.Companion.Padding(topDp: 2, bottomDp: 0, startDp: 0, endDp: 0),
                },
            },
            // Trailing search + info icons. Match upstream's
            // ChannelNameBar — no-op onClick because the upstream popup
            // ("FunctionalityNotAvailablePopup") would itself need
            // bound popup APIs. Real affordances; would-be-popup omitted.
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
            // No NavigationIcon: programmatic drawer open requires
            // DrawerState.open() (a Kotlin suspend function), which
            // isn't bound yet. A no-op hamburger would be a misleading
            // affordance — users open the drawer with an edge-swipe
            // from the left until #74 follow-up lands.
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

    // "me" messages: right-aligned via Arrangement.End on the Row,
    // no avatar tile, blueish bubble. Upstream uses a primary-color
    // bubble — same idea, different exact color since we don't have
    // MaterialTheme.colorScheme reads (issue #61).
    static Row BuildMyMessageRow(Message m) =>
        new(Arrangement.End)
        {
            Modifier.Companion.FillMaxWidth().Padding(horizontalDp: 8, verticalDp: 4),
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

    // Others: drawable-resource avatar on the left, then
    // author/timestamp row + bubble. On a streak (same author as
    // previous message), hide the avatar with a same-width Spacer so
    // the message body stays visually aligned with the previous one —
    // matches upstream's "first message in a chain only" behavior.
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
            // Drawable avatar via the new Phase 7 [PainterResource]
            // facade. Clip(20) on a 40dp box yields a circle.
            row.Add(new Image(m.AuthorAvatarRes, "Profile photo")
            {
                Modifier = Modifier.Companion.Size(40).Clip(20),
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
    // alignEnd pushes the row's content to the right via
    // Arrangement.End — matches what "me" message rows want.
    static Row BuildAuthorAndTimestamp(Message m, bool alignEnd)
    {
        var row = new Row(alignEnd ? Arrangement.End : null)
        {
            Modifier.Companion.FillMaxWidth(),
        };
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

    /// <summary>
    /// Builds the navigation drawer panel. Mirrors upstream's
    /// <c>JetchatDrawerContent</c>: header, "Chats" section with two
    /// channel rows, "Recent Profiles" section with two profile rows.
    /// The whole column is wrapped in <see cref="Modifier.VerticalScroll"/>
    /// so the panel scrolls if it overflows on small heights —
    /// upstream uses the same pattern via <c>verticalScroll(rememberScrollState())</c>.
    /// </summary>
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
                    Modifier = Modifier.Companion.Padding(horizontalDp: 28, verticalDp: 0),
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
            new Text("Jetchat"),
        };

    static Box BuildDrawerSectionHeader(string label) =>
        new()
        {
            Modifier.Companion.FillMaxWidth().Padding(horizontalDp: 28, verticalDp: 16),
            new Text(label),
        };

    static Row BuildChatItem(ConversationUiState ui, string channel)
    {
        bool selected = ui.CurrentChannel == channel;
        // Selection highlight: matching primary-container fill on the
        // pill. Tapping updates the mutable channel state, which in
        // turn re-renders the top app bar title.
        var modifier = Modifier.Companion
            .FillMaxWidth()
            .Height(56)
            .Padding(horizontalDp: 12, verticalDp: 0)
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
                Modifier = Modifier.Companion.Padding(topDp: 16, bottomDp: 16, startDp: 0, endDp: 0),
            },
        };
    }

    static Row BuildProfileItem(string name, int avatarRes) =>
        new()
        {
            Modifier.Companion
                .FillMaxWidth()
                .Height(56)
                .Padding(horizontalDp: 12, verticalDp: 0)
                .Clip(28)
                .Clickable(NoOp),
            new Image(avatarRes, "Profile photo")
            {
                Modifier = Modifier.Companion.Padding(16).Size(24).Clip(12),
            },
            new Spacer(Modifier.Companion.Width(12)),
            new Text(name)
            {
                Modifier = Modifier.Companion.Padding(topDp: 16, bottomDp: 16, startDp: 0, endDp: 0),
            },
        };

    static void Send(ConversationUiState ui, MutableState<string> input)
    {
        var text = input.Value ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text)) return;
        ui.AddMessage(MyName, text, Resource.Drawable.avatar_ali, FormatNow());
        input.Value = string.Empty;
    }

    static string FormatNow()
    {
        var now = System.DateTime.Now;
        return now.ToString("h:mm tt", System.Globalization.CultureInfo.InvariantCulture);
    }

    static void NoOp() { }
}
