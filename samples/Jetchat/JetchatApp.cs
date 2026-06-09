using AndroidX.Compose.Material3;
using AndroidX.Compose.UI.Text.Input;
using ComposeNet;

namespace ComposeNet.Samples.Jetchat;

/// <summary>
/// Top-level Jetchat tree. Wraps the
/// <see cref="JetchatTheme"/> around a
/// <see cref="ModalNavigationDrawer"/> whose body is a
/// <see cref="NavHost"/> with two routes: the conversation and the
/// profile screen. Drawer chat rows swap the active channel in-place;
/// drawer profile rows and message-avatar taps both
/// <see cref="NavController.Navigate(string)"/> to the profile route.
/// </summary>
public static class JetchatApp
{
    /// <summary>Routes registered with the top-level <see cref="NavHost"/>.</summary>
    public static class Routes
    {
        /// <summary>The conversation screen — start destination.</summary>
        public const string Home = "home";

        /// <summary>The profile screen — takes a <c>{userId}</c> placeholder.</summary>
        public const string ProfilePattern = "profile/{userId}";

        /// <summary>Build the profile route for a specific user id.</summary>
        public static string Profile(string userId) => $"profile/{userId}";
    }

    /// <summary>Materialize the Jetchat tree for one composition pass.</summary>
    public static ComposableNode Build(
        NavController                nav,
        ConversationUiState          ui,
        MutableState<TextFieldValue> input,
        MutableState<string>         selectedMenu,
        ScrollState          drawerScroll,
        DrawerStateHolder    drawerState,
        MutableState<int>    selectedSelector,
        MutableState<bool>   popupOpen,
        LazyListState        messagesScroll,
        ProfileViewModel     profileViewModel) =>
        JetchatTheme.Build(new Composed(c =>
        {
            var scheme = MaterialTheme.CurrentColorScheme(c);
            return new ModalNavigationDrawer(drawerState)
            {
                Drawer  = JetchatDrawer.Build(
                    ui:                 ui,
                    selectedMenu:       selectedMenu,
                    drawerState:        drawerState,
                    scroll:             drawerScroll,
                    scheme:             scheme,
                    onChatClicked:      channel =>
                    {
                        selectedMenu.Value = channel;
                        if (!IsOnHome(nav))
                            nav.PopBackStack(Routes.Home, inclusive: false);
                    },
                    onProfileClicked:   userId =>
                    {
                        selectedMenu.Value = userId;
                        profileViewModel.SetUserId(userId);
                        if (!IsOnHome(nav))
                            nav.PopBackStack(Routes.Home, inclusive: false);
                        nav.Navigate(Routes.Profile(userId));
                    }),
                Content = new NavHost(startDestination: Routes.Home, navController: nav)
                {
                    new Composable(Routes.Home)
                    {
                        Conversation.Build(
                            ui:               ui,
                            input:            input,
                            selectedMenu:     selectedMenu,
                            selectedSelector: selectedSelector,
                            popupOpen:        popupOpen,
                            messagesScroll:   messagesScroll,
                            onOpenDrawer:     () => _ = drawerState.OpenAsync(),
                            onAuthorClicked:  userId =>
                            {
                                profileViewModel.SetUserId(userId);
                                nav.Navigate(Routes.Profile(userId));
                            }),
                    },
                    new Composable(Routes.ProfilePattern, entry =>
                    {
                        var userId = entry.Arguments?.GetString("userId");
                        return Profile.Build(
                            state:  Profiles.GetById(userId),
                            onBack: () => nav.PopBackStack());
                    }),
                },
            };
        }));

    static bool IsOnHome(NavController nav)
    {
        var entry = nav.CurrentBackStackEntry;
        return entry is null || entry.Route == Routes.Home;
    }
}
