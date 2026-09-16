using AndroidX.Compose.Material3;
using AndroidX.Compose.Samples.Jetchat.Theme;
using AndroidX.Compose.UI.Text.Input;
using Baselines = AndroidX.Compose.UI.Layout.AlignmentLineKt;
using Typography = AndroidX.Compose.Samples.Jetchat.Theme.Typography;

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
        MutableState<string>         selectedMenu,
        MutableState<bool>           popupOpen,
        LazyListState                messagesScroll,
        MutableState<bool>           isRecording,
        MutableNumberState<float>    swipeOffset,
        Action                       onOpenDrawer,
        Action<string>               onAuthorClicked) =>
        new Composed(c =>
        {
            var input = c.RememberSaveable(
                () => new MutableState<TextFieldValue>(ComposeExtensions.NewTextFieldValue()),
                key1: ui.ChannelName);
            var selectedSelector = c.RememberSaveable(
                () => new MutableState<int>(0), key1: ui.ChannelName);
            var scheme          = c.ColorScheme();
            var topBarState     = c.RememberTopAppBarState();
            var scrollBehavior  = c.PinnedScrollBehavior(topBarState);
            var root = new Column
            {
                Modifier.FillMaxSize(),
                new Scaffold
                {
                    Modifier = Modifier.NestedScroll(scrollBehavior.NestedScrollConnection),
                    ContentWindowInsets = c.ScaffoldContentWindowInsets()
                        .Exclude(c.NavigationBarsInsets())
                        .Exclude(c.ImeInsets()),
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
                    FontFamily = JetchatFonts.Montserrat,
                    Color      = Color.FromPacked(scheme.OnSurface),
                }.WithTypography(Typography.TitleMedium),
                new Text($"{ui.ChannelMembers} members")
                {
                    FontFamily = JetchatFonts.Karla,
                    Color    = Color.FromPacked(scheme.OnSurfaceVariant),
                    Modifier = Modifier.Padding(top: 2),
                }.WithTypography(Typography.BodySmall),
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
                var densityValue = AndroidX.Compose.UI.Platform.CompositionLocalsKt.LocalDensity.GetCurrent(c, 0)
                    ?? throw new InvalidOperationException("LocalDensity was unavailable in the Jetchat conversation.");
                var density = Android.Runtime.Extensions.JavaCast<AndroidX.Compose.UI.Unit.IDensity>(densityValue);
                int jumpThreshold = (int)(56 * density.Density);
                var visible = c.Remember(
                    () => ComposeExtensions.DerivedStateOf(
                        () => messagesScroll.FirstVisibleItemIndex != 0
                            || messagesScroll.FirstVisibleItemScrollOffset > jumpThreshold),
                    key1: messagesScroll,
                    key2: jumpThreshold);
                var transition = c.UpdateTransition(visible.Value, "Jump to bottom");
                var motion = c.Remember(() => AnimationSpecs.Tween(200));
                float bottomOffset = transition.AnimateFloat(
                    c,
                    enabled => enabled ? 32f : -32f,
                    motion,
                    "Jump to bottom offset").Value;
                if (bottomOffset <= 0)
                    return null;

                return new ExtendedFloatingActionButton(
                    onClick:  () => _ = messagesScroll.AnimateScrollToItemAsync(0),
                    expanded: true)
                {
                    Modifier = Modifier
                        .Align(Alignment.BottomCenter)
                        .Offset(y: -bottomOffset)
                        .Height(36),
                    ContainerColor = Color.FromPacked(scheme.Surface),
                    ContentColor = Color.FromPacked(scheme.Primary),
                    Icon = new Icon(Resource.Drawable.ic_arrow_downward, "Jump to latest message")
                    {
                        Tint = Color.FromPacked(scheme.Primary),
                        Modifier = Modifier.Height(18),
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
                FontFamily = JetchatFonts.Montserrat,
                Color      = Color.FromPacked(scheme.OnSurfaceVariant),
                Modifier   = Modifier.Padding(horizontal: 16),
            }.WithTypography(Typography.LabelSmall),
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
                FontFamily = JetchatFonts.Montserrat,
                Color      = Color.FromPacked(scheme.OnSurface),
                Modifier   = Modifier
                    .AlignBy(Baselines.LastBaseline)
                    .PaddingFrom(Baselines.LastBaseline, after: 8),
            }.WithTypography(Typography.TitleMedium),
            Spacer.Width(8),
            new Text(m.Timestamp)
            {
                FontFamily = JetchatFonts.Karla,
                Color    = Color.FromPacked(scheme.OnSurfaceVariant),
                Modifier = Modifier.AlignBy(Baselines.LastBaseline),
            }.WithTypography(Typography.BodySmall),
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
                FontFamily = JetchatFonts.Karla,
                Color    = fg,
                Modifier = Modifier
                    .Background(bg, new RoundedCornerShape(4.Dp(), 20.Dp(), 20.Dp(), 20.Dp()))
                    .Padding(horizontal: 16, vertical: 16),
            }.WithTypography(Typography.BodyLarge),
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

    static ComposableNode BuildInputArea(
        ConversationUiState          ui,
        MutableState<TextFieldValue> input,
        ColorScheme                  scheme,
        MutableState<int>            selectedSelector,
        LazyListState                messagesScroll,
        MutableState<bool>           isRecording,
        MutableNumberState<float>    swipeOffset) =>
        new Composed(c =>
        {
            var focused = c.MutableStateOf(false);
            long cursorColor = scheme.Secondary;
            var cursorBrush = c.Remember(
                () => Brush.SolidColor(Color.FromPacked(cursorColor)), key1: cursorColor);
            var keyboardActions = c.Remember(() => KeyboardActionsHelper.Create(
                onSend: () => Send(ui, input, selectedSelector, messagesScroll)),
                key1: input, key2: selectedSelector, key3: ui);
            var selectorFocus = c.Remember(() => new FocusRequester());
            int selector = selectedSelector.Value;
            c.LaunchedEffect(selector, _ =>
            {
                if (selector == SelEmoji && selectedSelector.Value == selector)
                    selectorFocus.RequestFocus();
                return Task.CompletedTask;
            });
            var surface = new Surface
            {
                TonalElevation = 2,
                ContentColor = Color.FromPacked(scheme.Secondary),
                Modifier = Modifier.FillMaxWidth(),
            };
            surface.Add(new Column
            {
                // Keep the Surface behind the bars; its content owns these insets once.
                Modifier.FillMaxWidth().NavigationBarsPadding().ImePadding(),
                BuildTextFieldRow(input, scheme, isRecording, swipeOffset, cursorBrush, keyboardActions, focus =>
                {
                    if (focused.Value == focus.IsFocused)
                        return;
                    focused.Value = focus.IsFocused;
                    if (focus.IsFocused)
                    {
                        selectedSelector.Value = 0;
                        _ = messagesScroll.AnimateScrollToItemAsync(0);
                    }
                }, focused.Value),
                BuildSelectorRow(ui, input, scheme, selectedSelector, messagesScroll),
                BuildSelectorPanel(input, scheme, selectedSelector, selectorFocus),
            });
            return surface;
        });

    static Row BuildTextFieldRow(
        MutableState<TextFieldValue> input,
        ColorScheme                  scheme,
        MutableState<bool>           isRecording,
        MutableNumberState<float>    swipeOffset,
        AndroidX.Compose.UI.Graphics.Brush cursorBrush,
        AndroidX.Compose.Foundation.Text.KeyboardActions keyboardActions,
        Action<FocusState> onFocusChanged,
        bool focused)
    {
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
                        : new BasicTextField(input.Value, value => input.Value = value, maxLines: 1)
                          {
                              Modifier = Modifier
                                  .FillMaxWidth()
                                  .Padding(start: 32)
                                  .OnFocusChanged(onFocusChanged)
                                  .Semantics("Message"),
                              TextStyle = new TextStyle
                              {
                                  FontFamily = JetchatFonts.Karla,
                                  FontSize = Typography.BodyLarge.FontSize,
                                  LineHeight = Typography.BodyLarge.LineHeight,
                                  LetterSpacing = Typography.BodyLarge.LetterSpacing,
                                  FontWeight = Typography.BodyLarge.FontWeight,
                                  Color = Color.FromPacked(scheme.Secondary),
                              },
                              CursorBrush = cursorBrush,
                              DecorationBox = inner => new Box
                              {
                                  Modifier.FillMaxWidth().Height(64),
                                  new Box { Modifier.Align(Alignment.CenterStart), inner },
                                  input.Value.Text.Length == 0 && !focused
                                      ? new Text("Message #composers")
                                      {
                                          Modifier = Modifier.Align(Alignment.CenterStart),
                                          FontFamily = JetchatFonts.Karla,
                                          Color = Color.FromPacked(scheme.OnSurfaceVariant),
                                      }.WithTypography(Typography.BodyLarge)
                                      : null,
                              },
                              KeyboardOptions = CreateMessageKeyboardOptions(),
                              KeyboardActions = keyboardActions,
                          }),
            },
        };

        row.Add(new Tooltip
        {
            Modifier = Modifier.Align(Alignment.Vertical.CenterVertically),
            EnableUserInput = false,
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
        return row;
    }

    static Row BuildSelectorRow(
        ConversationUiState  ui,
        MutableState<TextFieldValue> input,
        ColorScheme          scheme,
        MutableState<int>    selectedSelector,
        LazyListState        messagesScroll)
    {
        var row = new Row(
            horizontalArrangement: null,
            verticalAlignment: Alignment.Vertical.CenterVertically)
        {
            Modifier
                .FillMaxWidth()
                .Height(72)
                .Padding(start: 16, end: 16, bottom: 16),
            InputSelectorButton(Resource.Drawable.ic_mood,            "Show Emoji selector", SelEmoji,   selectedSelector, scheme),
            InputSelectorButton(Resource.Drawable.ic_alternate_email, "Direct Message",      SelDm,      selectedSelector, scheme),
            InputSelectorButton(Resource.Drawable.ic_insert_photo,    "Attach Photo",        SelPicture, selectedSelector, scheme),
            InputSelectorButton(Resource.Drawable.ic_place,           "Location selector",   SelMap,     selectedSelector, scheme),
            InputSelectorButton(Resource.Drawable.ic_duo,             "Start videochat",     SelPhone,   selectedSelector, scheme),
            new Spacer
            {
                Modifier = Modifier.Weight(1f),
            },
        };
        bool enabled = !string.IsNullOrWhiteSpace(input.Value.Text);
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
            ContentPadding = new PaddingValues(0),
            Colors = ComposableContext.Current.ButtonColors(
                containerColor: Color.FromPacked(scheme.Primary),
                contentColor: Color.FromPacked(scheme.OnPrimary),
                disabledContainerColor: Color.Transparent,
                disabledContentColor: Color.FromPacked(scheme.OnSurface).WithAlpha(77)),
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
            Tint = Color.FromPacked(selected ? scheme.OnSecondary : scheme.Secondary),
        });
        if (selected)
            button.Modifier = Modifier
                .Size(48)
                .Background(
                    Color.FromPacked(scheme.Secondary),
                    new RoundedCornerShape(14.Dp()));
        else
            button.Modifier = Modifier.Size(48);
        return button;
    }

    static ComposableNode BuildSelectorPanel(
        MutableState<TextFieldValue> input,
        ColorScheme          scheme,
        MutableState<int>    selectedSelector,
        FocusRequester      selectorFocus)
    {
        int sel = selectedSelector.Value;
        var surface = new Surface
        {
            TonalElevation = 8,
            Modifier = Modifier.FillMaxWidth(),
        };
        if (sel == SelEmoji)
        {
            surface.Add(EmojiSelector.Build(input, scheme, selectorFocus));
            return surface;
        }

        if (sel == SelDm)
        {
            return new AlertDialog(onDismissRequest: () => selectedSelector.Value = 0)
            {
                Text = new Text("Functionality not available \U0001F648"),
                ConfirmButton = new TextButton(() => selectedSelector.Value = 0)
                {
                    new Text("CLOSE"),
                },
            };
        }

        bool showUnavailable = sel is SelPicture or SelMap or SelPhone;
        string title    = "Functionality currently not available";
        string subtitle = "Grab a beverage and check back later!";
        surface.Add(new AnimatedVisibility(
            showUnavailable,
            enter: Transitions.FadeIn(),
            exit: Transitions.FadeOut())
        {
            new Column(
                verticalArrangement: Arrangement.Center,
                horizontalAlignment: Alignment.Horizontal.CenterHorizontally)
            {
                Modifier
                    .FillMaxWidth()
                    .Height(320)
                    .AnimateEnterExit(
                        enter: Transitions.SlideInHorizontally(width => -width),
                        exit: Transitions.SlideOutHorizontally(width => -width)),
                new Text(title)
                {
                    FontFamily = JetchatFonts.Montserrat,
                    Color = Color.FromPacked(scheme.OnSurface),
                }.WithTypography(Typography.TitleMedium),
                new Text(subtitle)
                {
                    FontFamily = JetchatFonts.Montserrat,
                    Color = Color.FromPacked(scheme.OnSurfaceVariant),
                    Modifier = Modifier.PaddingFrom(Baselines.FirstBaseline, before: 32),
                }.WithTypography(Typography.BodyMedium),
            },
        });
        return surface;
    }


    static void Send(
        ConversationUiState  ui,
        MutableState<TextFieldValue> input,
        MutableState<int>    selectedSelector,
        LazyListState        messagesScroll)
    {
        MessageInput.Send(input,
            text => ui.AddMessage(new Message(MyName, text, "8:30 PM")),
            () => _ = messagesScroll.AnimateScrollToItemAsync(0),
            () => selectedSelector.Value = 0);
    }

}
