
namespace ComposeNet.Samples.JetNews;

/// <summary>
/// Discriminator for rows in the home feed — used to flatten the
/// section/header/card layout into a single <see cref="LazyColumn{T}"/>
/// without needing nested lazy scopes.
/// </summary>
public abstract record HomeRow
{
    /// <summary>The single highlighted post at the top of the feed (PostCardTop layout).</summary>
    public sealed record Highlight(Post Post) : HomeRow;

    /// <summary>A section header (e.g. "Popular on JetNews").</summary>
    public sealed record SectionHeader(string Label) : HomeRow;

    /// <summary>A standard recommended/recent post row (PostCardSimple layout).</summary>
    public sealed record Recommended(Post Post) : HomeRow;

    /// <summary>Horizontally-scrolling row of <see cref="HomeCards.BuildPopular"/> cards.</summary>
    public sealed record PopularCarousel(IReadOnlyList<Post> Posts) : HomeRow;

    /// <summary>Full-width horizontal divider rendered between major sections.</summary>
    public sealed record Divider : HomeRow;
}
