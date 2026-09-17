using AndroidX.Compose.Material3;
using AndroidX.Compose.Samples.Jetchat.Theme;
using Typography = AndroidX.Compose.Samples.Jetchat.Theme.Typography;

namespace AndroidX.Compose.Samples.Jetchat;

/// <summary>
/// The activity-wide navigation drawer. Extracted from
/// <see cref="Conversation"/> so it can sit above the
/// <see cref="NavHost"/> and stay visible across the conversation and
/// profile screens — matching upstream Jetchat, where the drawer lives
/// in the host activity rather than per-fragment.
/// </summary>
public static class JetchatDrawer
{
    /// <summary>
    /// Build the drawer sheet. <paramref name="onChatClicked"/> fires
    /// for chat rows; <paramref name="onProfileClicked"/> fires for the
    /// "Recent Profiles" rows.
    /// </summary>
    public static ModalDrawerSheet Build(
        ConversationUiState  ui,
        MutableState<string> selectedMenu,
        DrawerStateHolder    drawerState,
        ScrollState          scroll,
        ColorScheme          scheme,
        Action<string> onChatClicked,
        Action<string> onProfileClicked)
    {
        var sheet = new ModalDrawerSheet
        {
            ContainerColor = Color.FromPacked(scheme.Surface),
        };
        sheet.Add(new Column
        {
            Modifier.FillMaxWidth().VerticalScroll(scroll),
            new Spacer(Modifier.StatusBarsPadding()),
            BuildHeader(),
            BuildDivider(scheme, sidePadding: 0),
            BuildSectionHeader("Chats", scheme),
            BuildChatItem(selectedMenu, drawerState, "composers",    scheme, onChatClicked),
            BuildChatItem(selectedMenu, drawerState, "droidcon-nyc", scheme, onChatClicked),
            BuildDivider(scheme, sidePadding: 28),
            BuildSectionHeader("Recent Profiles", scheme),
            BuildProfileItem(selectedMenu, drawerState, "Ali Conors (you)", Profiles.MeProfile.UserId,        Resource.Drawable.avatar_ali,          scheme, onProfileClicked),
            BuildProfileItem(selectedMenu, drawerState, "Taylor Brooks",    Profiles.ColleagueProfile.UserId, Resource.Drawable.avatar_someone_else, scheme, onProfileClicked),
        });
        return sheet;
    }

    static Row BuildHeader() =>
        new(
            horizontalArrangement: null,
            verticalAlignment: Alignment.Vertical.CenterVertically)
        {
            Modifier.FillMaxWidth().Padding(16),
            JetchatIcon.Build(null, sizeDp: 24),
            new Image(Resource.Drawable.jetchat_logo, "Jetchat")
            {
                Modifier = Modifier.Padding(start: 8).Width(87).Height(24),
            },
        };

    static HorizontalDivider BuildDivider(ColorScheme scheme, int sidePadding) =>
        new()
        {
            Color = Color.FromPacked(scheme.OnSurface).WithAlpha(31),
            Modifier = sidePadding > 0
                ? Modifier.Padding(horizontal: sidePadding)
                : null,
        };

    static Box BuildSectionHeader(string label, ColorScheme scheme) =>
        new()
        {
            Modifier.FillMaxWidth().HeightIn(min: 52).Padding(horizontal: 28),
            new Text(label)
            {
                FontFamily = JetchatFonts.Karla,
                Color    = Color.FromPacked(scheme.OnSurfaceVariant),
                Modifier = Modifier.Align(Alignment.CenterStart),
            }.WithTypography(Typography.BodySmall),
        };

    static Row BuildChatItem(
        MutableState<string> selectedMenu,
        DrawerStateHolder    drawerState,
        string               channel,
        ColorScheme          scheme,
        Action<string> onChatClicked)
    {
        bool selected = selectedMenu.Value == channel;
        var modifier = Modifier
            .FillMaxWidth()
            .Height(56)
            .Padding(horizontal: 12)
            .Clip(28)
            .Clickable(() =>
            {
                onChatClicked(channel);
                _ = drawerState.CloseAsync();
            });
        if (selected)
            modifier = modifier.Background(Color.FromPacked(scheme.PrimaryContainer));

        var iconTint = Color.FromPacked(
            selected ? scheme.Primary : scheme.OnSurfaceVariant);
        var textColor = Color.FromPacked(
            selected ? scheme.Primary : scheme.OnSurface);

        return new Row(
            horizontalArrangement: null,
            verticalAlignment: Alignment.Vertical.CenterVertically)
        {
            modifier,
            new Icon(Resource.Drawable.ic_jetchat, null)
            {
                Modifier = Modifier.Padding(top: 16, bottom: 16, start: 16),
                Tint = iconTint,
            },
            new Text(channel)
            {
                FontFamily = JetchatFonts.Montserrat,
                Color      = textColor,
                Modifier   = Modifier.Padding(start: 12),
            }.WithTypography(Typography.BodyMedium),
        };
    }

    static Row BuildProfileItem(
        MutableState<string> selectedMenu,
        DrawerStateHolder    drawerState,
        string               name,
        string               userId,
        int                  avatarRes,
        ColorScheme          scheme,
        Action<string> onProfileClicked)
    {
        bool selected = selectedMenu.Value == userId;
        var modifier = Modifier
            .FillMaxWidth()
            .Height(56)
            .Padding(horizontal: 12)
            .Clip(28)
            .Clickable(() =>
            {
                onProfileClicked(userId);
                _ = drawerState.CloseAsync();
            });
        if (selected)
            modifier = modifier.Background(Color.FromPacked(scheme.PrimaryContainer));

        return new Row(
            horizontalArrangement: null,
            verticalAlignment: Alignment.Vertical.CenterVertically)
        {
            modifier,
            new Image(avatarRes, "Profile photo")
            {
                Modifier = Modifier
                    .Padding(top: 16, bottom: 16, start: 16)
                    .Size(24)
                    .Clip(12),
            },
            new Text(name)
            {
                FontFamily = JetchatFonts.Montserrat,
                Color    = Color.FromPacked(scheme.OnSurface),
                Modifier = Modifier.Padding(start: 12),
            }.WithTypography(Typography.BodyMedium),
        };
    }
}
