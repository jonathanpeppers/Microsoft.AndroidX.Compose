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
            var popupOpen   = c.MutableStateOf(false);
            var scheme      = c.ColorScheme();

            var screen = new Scaffold
            {
                TopBar = BuildTopBar(scheme, onBack, popupOpen),
                Body   = BuildBody(state, scrollState, popupOpen, scheme),
            };

            var root = new Column
            {
                Modifier.FillMaxSize(),
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
                    Tint = Color.FromPacked(scheme.OnSurfaceVariant),
                },
            },
            Title   = new Text(""),
            Actions = new Row
            {
                new Icon(Resource.Drawable.ic_more_vert, "More options")
                {
                    Modifier = Modifier
                        .Clickable(() => popupOpen.Value = true)
                        .Padding(horizontal: 12, vertical: 16)
                        .Height(24),
                    Tint = Color.FromPacked(scheme.OnSurfaceVariant),
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
            Modifier.FillMaxSize(),
            new Surface
            {
                new Column
                {
                    Modifier.FillMaxSize().VerticalScroll(scrollState),
                    BuildProfileHeader(state, constraints.MaxHeight, scrollState),
                    BuildUserInfoFields(state, constraints.MaxHeight, scheme),
                },
            },
            BuildProfileFab(state, scrollState, popupOpen, scheme),
        });

    static ComposableNode BuildProfileHeader(
        ProfileScreenState state,
        Dp containerHeight,
        ScrollState scrollState)
    {
        if (state.Photo is null)
            return Spacer.Width(0);

        var resources = Android.Content.Res.Resources.System
            ?? throw new InvalidOperationException("Android system resources were unavailable in Jetchat.");
        var metrics = resources.DisplayMetrics
            ?? throw new InvalidOperationException("Android display metrics were unavailable in Jetchat.");
        Dp heroMax = containerHeight / 2f;
        if (heroMax < 1) heroMax = 240;
        var parallaxOffset = new Dp(scrollState.Value / metrics.Density / 2f);
        return new Image(state.Photo.Value, "Profile photo")
        {
            Modifier = Modifier
                .HeightIn(max: heroMax)
                .FillMaxWidth()
                .Padding(start: 16, top: parallaxOffset, end: 16)
                .Clip(120),
            ContentScale = ContentScale.Crop,
        };
    }

    static Column BuildUserInfoFields(ProfileScreenState state, Dp containerHeight, ColorScheme scheme)
    {
        var col = new Column
        {
            Spacer.Height(8),
            BuildNameAndPosition(state, scheme),
            BuildProfileProperty("Display name", state.DisplayName, scheme),
            BuildProfileProperty("Status",       state.Status,      scheme),
            BuildProfileProperty("Twitter",      state.Twitter,     scheme, isLink: true),
        };
        if (state.TimeZone is not null)
            col.Add(BuildProfileProperty("Timezone", state.TimeZone, scheme));

        // Add a spacer that always shows part (320.dp) of the fields list regardless of
        // the device, in order to always leave some content at the top.
        Dp trailing = containerHeight - new Dp(320);
        if (trailing < 0) trailing = Dp.Zero;
        col.Add(Spacer.Height(trailing));
        return col;
    }

    static Column BuildNameAndPosition(ProfileScreenState state, ColorScheme scheme) =>
        new()
        {
            Modifier.Padding(horizontal: 16),
            new Text(state.Name)
            {
                FontSize   = 24,
                FontWeight = FontWeight.Medium,
                Color      = Color.FromPacked(scheme.OnSurface),
                Modifier   = Modifier.Padding(top: 8),
            },
            new Text(state.Position)
            {
                FontSize = 16,
                Color    = Color.FromPacked(scheme.OnSurfaceVariant),
                Modifier = Modifier.Padding(top: 4, bottom: 20),
            },
        };

    static Column BuildProfileProperty(string label, string value, ColorScheme scheme, bool isLink = false) =>
        new()
        {
            Modifier.FillMaxWidth().Padding(start: 16, end: 16, bottom: 16),
            new HorizontalDivider(),
            new Text(label)
            {
                FontSize = 12,
                Color    = Color.FromPacked(scheme.OnSurfaceVariant),
                Modifier = Modifier.Padding(top: 8),
            },
            new Text(value)
            {
                FontSize = 16,
                Color = Color.FromPacked(
                    isLink ? scheme.Primary : scheme.OnSurface),
                Modifier = Modifier.Padding(top: 4),
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
            Modifier = Modifier
                .Align(Alignment.BottomEnd)
                .Padding(16)
                .NavigationBarsPadding()
                .Height(48)
                .WidthIn(min: 48),
            Icon = new Icon(iconRes, label)
            {
                Tint = Color.FromPacked(scheme.OnPrimaryContainer),
            },
            Text = new Text(label)
            {
                Color = Color.FromPacked(scheme.OnPrimaryContainer),
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
