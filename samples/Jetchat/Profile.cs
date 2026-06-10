using AndroidX.Compose.Material3;

namespace AndroidX.Compose.Samples.Jetchat;

/// <summary>
/// Builds the Jetchat profile tree. C# port of upstream's
/// <c>ProfileScreen</c> + <c>UserInfoFields</c> +
/// <c>ProfileProperty</c> + <c>ProfileFab</c> in
/// <c>profile/Profile.kt</c>.
/// </summary>
public static class Profile
{
    /// <summary>Materialize the profile tree for one composition pass.</summary>
    public static ComposableNode Build(
        ProfileScreenState state,
        Action onBack) =>
        new Composed(c =>
        {
            var scrollState = c.Remember(() => new ScrollState());
            var popupOpen   = c.Remember(() => new MutableState<bool>(false));
            var scheme      = c.ColorScheme();

            var screen = new Scaffold
            {
                TopBar = BuildTopBar(scheme, onBack, popupOpen),
                Body   = BuildBody(state, scrollState, popupOpen, scheme),
            };

            var root = new Column
            {
                Modifier.Companion.FillMaxSize(),
                screen,
            };
            if (popupOpen.Value)
                root.Add(BuildFunctionalityPopup(popupOpen));
            return root;
        });

    static CenterAlignedTopAppBar BuildTopBar(
        ColorScheme         scheme,
        Action onBack,
        MutableState<bool>  popupOpen) =>
        new()
        {
            NavigationIcon = new IconButton(onClick: onBack)
            {
                new Icon(Resource.Drawable.ic_arrow_back, "Back")
                {
                    TintArgb = scheme.OnSurfaceVariant,
                },
            },
            Title   = new Text(""),
            Actions = new Row
            {
                new Icon(Resource.Drawable.ic_more_vert, "More options")
                {
                    Modifier = Modifier.Companion
                        .Clickable(() => popupOpen.Value = true)
                        .Padding(horizontal: 12, vertical: 16)
                        .Height(24),
                    TintArgb = scheme.OnSurfaceVariant,
                },
            },
        };

    static ComposableNode BuildBody(
        ProfileScreenState  state,
        ScrollState         scrollState,
        MutableState<bool>  popupOpen,
        ColorScheme         scheme) =>
        new BoxWithConstraints(constraints => new Box
        {
            Modifier.Companion.FillMaxSize(),
            new Surface
            {
                new Column
                {
                    Modifier.Companion.FillMaxSize().VerticalScroll(scrollState),
                    BuildProfileHeader(state, constraints.MaxHeight),
                    BuildUserInfoFields(state, constraints.MaxHeight, scheme),
                },
            },
            BuildProfileFab(state, scrollState, popupOpen, scheme),
        });

    static ComposableNode BuildProfileHeader(ProfileScreenState state, float containerHeight)
    {
        if (state.Photo is null)
            return new Spacer(Modifier.Companion.Width(0));

        float heroMax = containerHeight / 2f;
        if (heroMax < 1f) heroMax = 240f;
        return new Image(state.Photo.Value, "Profile photo")
        {
            Modifier = Modifier.Companion
                .HeightIn(max: heroMax)
                .FillMaxWidth()
                .Padding(horizontal: 16, vertical: 16)
                .Clip(120),
            ContentScale = ContentScale.Crop,
        };
    }

    static Column BuildUserInfoFields(ProfileScreenState state, float containerHeight, ColorScheme scheme)
    {
        var col = new Column
        {
            new Spacer(Modifier.Companion.Height(8)),
            BuildNameAndPosition(state, scheme),
            BuildProfileProperty("Display name", state.DisplayName, scheme),
            BuildProfileProperty("Status",       state.Status,      scheme),
            BuildProfileProperty("Twitter",      state.Twitter,     scheme, isLink: true),
        };
        if (state.TimeZone is not null)
            col.Add(BuildProfileProperty("Timezone", state.TimeZone, scheme));

        // Add a spacer that always shows part (320.dp) of the fields list regardless of
        // the device, in order to always leave some content at the top.
        float trailing = containerHeight - 320f;
        if (trailing < 0f) trailing = 0f;
        col.Add(new Spacer(Modifier.Companion.Height((int)trailing)));
        return col;
    }

    static Column BuildNameAndPosition(ProfileScreenState state, ColorScheme scheme) =>
        new()
        {
            Modifier.Companion.Padding(horizontal: 16, vertical: 0),
            new Text(state.Name)
            {
                FontSize   = 24,
                FontWeight = FontWeight.Medium,
                Color      = new Color(scheme.OnSurface),
                Modifier   = Modifier.Companion.Padding(top: 8, bottom: 0, start: 0, end: 0),
            },
            new Text(state.Position)
            {
                FontSize = 16,
                Color    = new Color(scheme.OnSurfaceVariant),
                Modifier = Modifier.Companion.Padding(top: 4, bottom: 20, start: 0, end: 0),
            },
        };

    static Column BuildProfileProperty(string label, string value, ColorScheme scheme, bool isLink = false) =>
        new()
        {
            Modifier.Companion.Padding(start: 16, end: 16, top: 0, bottom: 16),
            new HorizontalDivider
            {
                ColorArgb = scheme.OnSurface,
            },
            new Text(label)
            {
                FontSize = 12,
                Color    = new Color(scheme.OnSurfaceVariant),
                Modifier = Modifier.Companion.Padding(top: 8, bottom: 0, start: 0, end: 0),
            },
            new Text(value)
            {
                FontSize = 16,
                Color    = isLink ? new Color(scheme.Primary) : new Color(scheme.OnSurface),
                Modifier = Modifier.Companion.Padding(top: 4, bottom: 0, start: 0, end: 0),
            },
        };

    static ComposableNode BuildProfileFab(
        ProfileScreenState  state,
        ScrollState         scrollState,
        MutableState<bool>  popupOpen,
        ColorScheme         scheme)
    {
        bool isMe    = state.IsMe();
        bool expanded = scrollState.Value == 0;
        string label  = isMe ? "Edit profile" : "Message";
        int iconRes   = isMe ? Resource.Drawable.ic_create : Resource.Drawable.ic_chat;

        return new ExtendedFloatingActionButton(
            onClick:  () => popupOpen.Value = true,
            expanded: expanded)
        {
            Modifier = Modifier.Companion
                .Align(Alignment.BottomEnd)
                .Padding(16)
                .NavigationBarsPadding()
                .Height(48)
                .WidthIn(min: 48),
            Icon = new Icon(iconRes, label)
            {
                TintArgb = scheme.OnTertiaryContainer,
            },
            Text = new Text(label)
            {
                Color = new Color(scheme.OnTertiaryContainer),
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
}
