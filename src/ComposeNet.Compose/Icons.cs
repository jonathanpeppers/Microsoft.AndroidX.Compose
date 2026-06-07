using AndroidX.Compose.UI.Graphics.Vector;

namespace ComposeNet;

/// <summary>
/// Material 3 vector icons. Mirrors Kotlin's
/// <c>androidx.compose.material.icons.Icons</c> namespace —
/// <see cref="Filled"/> for solid icons, <see cref="AutoMirrored"/>
/// for the directional icons that flip in RTL layouts.
/// </summary>
/// <remarks>
/// Each property returns a cached
/// <see cref="ImageVector"/>; pass it into
/// <see cref="Icon(ImageVector, string?)"/>.
/// Backed by the
/// <c>Xamarin.AndroidX.Compose.Material.Icons.Core(.Android)</c>
/// 1.7.8.5 NuGet, which ships only the Java library — getters are
/// invoked via JNI through <see cref="IconBridge"/>.
/// </remarks>
public static class Icons
{
    /// <summary>
    /// The Material "Filled" (solid) variant of each icon. Equivalent
    /// to Kotlin's <c>Icons.Default</c>.
    /// </summary>
    public static class Filled
    {
        const string V = "androidx/compose/material/icons/Icons$Filled";

        /// <summary>The Material "Search" filled icon.</summary>
        public static ImageVector Search        => IconBridge.Get("androidx/compose/material/icons/filled/SearchKt",        "getSearch",        V);
        /// <summary>The Material "Menu" filled icon.</summary>
        public static ImageVector Menu          => IconBridge.Get("androidx/compose/material/icons/filled/MenuKt",          "getMenu",          V);
        /// <summary>The Material "Add" filled icon.</summary>
        public static ImageVector Add           => IconBridge.Get("androidx/compose/material/icons/filled/AddKt",           "getAdd",           V);
        /// <summary>The Material "Delete" filled icon.</summary>
        public static ImageVector Delete        => IconBridge.Get("androidx/compose/material/icons/filled/DeleteKt",        "getDelete",        V);
        /// <summary>The Material "Edit" filled icon.</summary>
        public static ImageVector Edit          => IconBridge.Get("androidx/compose/material/icons/filled/EditKt",          "getEdit",          V);
        /// <summary>The Material "Settings" filled icon.</summary>
        public static ImageVector Settings      => IconBridge.Get("androidx/compose/material/icons/filled/SettingsKt",      "getSettings",      V);
        /// <summary>The Material "More vertical" (overflow) filled icon.</summary>
        public static ImageVector MoreVert      => IconBridge.Get("androidx/compose/material/icons/filled/MoreVertKt",      "getMoreVert",      V);
        /// <summary>The Material "Close" / "X" filled icon.</summary>
        public static ImageVector Close         => IconBridge.Get("androidx/compose/material/icons/filled/CloseKt",         "getClose",         V);
        /// <summary>The Material "Check" / tick filled icon.</summary>
        public static ImageVector Check         => IconBridge.Get("androidx/compose/material/icons/filled/CheckKt",         "getCheck",         V);
        /// <summary>The Material "Star" filled icon.</summary>
        public static ImageVector Star          => IconBridge.Get("androidx/compose/material/icons/filled/StarKt",          "getStar",          V);
        /// <summary>The Material "Favorite" (heart) filled icon.</summary>
        public static ImageVector Favorite      => IconBridge.Get("androidx/compose/material/icons/filled/FavoriteKt",      "getFavorite",      V);
        /// <summary>The Material "Share" filled icon.</summary>
        public static ImageVector Share         => IconBridge.Get("androidx/compose/material/icons/filled/ShareKt",         "getShare",         V);
        /// <summary>The Material "Home" filled icon.</summary>
        public static ImageVector Home          => IconBridge.Get("androidx/compose/material/icons/filled/HomeKt",          "getHome",          V);
        /// <summary>The Material "Person" filled icon.</summary>
        public static ImageVector Person        => IconBridge.Get("androidx/compose/material/icons/filled/PersonKt",        "getPerson",        V);
        /// <summary>The Material "Notifications" (bell) filled icon.</summary>
        public static ImageVector Notifications => IconBridge.Get("androidx/compose/material/icons/filled/NotificationsKt", "getNotifications", V);
        /// <summary>The Material "Refresh" filled icon.</summary>
        public static ImageVector Refresh       => IconBridge.Get("androidx/compose/material/icons/filled/RefreshKt",       "getRefresh",       V);
        /// <summary>The Material "Info" filled icon.</summary>
        public static ImageVector Info          => IconBridge.Get("androidx/compose/material/icons/filled/InfoKt",          "getInfo",          V);
        /// <summary>The Material "Warning" (triangle) filled icon.</summary>
        public static ImageVector Warning       => IconBridge.Get("androidx/compose/material/icons/filled/WarningKt",       "getWarning",       V);
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
            const string V = "androidx/compose/material/icons/Icons$AutoMirrored$Filled";

            /// <summary>The Material "Arrow back" auto-mirrored filled icon.</summary>
            public static ImageVector ArrowBack    => IconBridge.Get("androidx/compose/material/icons/automirrored/filled/ArrowBackKt",    "getArrowBack",    V);
            /// <summary>The Material "Arrow forward" auto-mirrored filled icon.</summary>
            public static ImageVector ArrowForward => IconBridge.Get("androidx/compose/material/icons/automirrored/filled/ArrowForwardKt", "getArrowForward", V);
        }
    }
}
