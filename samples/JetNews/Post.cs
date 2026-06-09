
namespace Microsoft.AndroidX.Compose.Samples.JetNews;

/// <summary>
/// One news article. A condensed version of the upstream sample's
/// <c>Post</c> model: no <c>Publication</c> reference object, no
/// inline-markup spans, no per-post bookmark flag (we track bookmarks
/// externally in a <see cref="MutableStateList{T}"/> of post
/// ids).
/// </summary>
/// <param name="Id">Stable id used as the navigation route key.</param>
/// <param name="Title">Headline rendered in cards and the article top bar.</param>
/// <param name="Subtitle">One-line teaser shown beneath the title.</param>
/// <param name="Metadata">Author / date / read time.</param>
/// <param name="Paragraphs">Body content, rendered top-to-bottom.</param>
/// <param name="HeroId">Drawable resource id for the full-size hero image.</param>
/// <param name="ThumbId">Drawable resource id for the 40×40 list thumbnail.</param>
public sealed record Post(
    string Id,
    string Title,
    string Subtitle,
    PostMetadata Metadata,
    IReadOnlyList<Paragraph> Paragraphs,
    int HeroId,
    int ThumbId);
