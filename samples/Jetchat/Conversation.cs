using System;
using System.Collections.Generic;
using AndroidX.Compose.Material3;
using ComposeNet;

namespace ComposeNet.Samples.Jetchat;

/// <summary>
/// Builds the Jetchat conversation screen tree. C# port of upstream's
/// <c>ConversationContent</c> + <c>UserInput</c>. The activity-wide
/// drawer lives in <see cref="JetchatDrawer"/> / <see cref="JetchatApp"/>
/// so it stays visible across the conversation and profile routes —
/// matching upstream Jetchat, where the drawer sits on the host activity
/// rather than inside <c>ConversationFragment</c>.
/// </summary>
public static class Conversation
{
    /// <summary>Author tag the local user sends with — matches upstream's <c>R.string.author_me</c>.</summary>
    public const string MyName = "me";

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
        MutableState<int>    selectedSelector,
        MutableState<bool>   popupOpen,
        LazyListState        messagesScroll,
        Action               onOpenDrawer,
        Action<string>       onAuthorClicked) =>
        new Composed(c =>
        {
            var scheme = MaterialTheme.CurrentColorScheme(c);
            var root   = new Column
            {
                Modifier.Companion.FillMaxSize(),
                new Scaffold
                {
                    TopBar = BuildTopBar(ui, scheme, onOpenDrawer, popupOpen),
                    Body   = BuildBody(ui, input, scheme, selectedSelector, messagesScroll, onAuthorClicked),
                },
            };
            if (popupOpen.Value)
                root.Add(BuildFunctionalityPopup(popupOpen));
            return root;
        });

    static CenterAlignedTopAppBar BuildTopBar(
        ConversationUiState ui,
        ColorScheme         scheme,
        Action              onOpenDrawer,
        MutableState<bool>  popupOpen) =>
        new()
        {
            NavigationIcon = new IconButton(onClick: onOpenDrawer)
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
        Action<string>       onAuthorClicked) =>
        new()
        {
            Modifier.Companion.FillMaxSize(),
            BuildMessages(ui, scheme, messagesScroll, onAuthorClicked),
            BuildInputArea(ui, input, scheme, selectedSelector, messagesScroll),
        };

    // Flat row stream so LazyColumn<T> can render messages and day
    // headers as a single item list — same shape as upstream's
    // `for index in messages.indices { … item { … } }` loop.
    abstract record ChatRow;
    sealed record MessageRow(Message Msg, bool IsFirstByAuthor, bool IsLastByAuthor) : ChatRow;
    sealed record HeaderRow(string Label) : ChatRow;

    static ComposableNode BuildMessages(ConversationUiState ui, ColorScheme scheme, LazyListState messagesScroll, Action<string> onAuthorClicked)
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
                    MessageRow mr => BuildMessageRow(mr.Msg, mr.IsFirstByAuthor, mr.IsLastByAuthor, scheme, onAuthorClicked),
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

    static Row BuildMessageRow(Message m, bool isFirstByAuthor, bool isLastByAuthor, ColorScheme scheme, Action<string> onAuthorClicked)
    {
        var row = new Row
        {
            Modifier.Companion.Padding(top: isLastByAuthor ? 8 : 0, bottom: 0, start: 0, end: 0),
        };

        if (isLastByAuthor)
            row.Add(BuildAvatar(m, scheme, onAuthorClicked));
        else
            row.Add(new Spacer(Modifier.Companion.Width(74)));

        row.Add(BuildAuthorAndTextMessage(m, isFirstByAuthor, isLastByAuthor, scheme));
        return row;
    }

    static Image BuildAvatar(Message m, ColorScheme scheme, Action<string> onAuthorClicked)
    {
        bool isMe = m.Author == MyName;
        long accent = isMe ? scheme.Primary : scheme.Tertiary;
        string userId = isMe ? Profiles.MeProfile.UserId : Profiles.ColleagueProfile.UserId;
        return new Image(m.AuthorImage, "Profile photo")
        {
            Modifier = Modifier.Companion
                .Padding(horizontal: 16, vertical: 0)
                .Size(42)
                .Border(1.5f, new Color(accent),         Shape.Circle())
                .Border(3,    new Color(scheme.Surface), Shape.Circle())
                .Clip(21)
                .Clickable(() => onAuthorClicked(userId)),
        };
    }

    static Column BuildAuthorAndTextMessage(Message m, bool isFirstByAuthor, bool isLastByAuthor, ColorScheme scheme)
    {
        var col = new Column
        {
            Modifier.Companion.Padding(top: 0, bottom: 0, start: 0, end: 16).Weight(1f, fill: true),
        };
        if (isLastByAuthor)
            col.Add(BuildAuthorNameTimestamp(m, scheme));
        col.Add(BuildChatItemBubble(m, scheme));
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

    static ComposableNode BuildChatItemBubble(Message m, ColorScheme scheme)
    {
        bool isMe = m.Author == MyName;
        long bg   = isMe ? scheme.Primary : scheme.SurfaceVariant;
        long fg   = isMe ? scheme.OnPrimary : scheme.OnSurface;
        return new Text(m.Content)
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
