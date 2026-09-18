
namespace AndroidX.Compose.Samples.JetNews;

/// <summary>
/// Discriminator for rows in the home feed — used to flatten the
/// section/header/card layout into a single <see cref="LazyColumn{T}"/>
/// without needing nested lazy scopes.
/// </summary>
/// <param name="Identity">
/// Stable, unique identity used to retain the row's composition state when
/// search filtering inserts, removes, or reorders rows.
/// </param>
public abstract record HomeRow(string Identity)
{
    /// <summary>The single highlighted post at the top of the feed (PostCardTop layout).</summary>
    public sealed record Highlight(Post Post) : HomeRow($"highlight:{Post.Id}");

    /// <summary>A section header (e.g. "Popular on JetNews").</summary>
    public sealed record SectionHeader(string Section, string Label) : HomeRow($"header:{Section}");

    /// <summary>A standard recommended/recent post row (PostCardSimple layout).</summary>
    public sealed record Recommended(string Section, Post Post) : HomeRow($"{Section}:{Post.Id}");

    /// <summary>Empty-state message shown when a search has no matches.</summary>
    public sealed record SearchEmpty(string Message) : HomeRow("search-empty");

    /// <summary>Horizontally-scrolling row of <see cref="HomeCards.BuildPopular"/> cards.</summary>
    public sealed record PopularCarousel(IReadOnlyList<Post> Posts) : HomeRow("popular-carousel");

    /// <summary>Full-width horizontal divider rendered between major sections.</summary>
    public sealed record Divider(string Section) : HomeRow($"divider:{Section}");
}
