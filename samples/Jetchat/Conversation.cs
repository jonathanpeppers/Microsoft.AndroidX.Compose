using System;
using System.Collections.Generic;
using AndroidX.Compose.Material3;
using ComposeNet;

namespace ComposeNet.Samples.Jetchat;

/// <summary>
/// Builds the Jetchat conversation tree. C# port of upstream's
/// <c>ConversationContent</c> + <c>UserInput</c> +
/// <c>JetchatDrawerContent</c>. See <c>README.md</c> for the remaining
/// gaps vs the Kotlin original (jump-to-bottom, message formatter,
/// voice record, profile screen, real emoji table).
/// </summary>
public static class Conversation
{
    /// <summary>Author tag the local user sends with — matches upstream's <c>R.string.author_me</c>.</summary>
    public const string MyName = "me";

    const string MeProfileId        = "me";
    const string ColleagueProfileId = "12345";

    const int SelEmoji   = 1;
    const int SelDm      = 2;
    const int SelPicture = 3;
    const int SelMap     = 4;
    const int SelPhone   = 5;

    /// <summary>Materialize the conversation tree for one composition pass.</summary>
    public static ComposableNode Build(
        ConversationUiState  ui,
        MutableState<string> input,
        MutableState<string> selectedMenu,
        ScrollState          drawerScroll,
        DrawerStateHolder    drawerState,
        MutableState<int>    selectedSelector,
        MutableState<bool>   popupOpen,
        LazyListState        messagesScroll) =>
        JetchatTheme.Build(new Composed(c =>
        {
            var scheme = MaterialTheme.CurrentColorScheme(c);
            var root   = new Column
            {
                Modifier.Companion.FillMaxSize(),
                new ModalNavigationDrawer(drawerState)
                {
                    Drawer  = BuildDrawer(ui, selectedMenu, drawerState, drawerScroll, scheme),
                    Content = new Scaffold
                    {
                        TopBar = BuildTopBar(ui, scheme, drawerState, popupOpen),
                        Body   = BuildBody(ui, input, scheme, selectedSelector, messagesScroll, popupOpen),
                    },
                },
            };
            if (popupOpen.Value)
                root.Add(BuildFunctionalityPopup(popupOpen));
            return root;
        }));

    static CenterAlignedTopAppBar BuildTopBar(
        ConversationUiState ui,
        ColorScheme         scheme,
        DrawerStateHolder   drawerState,
        MutableState<bool>  popupOpen) =>
        new()
        {
            NavigationIcon = new IconButton(onClick: () => _ = drawerState.OpenAsync())
            {
                JetchatIcon.Build("Open navigation drawer", sizeDp: 32),
            },
            Title = new Column
            {
                new Text(ui.ChannelName)
                {
                    FontSize   = 16,
                    FontWeight = FontWeight.Medium,
                    Color      = new Color(scheme.OnSurface),
                },
                new Text($"{ui.ChannelMembers} members")
                {
                    FontSize = 12,
                    Color    = new Color(scheme.OnSurfaceVariant),
                    Modifier = Modifier.Companion.Padding(top: 2, bottom: 0, start: 0, end: 0),
                },
            },
            Actions = new Row
            {
                new Icon(Resource.Drawable.ic_search, "Search")
                {
                    Modifier = Modifier.Companion
                        .Clickable(() => popupOpen.Value = true)
                        .Padding(horizontal: 12, vertical: 16)
                        .Height(24),
                    TintArgb = scheme.OnSurfaceVariant,
                },
                new Icon(Resource.Drawable.ic_info, "Information")
                {
                    Modifier = Modifier.Companion
                        .Clickable(() => popupOpen.Value = true)
                        .Padding(horizontal: 12, vertical: 16)
                        .Height(24),
                    TintArgb = scheme.OnSurfaceVariant,
                },
            },
        };

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
        MutableState<int>    selectedSelector,
        LazyListState        messagesScroll,
        MutableState<bool>   popupOpen) =>
        new()
        {
            Modifier.Companion.FillMaxSize(),
            BuildMessages(ui, scheme, messagesScroll, popupOpen),
            BuildInputArea(ui, input, scheme, selectedSelector, messagesScroll),
        };

    // Flat row stream so LazyColumn<T> can render messages and day
    // headers as a single item list — same shape as upstream's
    // `for index in messages.indices { … item { … } }` loop.
    abstract record ChatRow;
    sealed record MessageRow(Message Msg, bool IsFirstByAuthor, bool IsLastByAuthor) : ChatRow;
    sealed record HeaderRow(string Label) : ChatRow;

    static ComposableNode BuildMessages(ConversationUiState ui, ColorScheme scheme, LazyListState messagesScroll, MutableState<bool> popupOpen)
    {
        var msgs = ui.Messages;
        var rows = new List<ChatRow>(msgs.Count + 2);
        for (int i = 0; i < msgs.Count; i++)
        {
            // Hardcode day dividers for simplicity.
            if (i == msgs.Count - 1)
                rows.Add(new HeaderRow("20 Aug"));
            else if (i == 2)
                rows.Add(new HeaderRow("Today"));

            var m          = msgs[i];
            var prevAuthor = i - 1 >= 0         ? msgs[i - 1].Author : null;
            var nextAuthor = i + 1 < msgs.Count ? msgs[i + 1].Author : null;
            bool isFirst = prevAuthor != m.Author;
            bool isLast  = nextAuthor != m.Author;
            rows.Add(new MessageRow(m, isFirst, isLast));
        }

        return new Box
        {
            Modifier.Companion.FillMaxWidth().Weight(1f, fill: true),

            new LazyColumn<ChatRow>(
                items:       rows,
                itemContent: row => row switch
                {
                    MessageRow mr => BuildMessageRow(mr.Msg, mr.IsFirstByAuthor, mr.IsLastByAuthor, scheme, popupOpen),
                    HeaderRow  hr => BuildDayHeader(hr.Label, scheme),
                    _             => new Spacer(Modifier.Companion.Width(0)),
                })
            {
                Modifier      = Modifier.Companion.FillMaxSize(),
                ReverseLayout = true,
                State         = messagesScroll,
            },

            // Jump to bottom button shows up when the user has scrolled
            // away from the newest message (index 0 in reverse layout).
            new Composed(c =>
            {
                var visible = messagesScroll.FirstVisibleItemIndex != 0
                           || messagesScroll.FirstVisibleItemScrollOffset > 0;
                if (!visible)
                    return null;

                return new ExtendedFloatingActionButton(
                    onClick:  () => _ = messagesScroll.AnimateScrollToItemAsync(0),
                    expanded: false)
                {
                    Modifier = Modifier.Companion
                        .Align(Alignment.BottomCenter)
                        .Padding(start: 0, top: 0, end: 0, bottom: 16),
                    Icon = new Icon(Resource.Drawable.ic_arrow_downward, "Jump to bottom"),
                    Text = new Text("Jump to bottom"),
                };
            }),
        };
    }

    static Row BuildDayHeader(string label, ColorScheme scheme) =>
        new()
        {
            Modifier.Companion.Padding(horizontal: 16, vertical: 8).Height(16),
            new HorizontalDivider
            {
                Modifier  = Modifier.Companion.Weight(1f),
                ColorArgb = scheme.OnSurface,
            },
            new Text(label)
            {
                FontSize   = 11,
                FontWeight = FontWeight.Medium,
                Color      = new Color(scheme.OnSurfaceVariant),
                Modifier   = Modifier.Companion.Padding(horizontal: 16, vertical: 0),
            },
            new HorizontalDivider
            {
                Modifier  = Modifier.Companion.Weight(1f),
                ColorArgb = scheme.OnSurface,
            },
        };

    static Row BuildMessageRow(Message m, bool isFirstByAuthor, bool isLastByAuthor, ColorScheme scheme, MutableState<bool> popupOpen)
    {
        var row = new Row
        {
            Modifier.Companion.Padding(top: isLastByAuthor ? 8 : 0, bottom: 0, start: 0, end: 0),
        };

        if (isLastByAuthor)
            row.Add(BuildAvatar(m, scheme));
        else
            row.Add(new Spacer(Modifier.Companion.Width(74)));

        row.Add(BuildAuthorAndTextMessage(m, isFirstByAuthor, isLastByAuthor, scheme, popupOpen));
        return row;
    }

    static Image BuildAvatar(Message m, ColorScheme scheme)
    {
        bool isMe = m.Author == MyName;
        long accent = isMe ? scheme.Primary : scheme.Tertiary;
        return new Image(m.AuthorImage, "Profile photo")
        {
            Modifier = Modifier.Companion
                .Padding(horizontal: 16, vertical: 0)
                .Size(42)
                .Border(1.5f, new Color(accent),         Shape.Circle())
                .Border(3,    new Color(scheme.Surface), Shape.Circle())
                .Clip(21),
        };
    }

    static Column BuildAuthorAndTextMessage(Message m, bool isFirstByAuthor, bool isLastByAuthor, ColorScheme scheme, MutableState<bool> popupOpen)
    {
        var col = new Column
        {
            Modifier.Companion.Padding(top: 0, bottom: 0, start: 0, end: 16).Weight(1f, fill: true),
        };
        if (isLastByAuthor)
            col.Add(BuildAuthorNameTimestamp(m, scheme));
        col.Add(BuildChatItemBubble(m, scheme, popupOpen));
        col.Add(new Spacer(Modifier.Companion.Height(isFirstByAuthor ? 8 : 4)));
        return col;
    }

    static Row BuildAuthorNameTimestamp(Message m, ColorScheme scheme) =>
        new()
        {
            new Text(m.Author)
            {
                FontSize   = 16,
                FontWeight = FontWeight.Medium,
                Color      = new Color(scheme.OnSurface),
                Modifier   = Modifier.Companion.Padding(top: 0, bottom: 8, start: 0, end: 0),
            },
            new Spacer(Modifier.Companion.Width(8)),
            new Text(m.Timestamp)
            {
                FontSize = 12,
                Color    = new Color(scheme.OnSurfaceVariant),
                Modifier = Modifier.Companion.Padding(top: 0, bottom: 8, start: 0, end: 0),
            },
        };

    static ComposableNode BuildChatItemBubble(Message m, ColorScheme scheme, MutableState<bool> popupOpen)
    {
        bool isMe = m.Author == MyName;
        long bg   = isMe ? scheme.Primary : scheme.SurfaceVariant;
        long fg   = isMe ? scheme.OnPrimary : scheme.OnSurface;
        var formatted = MessageFormatter.Format(m.Content, isMe, scheme, _ => popupOpen.Value = true);
        return new AnnotatedText(formatted)
        {
            Color    = new Color(fg),
            Modifier = Modifier.Companion
                .Background(new Color(bg), Shape.RoundedCorners(4, 20, 20, 20))
                .Padding(horizontal: 16, vertical: 16),
        };
    }

    static Surface BuildInputArea(
        ConversationUiState  ui,
        MutableState<string> input,
        ColorScheme          scheme,
        MutableState<int>    selectedSelector,
        LazyListState        messagesScroll) =>
        new()
        {
            Modifier.Companion.FillMaxWidth().NavigationBarsPadding().ImePadding(),
            new Column
            {
                Modifier.Companion.FillMaxWidth(),
                BuildTextFieldRow(ui, input),
                BuildSelectorRow(ui, input, scheme, selectedSelector, messagesScroll),
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
        MutableState<int>    selectedSelector,
        LazyListState        messagesScroll)
    {
        var row = new Row(Arrangement.SpaceBetween)
        {
            Modifier.Companion.FillMaxWidth().Padding(horizontal: 4, vertical: 4),
            new Row
            {
                InputSelectorButton(Resource.Drawable.ic_mood,            "Show Emoji selector", SelEmoji,   selectedSelector, scheme),
                InputSelectorButton(Resource.Drawable.ic_alternate_email, "Direct Message",      SelDm,      selectedSelector, scheme),
                InputSelectorButton(Resource.Drawable.ic_insert_photo,    "Attach Photo",        SelPicture, selectedSelector, scheme),
                InputSelectorButton(Resource.Drawable.ic_place,           "Location selector",   SelMap,     selectedSelector, scheme),
                InputSelectorButton(Resource.Drawable.ic_duo,             "Start videochat",     SelPhone,   selectedSelector, scheme),
            },
        };
        bool enabled = !string.IsNullOrWhiteSpace(input.Value);
        row.Add(new TextButton(onClick: () => Send(ui, input, selectedSelector, messagesScroll))
        {
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
            new Icon(drawableId, contentDescription)
            {
                TintArgb = selected ? scheme.OnSecondary : scheme.OnSurface,
            },
        };
        if (selected)
            button.Modifier = Modifier.Companion.Background(new Color(scheme.Secondary), Shape.RoundedCorners(14, 14, 14, 14));
        return button;
    }

    static ComposableNode BuildSelectorPanel(ColorScheme scheme, MutableState<int> selectedSelector)
    {
        int sel = selectedSelector.Value;
        if (sel == 0) return new Spacer(Modifier.Companion.Width(0));
        string title    = "Functionality currently not available";
        string subtitle = "Grab a beverage and check back later!";
        return new Column
        {
            Modifier.Companion.FillMaxWidth().Height(320).Background(new Color(scheme.SurfaceVariant)),
            new Spacer(Modifier.Companion.Height(96)),
            new Text(title)
            {
                FontSize   = 16,
                FontWeight = FontWeight.Medium,
                Color      = new Color(scheme.OnSurfaceVariant),
                Modifier   = Modifier.Companion.Padding(horizontal: 16, vertical: 0),
            },
            new Spacer(Modifier.Companion.Height(8)),
            new Text(subtitle)
            {
                FontSize = 14,
                Color    = new Color(scheme.OnSurfaceVariant),
                Modifier = Modifier.Companion.Padding(horizontal: 16, vertical: 0),
            },
        };
    }

    static ModalDrawerSheet BuildDrawer(
        ConversationUiState  ui,
        MutableState<string> selectedMenu,
        DrawerStateHolder    drawerState,
        ScrollState          scroll,
        ColorScheme          scheme)
    {
        var sheet = new ModalDrawerSheet { ContainerColor = new Color(scheme.Surface) };
        sheet.Add(new Column
        {
            Modifier.Companion.FillMaxWidth().VerticalScroll(scroll),
            new Spacer(Modifier.Companion.StatusBarsPadding()),
            BuildDrawerHeader(scheme),
            BuildDividerItem(scheme, sidePadding: 0),
            BuildDrawerSectionHeader("Chats", scheme),
            BuildChatItem(selectedMenu, drawerState, "composers",    scheme),
            BuildChatItem(selectedMenu, drawerState, "droidcon-nyc", scheme),
            BuildDividerItem(scheme, sidePadding: 28),
            BuildDrawerSectionHeader("Recent Profiles", scheme),
            BuildProfileItem(selectedMenu, drawerState, "Ali Conors (you)", MeProfileId,        Resource.Drawable.avatar_ali,          scheme),
            BuildProfileItem(selectedMenu, drawerState, "Taylor Brooks",    ColleagueProfileId, Resource.Drawable.avatar_someone_else, scheme),
        });
        return sheet;
    }

    static Row BuildDrawerHeader(ColorScheme scheme) =>
        new()
        {
            Modifier.Companion.FillMaxWidth().Padding(16),
            JetchatIcon.Build(contentDescription: null, sizeDp: 24),
            new Spacer(Modifier.Companion.Width(8)),
            new Text("Jetchat")
            {
                FontSize   = 18,
                FontWeight = FontWeight.SemiBold,
                Color      = new Color(scheme.OnSurface),
            },
        };

    static HorizontalDivider BuildDividerItem(ColorScheme scheme, int sidePadding) =>
        new()
        {
            Modifier  = sidePadding > 0
                ? Modifier.Companion.Padding(horizontal: sidePadding, vertical: 0)
                : null,
        };

    static Box BuildDrawerSectionHeader(string label, ColorScheme scheme) =>
        new()
        {
            Modifier.Companion.FillMaxWidth().Height(52).Padding(horizontal: 28, vertical: 0),
            new Text(label)
            {
                FontSize = 14,
                Color    = new Color(scheme.OnSurfaceVariant),
                Modifier = Modifier.Companion.Padding(top: 16, bottom: 0, start: 0, end: 0),
            },
        };

    static Row BuildChatItem(MutableState<string> selectedMenu, DrawerStateHolder drawerState, string channel, ColorScheme scheme)
    {
        bool selected = selectedMenu.Value == channel;
        var modifier = Modifier.Companion
            .FillMaxWidth()
            .Height(56)
            .Padding(horizontal: 12, vertical: 0)
            .Clip(28)
            .Clickable(() =>
            {
                selectedMenu.Value = channel;
                _ = drawerState.CloseAsync();
            });
        if (selected)
            modifier = modifier.Background(new Color(scheme.PrimaryContainer));

        long iconTint = selected ? scheme.Primary : scheme.OnSurfaceVariant;
        long textColor = selected ? scheme.Primary : scheme.OnSurface;

        return new Row
        {
            modifier,
            new Icon(Resource.Drawable.ic_jetchat, null)
            {
                Modifier = Modifier.Companion.Padding(top: 16, bottom: 16, start: 16, end: 0),
                TintArgb = iconTint,
            },
            new Text(channel)
            {
                FontSize   = 14,
                FontWeight = selected ? FontWeight.SemiBold : FontWeight.Normal,
                Color      = new Color(textColor),
                Modifier   = Modifier.Companion.Padding(top: 16, bottom: 16, start: 12, end: 0),
            },
        };
    }

    static Row BuildProfileItem(MutableState<string> selectedMenu, DrawerStateHolder drawerState, string name, string userId, int avatarRes, ColorScheme scheme)
    {
        bool selected = selectedMenu.Value == userId;
        var modifier = Modifier.Companion
            .FillMaxWidth()
            .Height(56)
            .Padding(horizontal: 12, vertical: 0)
            .Clip(28)
            .Clickable(() =>
            {
                selectedMenu.Value = userId;
                _ = drawerState.CloseAsync();
            });
        if (selected)
            modifier = modifier.Background(new Color(scheme.PrimaryContainer));

        return new Row
        {
            modifier,
            new Image(avatarRes, "Profile photo")
            {
                Modifier = Modifier.Companion
                    .Padding(top: 16, bottom: 16, start: 16, end: 0)
                    .Size(24)
                    .Clip(12),
            },
            new Text(name)
            {
                FontSize = 14,
                Color    = new Color(scheme.OnSurface),
                Modifier = Modifier.Companion.Padding(top: 16, bottom: 16, start: 12, end: 0),
            },
        };
    }

    static void Send(
        ConversationUiState  ui,
        MutableState<string> input,
        MutableState<int>    selectedSelector,
        LazyListState        messagesScroll)
    {
        var text = input.Value ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text)) return;
        ui.AddMessage(new Message(MyName, text.Trim(), "8:30 PM"));
        input.Value = string.Empty;
        selectedSelector.Value = 0;
        _ = messagesScroll.AnimateScrollToItemAsync(0);
    }
}
