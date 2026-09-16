using AndroidX.Compose.Animation.Core;
using AndroidX.Compose.Material3;
using AndroidX.Compose.Samples.Jetchat.Theme;
using Baselines = AndroidX.Compose.UI.Layout.AlignmentLineKt;
using Typography = AndroidX.Compose.Samples.Jetchat.Theme.Typography;

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
                .Clip(Shape.Circle()),
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
                FontFamily = JetchatFonts.Montserrat,
                Color      = Color.FromPacked(scheme.OnSurface),
                Modifier   = Modifier.PaddingFrom(Baselines.FirstBaseline, before: 32),
            }.WithTypography(Typography.HeadlineSmall),
            new Text(state.Position)
            {
                FontFamily = JetchatFonts.Karla,
                Color    = Color.FromPacked(scheme.OnSurfaceVariant),
                Modifier = Modifier
                    .Padding(bottom: 20)
                    .PaddingFrom(Baselines.FirstBaseline, before: 24),
            }.WithTypography(Typography.BodyLarge),
        };

    static Column BuildProfileProperty(string label, string value, ColorScheme scheme, bool isLink = false) =>
        new()
        {
            Modifier.FillMaxWidth().Padding(start: 16, end: 16, bottom: 16),
            new HorizontalDivider(),
            new Text(label)
            {
                FontFamily = JetchatFonts.Karla,
                Color    = Color.FromPacked(scheme.OnSurfaceVariant),
                Modifier = Modifier.PaddingFrom(Baselines.FirstBaseline, before: 24),
            }.WithTypography(Typography.BodySmall),
            new Text(value)
            {
                FontFamily = JetchatFonts.Karla,
                Color = Color.FromPacked(
                    isLink ? scheme.Primary : scheme.OnSurface),
                Modifier = Modifier.PaddingFrom(Baselines.FirstBaseline, before: 24),
            }.WithTypography(Typography.BodyLarge),
        };

    static ComposableNode BuildProfileFab(
        ProfileScreenState  state,
        ScrollState         scrollState,
        MutableState<bool>  popupOpen,
        ColorScheme         scheme) =>
        new Composed(c =>
        {
            bool isMe      = state.IsMe();
            bool expanded  = scrollState.Value == 0;
            string label   = isMe ? "Edit profile" : "Message";
            int iconRes    = isMe ? Resource.Drawable.ic_create : Resource.Drawable.ic_chat;
            var transition = c.UpdateTransition(expanded, "Profile FAB");
            var fadeInSpec = c.Remember(() => AnimationSpecs.Tween(83, 67, EasingKt.LinearEasing));
            var fadeOutSpec = c.Remember(() => AnimationSpecs.Tween(83, easing: EasingKt.LinearEasing));
            var widthSpec = c.Remember(() => AnimationSpecs.Tween(200));
            var opacity = transition.AnimateFloat(
                c,
                value => value ? 1f : 0f,
                expanded ? fadeInSpec : fadeOutSpec,
                "Profile FAB text opacity");
            var widthFactor = transition.AnimateFloat(
                c,
                value => value ? 1f : 0f,
                widthSpec,
                "Profile FAB width");

            var fab = new FloatingActionButton(onClick: () => popupOpen.Value = true)
            {
                Modifier = Modifier
                    .Align(Alignment.BottomEnd)
                    .Padding(16)
                    .NavigationBarsPadding()
                    .Height(48)
                    .WidthIn(min: 48),
                ContainerColor = Color.FromPacked(scheme.TertiaryContainer),
            };
            var content = new Layout((scope, measurables, constraints) =>
            {
                if (measurables.Count != 2)
                    throw new InvalidOperationException("Profile FAB content requires exactly one icon and one label.");

                var icon = measurables[0].Measure(constraints);
                var text = measurables[1].Measure(constraints);
                int height = constraints.HasBoundedHeight
                    ? constraints.MaxHeight
                    : Math.Max(icon.Height, text.Height);
                float iconPadding = (height - icon.Width) / 2f;
                float expandedWidth = icon.Width + text.Width + iconPadding * 3f;
                int width = constraints.ConstrainWidth((int)MathF.Round(
                    height + (expandedWidth - height) * widthFactor.Value));

                return scope.Layout(width, constraints.ConstrainHeight(height), placement =>
                {
                    placement.PlaceRelative(
                        icon,
                        (int)MathF.Round(iconPadding),
                        height / 2 - icon.Height / 2);
                    placement.PlaceRelative(
                        text,
                        (int)MathF.Round(icon.Width + iconPadding * 2f),
                        height / 2 - text.Height / 2);
                });
            })
            {
                new Icon(iconRes, label)
                {
                    Modifier = Modifier.Size(24),
                },
                new Text(label)
                {
                    Modifier = Modifier.Alpha(opacity.Value),
                },
            };
            fab.Add(content);
            return fab;
        });

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
