using AndroidX.Compose.Material3;
using AndroidX.Compose.UI.Text.Input;

namespace AndroidX.Compose.Samples.Jetchat;

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
        ConversationUiState          ui,
        MutableState<TextFieldValue> input,
        MutableState<string>         selectedMenu,
        MutableState<int>            selectedSelector,
        MutableState<bool>           popupOpen,
        LazyListState                messagesScroll,
        MutableState<bool>           isRecording,
        MutableNumberState<float>    swipeOffset,
        Action                       onOpenDrawer,
        Action<string>               onAuthorClicked) =>
        new Composed(c =>
        {
            var scheme = c.ColorScheme();
            var root   = new Column
            {
                Modifier.FillMaxSize(),
                new Scaffold
                {
                    TopBar = BuildTopBar(ui, scheme, onOpenDrawer, popupOpen),
                    Body   = BuildBody(ui, input, scheme, selectedSelector, messagesScroll, popupOpen, onAuthorClicked, isRecording, swipeOffset),
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
            NavigationIcon = new IconButton(onClick: () => onOpenDrawer())
            {
                JetchatIcon.Build("Open navigation drawer", sizeDp: 32),
            },
            Title = new Column
            {
                new Text(ui.ChannelName)
                {
                    FontSize   = 16,
                    FontWeight = FontWeight.Medium,
                    Color      = scheme.OnSurface,
                },
                new Text($"{ui.ChannelMembers} members")
                {
                    FontSize = 12,
                    Color    = scheme.OnSurfaceVariant,
                    Modifier = Modifier.Padding(top: 2),
                },
            },
            Actions = new Row
            {
                new Icon(Resource.Drawable.ic_search, "Search")
                {
                    Modifier = Modifier
                        .Clickable(() => popupOpen.Value = true)
                        .Padding(horizontal: 12, vertical: 16)
                        .Height(24),
                    Tint = scheme.OnSurfaceVariant,
                },
                new Icon(Resource.Drawable.ic_info, "Information")
                {
                    Modifier = Modifier
                        .Clickable(() => popupOpen.Value = true)
                        .Padding(horizontal: 12, vertical: 16)
                        .Height(24),
                    Tint = scheme.OnSurfaceVariant,
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

    static ComposableNode BuildBody(
        ConversationUiState          ui,
        MutableState<TextFieldValue> input,
        ColorScheme                  scheme,
        MutableState<int>            selectedSelector,
        LazyListState                messagesScroll,
        MutableState<bool>           popupOpen,
        Action<string>               onAuthorClicked,
        MutableState<bool>           isRecording,
        MutableNumberState<float>    swipeOffset) =>
        new Composed(c =>
        {
            var dndTarget = c.Remember(() => new DragAndDropTarget(e =>
            {
                var clip = e.AndroidDragEvent.ClipData;
                var uri  = clip is not null && clip.ItemCount > 0
                    ? clip.GetItemAt(0)?.Uri?.ToString()
                    : null;
                ui.AddMessage(new Message(MyName, $"[image dropped: {uri ?? "?"}]", "8:30 PM"));
                _ = messagesScroll.AnimateScrollToItemAsync(0);
                return true;
            }));
            return new Column
            {
                Modifier.FillMaxSize().DragAndDropTarget(
                    shouldStartDragAndDrop: e =>
                    {
                        foreach (var m in e.MimeTypes)
                            if (m.StartsWith("image/", StringComparison.Ordinal))
                                return true;
                        return false;
                    },
                    target: dndTarget),
                BuildMessages(ui, scheme, messagesScroll, popupOpen, onAuthorClicked),
                BuildInputArea(ui, input, scheme, selectedSelector, messagesScroll, isRecording, swipeOffset),
            };
        });

    // Flat row stream so LazyColumn<T> can render messages and day
    // headers as a single item list — same shape as upstream's
    // `for index in messages.indices { … item { … } }` loop.
    abstract record ChatRow;
    sealed record MessageRow(Message Msg, bool IsFirstByAuthor, bool IsLastByAuthor) : ChatRow;
    sealed record HeaderRow(string Label) : ChatRow;

    static ComposableNode BuildMessages(ConversationUiState ui, ColorScheme scheme, LazyListState messagesScroll, MutableState<bool> popupOpen, Action<string> onAuthorClicked)
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
            Modifier.FillMaxWidth().Weight(1f, fill: true),

            new LazyColumn<ChatRow>(
                items:       rows,
                itemContent: row => row switch
                {
                    MessageRow mr => BuildMessageRow(mr.Msg, mr.IsFirstByAuthor, mr.IsLastByAuthor, scheme, popupOpen, onAuthorClicked),
                    HeaderRow  hr => BuildDayHeader(hr.Label, scheme),
                    _             => Spacer.Width(0),
                })
            {
                Modifier      = Modifier.FillMaxSize(),
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
                    Modifier = Modifier
                        .Align(Alignment.BottomCenter)
                        .Padding(bottom: 16),
                    Icon = new Icon(Resource.Drawable.ic_arrow_downward, "Jump to bottom"),
                    Text = new Text("Jump to bottom"),
                };
            }),
        };
    }

    static Row BuildDayHeader(string label, ColorScheme scheme) =>
        new()
        {
            Modifier.Padding(horizontal: 16, vertical: 8).Height(16),
            new HorizontalDivider
            {
                Modifier  = Modifier.Weight(1f),
                Color = scheme.OnSurface,
            },
            new Text(label)
            {
                FontSize   = 11,
                FontWeight = FontWeight.Medium,
                Color      = scheme.OnSurfaceVariant,
                Modifier   = Modifier.Padding(horizontal: 16),
            },
            new HorizontalDivider
            {
                Modifier  = Modifier.Weight(1f),
                Color = scheme.OnSurface,
            },
        };

    static Row BuildMessageRow(Message m, bool isFirstByAuthor, bool isLastByAuthor, ColorScheme scheme, MutableState<bool> popupOpen, Action<string> onAuthorClicked)
    {
        var row = new Row
        {
            Modifier.Padding(top: isLastByAuthor ? 8 : 0),
        };

        if (isLastByAuthor)
            row.Add(BuildAvatar(m, scheme, onAuthorClicked));
        else
            row.Add(Spacer.Width(74));

        row.Add(BuildAuthorAndTextMessage(m, isFirstByAuthor, isLastByAuthor, scheme, popupOpen));
        return row;
    }

    static Image BuildAvatar(Message m, ColorScheme scheme, Action<string> onAuthorClicked)
    {
        bool isMe = m.Author == MyName;
        long accent = isMe ? scheme.Primary : scheme.Tertiary;
        string userId = isMe ? Profiles.MeProfile.UserId : Profiles.ColleagueProfile.UserId;
        return new Image(m.AuthorImage, "Profile photo")
        {
            Modifier = Modifier
                .Padding(horizontal: 16)
                .Size(42)
                .Border(1.5f, accent,         Shape.Circle())
                .Border(3,    scheme.Surface, Shape.Circle())
                .Clip(21)
                .Clickable(() => onAuthorClicked(userId)),
        };
    }

    static Column BuildAuthorAndTextMessage(Message m, bool isFirstByAuthor, bool isLastByAuthor, ColorScheme scheme, MutableState<bool> popupOpen)
    {
        var col = new Column
        {
            Modifier.Padding(end: 16).Weight(1f, fill: true),
        };
        if (isLastByAuthor)
            col.Add(BuildAuthorNameTimestamp(m, scheme));
        col.Add(BuildChatItemBubble(m, scheme, popupOpen));
        col.Add(Spacer.Height(isFirstByAuthor ? 8 : 4));
        return col;
    }

    static Row BuildAuthorNameTimestamp(Message m, ColorScheme scheme) =>
        new()
        {
            new Text(m.Author)
            {
                FontSize   = 16,
                FontWeight = FontWeight.Medium,
                Color      = scheme.OnSurface,
                Modifier   = Modifier.Padding(bottom: 8),
            },
            Spacer.Width(8),
            new Text(m.Timestamp)
            {
                FontSize = 12,
                Color    = scheme.OnSurfaceVariant,
                Modifier = Modifier.Padding(bottom: 8),
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
            Color    = fg,
            Modifier = Modifier
                .Background(bg, Shape.RoundedCorners(4, 20, 20, 20))
                .Padding(horizontal: 16, vertical: 16),
        };
    }

    static Surface BuildInputArea(
        ConversationUiState          ui,
        MutableState<TextFieldValue> input,
        ColorScheme                  scheme,
        MutableState<int>            selectedSelector,
        LazyListState                messagesScroll,
        MutableState<bool>           isRecording,
        MutableNumberState<float>    swipeOffset) =>
        new()
        {
            Modifier.FillMaxWidth().NavigationBarsPadding().ImePadding(),
            new Column
            {
                Modifier.FillMaxWidth(),
                BuildTextFieldRow(input, scheme, isRecording, swipeOffset),
                BuildSelectorRow(ui, input, scheme, selectedSelector, messagesScroll),
                BuildSelectorPanel(input, scheme, selectedSelector),
            },
        };

    static Row BuildTextFieldRow(
        MutableState<TextFieldValue> input,
        ColorScheme                  scheme,
        MutableState<bool>           isRecording,
        MutableNumberState<float>    swipeOffset)
    {
        bool textEmpty = string.IsNullOrWhiteSpace(input.Value.Text);

        var row = new Row
        {
            Modifier.FillMaxWidth().Height(64),
            new Box
            {
                Modifier.Weight(1f, fill: true).FillMaxHeight(),
                new AnimatedContent<bool>(
                    targetState: isRecording.Value,
                    content: recording => recording
                        ? RecordButton.BuildRecordingIndicator(swipeOffset, scheme)
                        : new TextField(input)
                          {
                              Modifier = Modifier.FillMaxWidth(),
                          }),
            },
        };

        if (textEmpty || isRecording.Value)
        {
            row.Add(RecordButton.BuildButton(
                isRecording,
                swipeOffset,
                onCommit: () =>
                {
                    isRecording.Value  = false;
                    swipeOffset.Value  = 0f;
                },
                onCancel: () =>
                {
                    isRecording.Value  = false;
                    swipeOffset.Value  = 0f;
                },
                scheme: scheme));
        }
        return row;
    }

    static Row BuildSelectorRow(
        ConversationUiState  ui,
        MutableState<TextFieldValue> input,
        ColorScheme          scheme,
        MutableState<int>    selectedSelector,
        LazyListState        messagesScroll)
    {
        var row = new Row(Arrangement.SpaceBetween)
        {
            Modifier.FillMaxWidth().Padding(horizontal: 4, vertical: 4),
            new Row
            {
                InputSelectorButton(Resource.Drawable.ic_mood,            "Show Emoji selector", SelEmoji,   selectedSelector, scheme),
                InputSelectorButton(Resource.Drawable.ic_alternate_email, "Direct Message",      SelDm,      selectedSelector, scheme),
                InputSelectorButton(Resource.Drawable.ic_insert_photo,    "Attach Photo",        SelPicture, selectedSelector, scheme),
                InputSelectorButton(Resource.Drawable.ic_place,           "Location selector",   SelMap,     selectedSelector, scheme),
                InputSelectorButton(Resource.Drawable.ic_duo,             "Start videochat",     SelPhone,   selectedSelector, scheme),
            },
        };
        bool enabled = !string.IsNullOrWhiteSpace(input.Value?.Text);
        row.Add(new TextButton(onClick: () => Send(ui, input, selectedSelector, messagesScroll))
        {
            new Text("Send")
            {
                FontWeight = FontWeight.SemiBold,
                Color      = enabled ? scheme.Primary : scheme.OnSurfaceVariant,
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
                Tint = selected ? scheme.OnSecondary : scheme.OnSurface,
            },
        };
        if (selected)
            button.Modifier = Modifier.Background(scheme.Secondary, Shape.RoundedCorners(14, 14, 14, 14));
        return button;
    }

    static ComposableNode BuildSelectorPanel(
        MutableState<TextFieldValue> input,
        ColorScheme          scheme,
        MutableState<int>    selectedSelector)
    {
        int sel = selectedSelector.Value;
        if (sel == 0) return Spacer.Width(0);
        if (sel == SelEmoji) return EmojiSelector.Build(input, scheme);
        string title    = "Functionality currently not available";
        string subtitle = "Grab a beverage and check back later!";
        return new Column
        {
            Modifier.FillMaxWidth().Height(320).Background(scheme.SurfaceVariant),
            Spacer.Height(96),
            new Text(title)
            {
                FontSize   = 16,
                FontWeight = FontWeight.Medium,
                Color      = scheme.OnSurfaceVariant,
                Modifier   = Modifier.Padding(horizontal: 16),
            },
            Spacer.Height(8),
            new Text(subtitle)
            {
                FontSize = 14,
                Color    = scheme.OnSurfaceVariant,
                Modifier = Modifier.Padding(horizontal: 16),
            },
        };
    }


    static void Send(
        ConversationUiState  ui,
        MutableState<TextFieldValue> input,
        MutableState<int>    selectedSelector,
        LazyListState        messagesScroll)
    {
        var text = input.Value?.Text ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text)) return;
        ui.AddMessage(new Message(MyName, text.Trim(), "8:30 PM"));
        input.Value = ComposeExtensions.NewTextFieldValue();
        selectedSelector.Value = 0;
        _ = messagesScroll.AnimateScrollToItemAsync(0);
    }
}
