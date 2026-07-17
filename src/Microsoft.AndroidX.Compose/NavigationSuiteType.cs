namespace AndroidX.Compose;

/// <summary>
/// Navigation presentation selected by <see cref="NavigationSuiteScaffold"/>
/// and <see cref="NavigationSuiteItem"/>.
/// </summary>
public enum NavigationSuiteType
{
    /// <summary>Compact short navigation bar with vertically arranged items.</summary>
    ShortNavigationBarCompact,

    /// <summary>Medium short navigation bar with horizontally arranged items.</summary>
    ShortNavigationBarMedium,

    /// <summary>Collapsed wide navigation rail.</summary>
    WideNavigationRailCollapsed,

    /// <summary>Expanded wide navigation rail.</summary>
    WideNavigationRailExpanded,

    /// <summary>Legacy Material 3 navigation bar.</summary>
    NavigationBar,

    /// <summary>Legacy Material 3 navigation rail.</summary>
    NavigationRail,

    /// <summary>Legacy permanent navigation drawer.</summary>
    NavigationDrawer,

    /// <summary>No navigation component.</summary>
    None,
}
