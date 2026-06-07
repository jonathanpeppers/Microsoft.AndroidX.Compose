namespace ComposeNet.Samples.JetNews;

/// <summary>
/// Discriminator for rows in the home feed — used to flatten the
/// section/header/card layout into a single <see cref="ComposeNet.LazyColumn{T}"/>
/// without needing nested lazy scopes.
/// </summary>
public abstract record HomeRow
{
    /// <summary>The single highlighted post at the top of the feed.</summary>
    public sealed record Highlight(Post Post) : HomeRow;

    /// <summary>A section header (e.g. "Popular on JetNews").</summary>
    public sealed record SectionHeader(string Label) : HomeRow;

    /// <summary>A standard recommended/popular/recent post row.</summary>
    public sealed record Recommended(Post Post) : HomeRow;
}
