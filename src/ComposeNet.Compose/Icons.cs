using AndroidX.Compose.UI.Graphics.Vector;
using FilledKt = AndroidX.Compose.Material.Icons.Filled;
using AutoMirroredFilledKt = AndroidX.Compose.Material.Icons.AutoMirrored.Filled;
using IconsObject = AndroidX.Compose.Material.Icons.Icons;

namespace ComposeNet;

/// <summary>
/// Material 3 vector icons. Mirrors Kotlin's
/// <c>androidx.compose.material.icons.Icons</c> namespace —
/// <see cref="Filled"/> for solid icons, <see cref="AutoMirrored"/>
/// for the directional icons that flip in RTL layouts.
/// </summary>
/// <remarks>
/// Each property returns the cached
/// <see cref="ImageVector"/> exposed by the
/// <c>Xamarin.AndroidX.Compose.Material.Icons.Core(.Android)</c>
/// 1.7.8.6 bindings; pass it into
/// <see cref="Icon(ImageVector, string?)"/>.
/// </remarks>
public static class Icons
{
    /// <summary>
    /// The Material "Filled" (solid) variant of each icon. Equivalent
    /// to Kotlin's <c>Icons.Default</c>.
    /// </summary>
    public static class Filled
    {
        /// <summary>The Material "Search" filled icon.</summary>
        public static ImageVector Search        => FilledKt.SearchKt.GetSearch(IconsObject.Instance.Default);
        /// <summary>The Material "Menu" filled icon.</summary>
        public static ImageVector Menu          => FilledKt.MenuKt.GetMenu(IconsObject.Instance.Default);
        /// <summary>The Material "Add" filled icon.</summary>
        public static ImageVector Add           => FilledKt.AddKt.GetAdd(IconsObject.Instance.Default);
        /// <summary>The Material "Delete" filled icon.</summary>
        public static ImageVector Delete        => FilledKt.DeleteKt.GetDelete(IconsObject.Instance.Default);
        /// <summary>The Material "Edit" filled icon.</summary>
        public static ImageVector Edit          => FilledKt.EditKt.GetEdit(IconsObject.Instance.Default);
        /// <summary>The Material "Settings" filled icon.</summary>
        public static ImageVector Settings      => FilledKt.SettingsKt.GetSettings(IconsObject.Instance.Default);
        /// <summary>The Material "More vertical" (overflow) filled icon.</summary>
        public static ImageVector MoreVert      => FilledKt.MoreVertKt.GetMoreVert(IconsObject.Instance.Default);
        /// <summary>The Material "Close" / "X" filled icon.</summary>
        public static ImageVector Close         => FilledKt.CloseKt.GetClose(IconsObject.Instance.Default);
        /// <summary>The Material "Check" / tick filled icon.</summary>
        public static ImageVector Check         => FilledKt.CheckKt.GetCheck(IconsObject.Instance.Default);
        /// <summary>The Material "Star" filled icon.</summary>
        public static ImageVector Star          => FilledKt.StarKt.GetStar(IconsObject.Instance.Default);
        /// <summary>The Material "Favorite" (heart) filled icon.</summary>
        public static ImageVector Favorite      => FilledKt.FavoriteKt.GetFavorite(IconsObject.Instance.Default);
        /// <summary>The Material "Share" filled icon.</summary>
        public static ImageVector Share         => FilledKt.ShareKt.GetShare(IconsObject.Instance.Default);
        /// <summary>The Material "Home" filled icon.</summary>
        public static ImageVector Home          => FilledKt.HomeKt.GetHome(IconsObject.Instance.Default);
        /// <summary>The Material "Person" filled icon.</summary>
        public static ImageVector Person        => FilledKt.PersonKt.GetPerson(IconsObject.Instance.Default);
        /// <summary>The Material "Notifications" (bell) filled icon.</summary>
        public static ImageVector Notifications => FilledKt.NotificationsKt.GetNotifications(IconsObject.Instance.Default);
        /// <summary>The Material "Refresh" filled icon.</summary>
        public static ImageVector Refresh       => FilledKt.RefreshKt.GetRefresh(IconsObject.Instance.Default);
        /// <summary>The Material "Info" filled icon.</summary>
        public static ImageVector Info          => FilledKt.InfoKt.GetInfo(IconsObject.Instance.Default);
        /// <summary>The Material "Warning" (triangle) filled icon.</summary>
        public static ImageVector Warning       => FilledKt.WarningKt.GetWarning(IconsObject.Instance.Default);
    }

    /// <summary>
    /// Auto-mirrored variants — these icons automatically flip
    /// horizontally when the layout direction is RTL.
    /// </summary>
    public static class AutoMirrored
    {
        /// <summary>
        /// The auto-mirrored Material "Filled" (solid) variant.
        /// </summary>
        public static class Filled
        {
            /// <summary>The Material "Arrow back" auto-mirrored filled icon.</summary>
            public static ImageVector ArrowBack    => AutoMirroredFilledKt.ArrowBackKt.GetArrowBack(IconsObject.AutoMirrored.Instance.Default);
            /// <summary>The Material "Arrow forward" auto-mirrored filled icon.</summary>
            public static ImageVector ArrowForward => AutoMirroredFilledKt.ArrowForwardKt.GetArrowForward(IconsObject.AutoMirrored.Instance.Default);
        }
    }
}
