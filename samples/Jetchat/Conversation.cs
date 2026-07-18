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
    static readonly Func<DragAndDropEvent, bool> ShouldAcceptDrag = e =>
    {
        foreach (var mimeType in e.MimeTypes)
            if (mimeType == "text/plain" || mimeType.StartsWith("image/", StringComparison.Ordinal))
                return true;
        return false;
    };

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
            var scheme          = c.ColorScheme();
            var topBarState     = c.RememberTopAppBarState();
            var scrollBehavior  = c.PinnedScrollBehavior(topBarState);
            var root = new Column
            {
                Modifier.FillMaxSize(),
                new Scaffold
                {
                    Modifier = Modifier.NestedScroll(scrollBehavior.NestedScrollConnection),
                    TopBar = BuildTopBar(ui, scheme, onOpenDrawer, popupOpen, scrollBehavior),
                    Body   = BuildBody(ui, input, scheme, selectedSelector, messagesScroll, onAuthorClicked, isRecording, swipeOffset),
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
        MutableState<bool>  popupOpen,
        AndroidX.Compose.Material3.ITopAppBarScrollBehavior scrollBehavior) =>
        new()
        {
            ScrollBehavior = scrollBehavior,
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
                    Color      = Color.FromPacked(scheme.OnSurface),
                },
                new Text($"{ui.ChannelMembers} members")
                {
                    FontSize = 12,
                    Color    = Color.FromPacked(scheme.OnSurfaceVariant),
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
                    Tint = Color.FromPacked(scheme.OnSurfaceVariant),
                },
                new Icon(Resource.Drawable.ic_info, "Information")
                {
                    Modifier = Modifier
                        .Clickable(() => popupOpen.Value = true)
                        .Padding(horizontal: 12, vertical: 16)
                        .Height(24),
                    Tint = Color.FromPacked(scheme.OnSurfaceVariant),
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
        Action<string>               onAuthorClicked,
        MutableState<bool>           isRecording,
        MutableNumberState<float>    swipeOffset) =>
        new Composed(c =>
        {
            var dragBackground = c.MutableStateOf(Color.Transparent.ToPacked());
            var dragBorder     = c.MutableStateOf(Color.Transparent.ToPacked());
            var dndTarget      = c.Remember(() => new DragAndDropTarget());

            dndTarget.OnDrop = e =>
            {
                var clip = e.AndroidDragEvent.ClipData;
                var item = clip is not null && clip.ItemCount > 0
                    ? clip.GetItemAt(0)
                    : null;
                var content = item?.Text?.ToString() ?? item?.Uri?.ToString();
                if (string.IsNullOrWhiteSpace(content))
                    return false;

                ui.AddMessage(new Message(MyName, content, "now"));
                _ = messagesScroll.AnimateScrollToItemAsync(0);
                return true;
            };
            dndTarget.OnStarted = _ => dragBorder.Value = Color.Red.ToPacked();
            dndTarget.OnEntered = _ => dragBackground.Value = Color.Red.WithAlpha(77).ToPacked();
            dndTarget.OnExited  = _ => dragBackground.Value = Color.Transparent.ToPacked();
            dndTarget.OnEnded   = _ =>
            {
                dragBackground.Value = Color.Transparent.ToPacked();
                dragBorder.Value     = Color.Transparent.ToPacked();
            };

            return new Column
            {
                Modifier
                    .FillMaxSize()
                    .Background(Color.FromPacked(dragBackground.Value))
                    .Border(2, Color.FromPacked(dragBorder.Value))
                    .DragAndDropTarget(
                    shouldStartDragAndDrop: ShouldAcceptDrag,
                    target: dndTarget),
                new BackHandler(
                    onBack:  () => selectedSelector.Value = 0,
                    enabled: selectedSelector.Value != 0),
                BuildMessages(ui, scheme, messagesScroll, onAuthorClicked),
                BuildInputArea(ui, input, scheme, selectedSelector, messagesScroll, isRecording, swipeOffset),
            };
        });

    // Flat row stream so LazyColumn<T> can render messages and day
    // headers as a single item list — same shape as upstream's
    // `for index in messages.indices { … item { … } }` loop.
    abstract record ChatRow;
    sealed record MessageRow(Message Msg, bool IsFirstByAuthor, bool IsLastByAuthor) : ChatRow;
    sealed record HeaderRow(string Label) : ChatRow;

    static ComposableNode BuildMessages(
        ConversationUiState ui,
        ColorScheme         scheme,
        LazyListState       messagesScroll,
        Action<string>      onAuthorClicked)
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
                    MessageRow mr => BuildMessageRow(mr.Msg, mr.IsFirstByAuthor, mr.IsLastByAuthor, scheme, onAuthorClicked),
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
            c =>
            {
                var visible = messagesScroll.FirstVisibleItemIndex != 0
                           || messagesScroll.FirstVisibleItemScrollOffset > DpToPx(56);
                if (!visible)
                    return null;

                return new ExtendedFloatingActionButton(
                    onClick:  () => _ = messagesScroll.AnimateScrollToItemAsync(0),
                    expanded: false)
                {
                    Modifier = Modifier
                        .Align(Alignment.BottomCenter)
                        .Padding(bottom: 16)
                        .Height(48)
                        .Semantics("Jump to latest message"),
                    Icon = new Icon(Resource.Drawable.ic_arrow_downward, "Jump to latest message")
                    {
                        Tint = Color.FromPacked(scheme.Primary),
                    },
                    Text = new Text("Jump to bottom"),
                };
            },
        };
    }

    static Row BuildDayHeader(string label, ColorScheme scheme) =>
        new(horizontalArrangement: null, verticalAlignment: Alignment.Vertical.CenterVertically)
        {
            // Deliberate deviation from upstream Kotlin's `.height(16.dp)`,
            // which clips the descender of "Today" / "y" against the divider.
            Modifier.Padding(horizontal: 16, vertical: 8),
            new HorizontalDivider
            {
                Modifier  = Modifier.Weight(1f),
                Color = Color.FromPacked(scheme.OnSurface).WithAlpha(31),
            },
            new Text(label)
            {
                FontSize   = 11,
                FontWeight = FontWeight.Medium,
                Color      = Color.FromPacked(scheme.OnSurfaceVariant),
                Modifier   = Modifier.Padding(horizontal: 16),
            },
            new HorizontalDivider
            {
                Modifier  = Modifier.Weight(1f),
                Color = Color.FromPacked(scheme.OnSurface).WithAlpha(31),
            },
        };

    static Row BuildMessageRow(
        Message        m,
        bool           isFirstByAuthor,
        bool           isLastByAuthor,
        ColorScheme    scheme,
        Action<string> onAuthorClicked)
    {
        var row = new Row
        {
            Modifier.Padding(top: isLastByAuthor ? 8 : 0),
        };

        if (isLastByAuthor)
            row.Add(BuildAvatar(m, scheme, onAuthorClicked));
        else
            row.Add(Spacer.Width(74));

        row.Add(BuildAuthorAndTextMessage(m, isFirstByAuthor, isLastByAuthor, scheme, onAuthorClicked));
        return row;
    }

    static Image BuildAvatar(Message m, ColorScheme scheme, Action<string> onAuthorClicked)
    {
        bool isMe = m.Author == MyName;
        var accent = Color.FromPacked(isMe ? scheme.Primary : scheme.Tertiary);
        string userId = isMe ? Profiles.MeProfile.UserId : Profiles.ColleagueProfile.UserId;
        return new Image(m.AuthorImage, "Profile photo")
        {
            Modifier = Modifier
                .Padding(horizontal: 16)
                .Size(42)
                .Border(1.5f, accent,         Shape.Circle())
                .Border(3, Color.FromPacked(scheme.Surface), Shape.Circle())
                .Clip(21)
                .Clickable(() => onAuthorClicked(userId)),
        };
    }

    static Column BuildAuthorAndTextMessage(
        Message        m,
        bool           isFirstByAuthor,
        bool           isLastByAuthor,
        ColorScheme    scheme,
        Action<string> onAuthorClicked)
    {
        var col = new Column
        {
            Modifier.Padding(end: 16).Weight(1f, fill: true),
        };
        if (isLastByAuthor)
            col.Add(BuildAuthorNameTimestamp(m, scheme));
        col.Add(BuildChatItemBubble(m, scheme, onAuthorClicked));
        col.Add(Spacer.Height(isFirstByAuthor ? 8 : 4));
        return col;
    }

    static Row BuildAuthorNameTimestamp(Message m, ColorScheme scheme) =>
        new()
        {
            Modifier.Semantics(mergeDescendants: true, properties: _ => { }),
            new Text(m.Author)
            {
                FontSize   = 16,
                FontWeight = FontWeight.Medium,
                Color      = Color.FromPacked(scheme.OnSurface),
                Modifier   = Modifier.Padding(bottom: 8),
            },
            Spacer.Width(8),
            new Text(m.Timestamp)
            {
                FontSize = 12,
                Color    = Color.FromPacked(scheme.OnSurfaceVariant),
                Modifier = Modifier.Padding(bottom: 8),
            },
        };

    static ComposableNode BuildChatItemBubble(
        Message        m,
        ColorScheme    scheme,
        Action<string> onAuthorClicked)
    {
        bool isMe = m.Author == MyName;
        var bg = Color.FromPacked(isMe ? scheme.Primary : scheme.SurfaceVariant);
        var fg = Color.FromPacked(isMe ? scheme.OnPrimary : scheme.OnSurface);
        var formatted = MessageFormatter.Format(
            m.Content,
            isMe,
            scheme,
            handle => onAuthorClicked(Profiles.GetById(handle).UserId));
        var content = new Column
        {
            new AnnotatedText(formatted)
            {
                Color    = fg,
                Modifier = Modifier
                    .Background(bg, new RoundedCornerShape(4.Dp(), 20.Dp(), 20.Dp(), 20.Dp()))
                    .Padding(horizontal: 16, vertical: 16),
            },
        };
        if (m.Image is int image)
        {
            content.Add(Spacer.Height(4));
            content.Add(new Image(image, "Attached image")
            {
                Modifier = Modifier
                    .Size(160)
                    .Background(bg, new RoundedCornerShape(4.Dp(), 20.Dp(), 20.Dp(), 20.Dp()))
                    .Clip(new RoundedCornerShape(4.Dp(), 20.Dp(), 20.Dp(), 20.Dp())),
            });
        }
        return content;
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
                        : new TextField(input, singleLine: true, maxLines: 1)
                          {
                              Modifier = Modifier
                                  .FillMaxWidth()
                                  .Semantics("Message"),
                              Placeholder = new Text("Type a message"),
                              KeyboardOptions = CreateMessageKeyboardOptions(),
                          }),
            },
        };

        if (textEmpty || isRecording.Value)
        {
            row.Add(new Tooltip
            {
                Tip = new Surface
                {
                    new Text("Touch and hold to record")
                    {
                        Modifier = Modifier.Padding(horizontal: 12, vertical: 8),
                    },
                },
                Anchor = RecordButton.BuildButton(
                    isRecording,
                    swipeOffset,
                    onCommit: () =>
                    {
                        isRecording.Value = false;
                        swipeOffset.Value = 0f;
                    },
                    onCancel: () =>
                    {
                        isRecording.Value = false;
                        swipeOffset.Value = 0f;
                    },
                    scheme: scheme),
            });
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
            Modifier.FillMaxWidth().Height(40).Padding(horizontal: 4),
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
        var sendModifier = Modifier.Height(36);
        if (!enabled)
        {
            sendModifier = sendModifier.Border(
                1,
                Color.FromPacked(scheme.OnSurface).WithAlpha(77),
                new RoundedCornerShape(18.Dp()));
        }
        var sendButton = new Button(
            onClick: () => Send(ui, input, selectedSelector, messagesScroll),
            enabled: enabled)
        {
            Modifier = sendModifier,
            Shape = new RoundedCornerShape(18.Dp()),
            Colors = ComposableContext.Current.ButtonColors(
                containerColor: Color.FromPacked(scheme.Primary),
                contentColor: Color.FromPacked(scheme.OnPrimary),
                disabledContainerColor: Color.Transparent,
                disabledContentColor: Color.FromPacked(scheme.OnSurfaceVariant)),
        };
        sendButton.Add(new Text("Send")
            {
                FontWeight = FontWeight.SemiBold,
            });
        row.Add(sendButton);
        return row;
    }

    static AndroidX.Compose.Foundation.Text.KeyboardOptions CreateMessageKeyboardOptions()
    {
        var defaults = KeyboardOptionsCompanion.Default;
        return defaults.Copy(
            defaults.Capitalization,
            defaults.AutoCorrectEnabled,
            KeyboardType.Text,
            AndroidX.Compose.ImeAction.Send,
            defaults.PlatformImeOptions,
            defaults.ShowKeyboardOnFocus,
            defaults.HintLocales);
    }

    static IconButton InputSelectorButton(
        int               drawableId,
        string            contentDescription,
        int               selectorId,
        MutableState<int> selectedSelector,
        ColorScheme       scheme,
        Action?           onSelected = null)
    {
        bool selected = selectedSelector.Value == selectorId;
        var button = new IconButton(onClick: () =>
        {
            selectedSelector.Value = selected ? 0 : selectorId;
            if (!selected)
                onSelected?.Invoke();
        })
        {
            Modifier = Modifier.Size(40),
        };
        button.Add(new Icon(drawableId, contentDescription)
        {
            Tint = Color.FromPacked(selected ? scheme.OnSecondary : scheme.OnSurface),
        });
        if (selected)
            button.Modifier = Modifier
                .Size(40)
                .Padding(4)
                .Background(
                    Color.FromPacked(scheme.Secondary),
                    new RoundedCornerShape(16.Dp()));
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
            Modifier.FillMaxWidth().Height(320)
                .Background(Color.FromPacked(scheme.SurfaceVariant)),
            Spacer.Height(96),
            new Text(title)
            {
                FontSize   = 16,
                FontWeight = FontWeight.Medium,
                Color      = Color.FromPacked(scheme.OnSurfaceVariant),
                Modifier   = Modifier.Padding(horizontal: 16),
            },
            Spacer.Height(8),
            new Text(subtitle)
            {
                FontSize = 14,
                Color    = Color.FromPacked(scheme.OnSurfaceVariant),
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

    static int DpToPx(int value)
    {
        var resources = Android.Content.Res.Resources.System
            ?? throw new InvalidOperationException("Android system resources were unavailable in Jetchat.");
        var metrics = resources.DisplayMetrics
            ?? throw new InvalidOperationException("Android display metrics were unavailable in Jetchat.");
        return (int)(value * metrics.Density);
    }
}
