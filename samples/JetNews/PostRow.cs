namespace ComposeNet.Samples.JetNews;

/// <summary>
/// Discriminator for rows on the article screen — the hero card plus
/// one row per paragraph, flattened into the body
/// <see cref="ComposeNet.LazyColumn{T}"/>.
/// </summary>
public abstract record PostRow
{
    /// <summary>The hero card (image + title + subtitle + meta).</summary>
    public sealed record Hero(Post Post) : PostRow;

    /// <summary>One paragraph of the body. <c>Index</c> is its position in the post.</summary>
    public sealed record Body(Paragraph Paragraph, int Index) : PostRow;
}
