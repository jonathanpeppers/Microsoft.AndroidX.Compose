using AndroidX.Compose.Material3;

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
        var sheet = new ModalDrawerSheet { ContainerColor = scheme.Surface };
        sheet.Add(new Column
        {
            Modifier.FillMaxWidth().VerticalScroll(scroll),
            new Spacer(Modifier.StatusBarsPadding()),
            BuildHeader(scheme),
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

    static Row BuildHeader(ColorScheme scheme) =>
        new()
        {
            Modifier.FillMaxWidth().Padding(16),
            JetchatIcon.Build(contentDescription: null, sizeDp: 24),
            Spacer.Width(8),
            new Text("Jetchat")
            {
                FontSize   = 18,
                FontWeight = FontWeight.SemiBold,
                Color      = scheme.OnSurface,
            },
        };

    static HorizontalDivider BuildDivider(ColorScheme scheme, int sidePadding) =>
        new()
        {
            Modifier = sidePadding > 0
                ? Modifier.Padding(horizontal: sidePadding, vertical: 0)
                : null,
        };

    static Box BuildSectionHeader(string label, ColorScheme scheme) =>
        new()
        {
            Modifier.FillMaxWidth().Height(52).Padding(horizontal: 28, vertical: 0),
            new Text(label)
            {
                FontSize = 14,
                Color    = scheme.OnSurfaceVariant,
                Modifier = Modifier.Padding(top: 16, bottom: 0, start: 0, end: 0),
            },
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
            .Padding(horizontal: 12, vertical: 0)
            .Clip(28)
            .Clickable(() =>
            {
                onChatClicked(channel);
                _ = drawerState.CloseAsync();
            });
        if (selected)
            modifier = modifier.Background(scheme.PrimaryContainer);

        long iconTint  = selected ? scheme.Primary : scheme.OnSurfaceVariant;
        long textColor = selected ? scheme.Primary : scheme.OnSurface;

        return new Row
        {
            modifier,
            new Icon(Resource.Drawable.ic_jetchat, null)
            {
                Modifier = Modifier.Padding(top: 16, bottom: 16, start: 16, end: 0),
                TintArgb = iconTint,
            },
            new Text(channel)
            {
                FontSize   = 14,
                FontWeight = selected ? FontWeight.SemiBold : FontWeight.Normal,
                Color      = textColor,
                Modifier   = Modifier.Padding(top: 16, bottom: 16, start: 12, end: 0),
            },
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
            .Padding(horizontal: 12, vertical: 0)
            .Clip(28)
            .Clickable(() =>
            {
                onProfileClicked(userId);
                _ = drawerState.CloseAsync();
            });
        if (selected)
            modifier = modifier.Background(scheme.PrimaryContainer);

        return new Row
        {
            modifier,
            new Image(avatarRes, "Profile photo")
            {
                Modifier = Modifier
                    .Padding(top: 16, bottom: 16, start: 16, end: 0)
                    .Size(24)
                    .Clip(12),
            },
            new Text(name)
            {
                FontSize = 14,
                Color    = scheme.OnSurface,
                Modifier = Modifier.Padding(top: 16, bottom: 16, start: 12, end: 0),
            },
        };
    }
}
