using System.Collections.Generic;
using AndroidX.Compose.Material3;
using ComposeNet;

namespace ComposeNet.Samples.Jetchat;

/// <summary>
/// Builds the Jetchat conversation tree. A simplified port of upstream's
/// <c>ConversationContent</c> + <c>UserInput</c> + <c>JetchatDrawerContent</c>.
/// See the sample's <c>README.md</c> ("What's omitted") for the
/// remaining gaps vs the Kotlin original.
/// </summary>
public static class Conversation
{
    /// <summary>Author tag the local user sends with.</summary>
    public const string MyName = "me";

    // Avatar shown for "me" bubbles. Same drawable for every channel
    // since the local user is implicitly the same person.
    static int MyAvatar => Resource.Drawable.avatar_ali;

    // Input selector indices — toggles the expanded panel below the
    // text field. 0 = none (panel hidden).
    const int SelEmoji   = 1;
    const int SelDm      = 2;
    const int SelPicture = 3;
    const int SelMap     = 4;
    const int SelPhone   = 5;

    /// <summary>Materialize the conversation tree for one composition pass.</summary>
    public static ComposableNode Build(
        ConversationUiState  ui,
        MutableState<string> input,
        ScrollState          drawerScroll,
        DrawerStateHolder    drawerState,
        MutableState<int>    selectedSelector,
        MutableState<bool>   popupOpen) =>
        new MaterialTheme
        {
            new Composed(c =>
            {
                var scheme = MaterialTheme.CurrentColorScheme(c);
                var root   = new Column
                {
                    Modifier.Companion.FillMaxSize(),
                    new ModalNavigationDrawer(drawerState)
                    {
                        Drawer  = BuildDrawer(ui, drawerScroll, scheme),
                        Content = new Scaffold
                        {
                            TopBar = BuildTopBar(ui, scheme, drawerState, popupOpen),
                            Body   = BuildBody(ui, input, scheme, selectedSelector),
                        },
                    },
                };
                if (popupOpen.Value)
                    root.Add(BuildFunctionalityPopup(popupOpen));
                return root;
            }),
        };

    static CenterAlignedTopAppBar BuildTopBar(
        ConversationUiState ui,
        ColorScheme         scheme,
        DrawerStateHolder   drawerState,
        MutableState<bool>  popupOpen)
    {
        var channel = ui.Current;
        return new CenterAlignedTopAppBar
        {
            NavigationIcon = new IconButton(onClick: () => _ = drawerState.OpenAsync())
            {
                new Icon(Resource.Drawable.ic_menu, "Open navigation drawer"),
            },
            Title = new Column
            {
                new Text($"#{channel.Name}")
                {
                    FontSize   = 16,
                    FontWeight = FontWeight.Medium,
                    Color      = new Color(scheme.OnSurface),
                },
                new Text($"{channel.Members} members")
                {
                    FontSize   = 12,
                    FontWeight = FontWeight.Normal,
                    Color      = new Color(scheme.OnSurfaceVariant),
                    Modifier   = Modifier.Companion.Padding(top: 2, bottom: 0, start: 0, end: 0),
                },
            },
            Actions = new Row
            {
                new IconButton(onClick: () => popupOpen.Value = true)
                {
                    new Icon(Resource.Drawable.ic_search, "Search"),
                },
                new IconButton(onClick: () => popupOpen.Value = true)
                {
                    new Icon(Resource.Drawable.ic_info, "Channel info"),
                },
            },
        };
    }

    static AlertDialog BuildFunctionalityPopup(MutableState<bool> popupOpen) =>
        new(onDismissRequest: () => popupOpen.Value = false)
        {
            Text          = new Text("Functionality not available \U0001F648"),
            ConfirmButton = new TextButton(onClick: () => popupOpen.Value = false)
            {
                new Text("CLOSE"),
            },
        };

    static Column BuildBody(
        ConversationUiState  ui,
        MutableState<string> input,
        ColorScheme          scheme,
        MutableState<int>    selectedSelector) =>
        new()
        {
            Modifier.Companion.FillMaxSize(),
            BuildMessages(ui, scheme),
            BuildInputArea(ui, input, scheme, selectedSelector),
        };

    /// <summary>
    /// Discriminated row type for the chat <see cref="LazyColumn{T}"/>:
    /// either a message bubble or an inline day-separator header.
    /// </summary>
    abstract record ChatRow;
    sealed record MessageRow(Message Msg, bool IsStreak) : ChatRow;
    sealed record HeaderRow(string Label) : ChatRow;

    static ComposableNode BuildMessages(ConversationUiState ui, ColorScheme scheme)
    {
        var src = ui.Current.Messages;
        // Reverse to newest-first so reverseLayout=true pins the newest
        // message to the bottom of the viewport (index 0 == bottom).
        var rows = new List<ChatRow>(src.Count + 1);
        Message? prev = null;
        for (int i = src.Count - 1; i >= 0; i--)
        {
            var m = src[i];
            // In the reversed walk, prev is the next-newer message we
            // already emitted. isStreak hides the avatar on any message
            // followed (in time) by another from the same author.
            bool isStreak = prev is not null && prev.Author == m.Author;
            rows.Add(new MessageRow(m, isStreak));
            prev = m;
        }
        // Topmost item in reverseLayout = last index. Place "Today" there
        // so it sits above the oldest message.
        rows.Add(new HeaderRow("Today"));

        return new LazyColumn<ChatRow>(
            items:       rows,
            itemContent: row => row switch
            {
                MessageRow mr => BuildMessageRow(mr.Msg, mr.IsStreak, scheme),
                HeaderRow  hr => BuildDaySeparator(hr.Label, scheme),
                _             => new Spacer(Modifier.Companion.Width(0)),
            })
        {
            Modifier      = Modifier.Companion.FillMaxWidth().Weight(1f, fill: true).Padding(horizontal: 8, vertical: 0),
            ReverseLayout = true,
        };
    }

    static Row BuildDaySeparator(string label, ColorScheme scheme) =>
        new()
        {
            Modifier.Companion.FillMaxWidth().Padding(horizontal: 8, vertical: 12),
            new HorizontalDivider { Modifier = Modifier.Companion.Weight(1f) },
            new Text(label)
            {
                FontSize      = 11,
                FontWeight    = FontWeight.Medium,
                LetterSpacing = 1,
                Color         = new Color(scheme.OnSurfaceVariant),
                Modifier      = Modifier.Companion.Padding(horizontal: 12, vertical: 0),
            },
            new HorizontalDivider { Modifier = Modifier.Companion.Weight(1f) },
        };

    static Row BuildMessageRow(Message m, bool isStreak, ColorScheme scheme)
    {
        bool isMe = m.Author == MyName;
        return isMe
            ? BuildMyMessageRow(m, isStreak, scheme)
            : BuildOtherMessageRow(m, isStreak, scheme);
    }

    static Row BuildMyMessageRow(Message m, bool isStreak, ColorScheme scheme) =>
        new(Arrangement.End)
        {
            Modifier.Companion.FillMaxWidth().Padding(start: 8, end: 8, top: isStreak ? 4 : 8, bottom: 0),
            new Column
            {
                BuildAuthorAndTimestamp(m, scheme, alignEnd: true),
                new Text(m.Content)
                {
                    Color    = new Color(scheme.OnPrimary),
                    Modifier = Modifier.Companion
                        .Padding(top: 4, bottom: 0, start: 0, end: 0)
                        // Asymmetric "me" bubble: flatten the top-end corner
                        // (where it visually meets the sender's edge).
                        .Background(new Color(scheme.Primary), Shape.RoundedCorners(20, 4, 20, 20))
                        .Padding(horizontal: 12, vertical: 8),
                },
            },
        };

    static Row BuildOtherMessageRow(Message m, bool isStreak, ColorScheme scheme)
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
            contentCol.Add(BuildAuthorAndTimestamp(m, scheme, alignEnd: false));
        contentCol.Add(new Text(m.Content)
        {
            Color    = new Color(scheme.OnSurface),
            Modifier = Modifier.Companion
                .Padding(top: 4, bottom: 0, start: 0, end: 16)
                // Asymmetric "other" bubble: flatten the top-start corner
                // (where it visually meets the avatar).
                .Background(new Color(scheme.SurfaceVariant), Shape.RoundedCorners(4, 20, 20, 20))
                .Padding(horizontal: 12, vertical: 8),
        });
        row.Add(contentCol);
        return row;
    }

    static Row BuildAuthorAndTimestamp(Message m, ColorScheme scheme, bool alignEnd)
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
            Color      = new Color(scheme.OnSurface),
        });
        row.Add(new Spacer(Modifier.Companion.Width(8)));
        row.Add(new Text(m.Timestamp)
        {
            FontSize   = 12,
            FontWeight = FontWeight.Normal,
            Color      = new Color(scheme.OnSurfaceVariant),
        });
        return row;
    }

    static Surface BuildInputArea(
        ConversationUiState  ui,
        MutableState<string> input,
        ColorScheme          scheme,
        MutableState<int>    selectedSelector) =>
        new()
        {
            // Surface gives the input region its own tonal layer and
            // soaks up the IME + nav-bar insets so the bar slides up
            // with the soft keyboard.
            Modifier.Companion.FillMaxWidth().NavigationBarsPadding().ImePadding(),
            new Column
            {
                Modifier.Companion.FillMaxWidth(),
                BuildTextFieldRow(ui, input),
                BuildSelectorRow(ui, input, scheme, selectedSelector),
                BuildSelectorPanel(scheme, selectedSelector),
            },
        };

    static Row BuildTextFieldRow(ConversationUiState ui, MutableState<string> input) =>
        new()
        {
            Modifier.Companion.FillMaxWidth().Padding(horizontal: 8, vertical: 4),
            new TextField(input)
            {
                Modifier = Modifier.Companion.Weight(1f, fill: true),
            },
        };

    static Row BuildSelectorRow(
        ConversationUiState  ui,
        MutableState<string> input,
        ColorScheme          scheme,
        MutableState<int>    selectedSelector)
    {
        var row = new Row(Arrangement.SpaceBetween)
        {
            Modifier.Companion.FillMaxWidth().Padding(horizontal: 4, vertical: 4),
            new Row
            {
                InputSelectorButton(Resource.Drawable.ic_mood,             "Emoji",   SelEmoji,   selectedSelector, scheme),
                InputSelectorButton(Resource.Drawable.ic_alternate_email,  "Mention", SelDm,      selectedSelector, scheme),
                InputSelectorButton(Resource.Drawable.ic_insert_photo,     "Attach photo", SelPicture, selectedSelector, scheme),
                InputSelectorButton(Resource.Drawable.ic_place,            "Share location", SelMap, selectedSelector, scheme),
                InputSelectorButton(Resource.Drawable.ic_duo,              "Video call", SelPhone, selectedSelector, scheme),
            },
        };
        bool enabled = !string.IsNullOrWhiteSpace(input.Value);
        row.Add(new TextButton(onClick: () => Send(ui, input, selectedSelector))
        {
            // Send is a no-op on empty input — surface the disabled
            // state visually via the label color since the underlying
            // facade doesn't expose an Enabled flag.
            new Text("Send")
            {
                FontWeight = FontWeight.SemiBold,
                Color      = enabled ? new Color(scheme.Primary) : new Color(scheme.OnSurfaceVariant),
            },
        });
        return row;
    }

    static IconButton InputSelectorButton(
        int               drawableId,
        string            contentDescription,
        int               selectorId,
        MutableState<int> selectedSelector,
        ColorScheme       scheme)
    {
        bool selected = selectedSelector.Value == selectorId;
        var button = new IconButton(onClick: () =>
            selectedSelector.Value = selected ? 0 : selectorId)
        {
            new Icon(drawableId, contentDescription),
        };
        if (selected)
            button.Modifier = Modifier.Companion.Background(new Color(scheme.SecondaryContainer), Shape.Circle());
        return button;
    }

    static ComposableNode BuildSelectorPanel(ColorScheme scheme, MutableState<int> selectedSelector)
    {
        int sel = selectedSelector.Value;
        if (sel == 0) return new Spacer(Modifier.Companion.Width(0));
        string label = sel switch
        {
            SelEmoji   => "Emoji selector — not yet implemented in this port.",
            SelDm      => "Mention picker — not yet implemented in this port.",
            SelPicture => "Photo picker — not yet implemented in this port.",
            SelMap     => "Location share — not yet implemented in this port.",
            SelPhone   => "Video call — not yet implemented in this port.",
            _          => "",
        };
        return new Box
        {
            Modifier.Companion.FillMaxWidth().Height(180).Background(new Color(scheme.SurfaceVariant)),
            new Text(label)
            {
                Color    = new Color(scheme.OnSurfaceVariant),
                Modifier = Modifier.Companion.Padding(16),
            },
        };
    }

    static ModalDrawerSheet BuildDrawer(
        ConversationUiState ui,
        ScrollState         scroll,
        ColorScheme         scheme) =>
        new()
        {
            new Column
            {
                Modifier.Companion.FillMaxWidth().VerticalScroll(scroll),
                BuildDrawerHeader(scheme),
                new HorizontalDivider { ColorArgb = scheme.OnSurface }, // tint matches upstream's tonal divider
                BuildDrawerSectionHeader("Chats", scheme),
                BuildChatItem(ui, "composers",    scheme),
                BuildChatItem(ui, "droidcon-nyc", scheme),
                new HorizontalDivider
                {
                    Modifier  = Modifier.Companion.Padding(horizontal: 28, vertical: 0),
                    ColorArgb = scheme.OnSurface,
                },
                BuildDrawerSectionHeader("Recent profiles", scheme),
                BuildProfileItem("Ali Conors (you)", Resource.Drawable.avatar_ali,    scheme),
                BuildProfileItem("Taylor Brooks",     Resource.Drawable.avatar_taylor, scheme),
            },
        };

    static Row BuildDrawerHeader(ColorScheme scheme) =>
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
                Color      = new Color(scheme.OnSurface),
            },
        };

    static Box BuildDrawerSectionHeader(string label, ColorScheme scheme) =>
        new()
        {
            Modifier.Companion.FillMaxWidth().Padding(horizontal: 28, vertical: 16),
            new Text(label)
            {
                FontSize   = 14,
                FontWeight = FontWeight.Medium,
                Color      = new Color(scheme.OnSurfaceVariant),
            },
        };

    static Row BuildChatItem(ConversationUiState ui, string channel, ColorScheme scheme)
    {
        bool selected = ui.CurrentChannel == channel;
        var modifier = Modifier.Companion
            .FillMaxWidth()
            .Height(56)
            .Padding(horizontal: 12, vertical: 0)
            .Clip(28)
            .Clickable(() => ui.CurrentChannel = channel);
        if (selected)
            modifier = modifier.Background(new Color(scheme.PrimaryContainer));

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
                Color      = new Color(selected ? scheme.OnPrimaryContainer : scheme.OnSurface),
                Modifier   = Modifier.Companion.Padding(top: 16, bottom: 16, start: 0, end: 0),
            },
        };
    }

    static Row BuildProfileItem(string name, int avatarRes, ColorScheme scheme) =>
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
                Color    = new Color(scheme.OnSurface),
                Modifier = Modifier.Companion.Padding(top: 16, bottom: 16, start: 0, end: 0),
            },
        };

    static void Send(
        ConversationUiState  ui,
        MutableState<string> input,
        MutableState<int>    selectedSelector)
    {
        var text = input.Value ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text)) return;
        ui.AddMessage(MyName, text, MyAvatar, "now");
        input.Value = string.Empty;
        // Dismiss any open input selector panel after sending.
        selectedSelector.Value = 0;
    }

    static void NoOp() { }
}
