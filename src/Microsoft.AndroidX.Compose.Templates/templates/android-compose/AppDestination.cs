namespace MyApplication;

/// <summary>A top-level application destination.</summary>
internal sealed record AppDestination(string Label, int Icon)
{
    /// <summary>All top-level destinations in display order.</summary>
    internal static IReadOnlyList<AppDestination> All { get; } =
    [
        new("Home", Resource.Drawable.ic_home),
        new("Favorites", Resource.Drawable.ic_favorite),
        new("Profile", Resource.Drawable.ic_account_box),
    ];
}
