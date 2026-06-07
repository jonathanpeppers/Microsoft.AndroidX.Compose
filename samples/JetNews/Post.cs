using System.Collections.Generic;

namespace ComposeNet.Samples.JetNews;

/// <summary>
/// One news article. A condensed version of the upstream sample's
/// <c>Post</c> model: no <c>Publication</c> reference object, no
/// inline-markup spans, no per-post bookmark flag (we track bookmarks
/// externally in a <see cref="ComposeNet.MutableStateList{T}"/> of post
/// ids).
/// </summary>
/// <param name="Id">Stable id used as the navigation route key.</param>
/// <param name="Title">Headline rendered in cards and the article top bar.</param>
/// <param name="Subtitle">One-line teaser shown beneath the title.</param>
/// <param name="Metadata">Author / date / read time.</param>
/// <param name="Paragraphs">Body content, rendered top-to-bottom.</param>
/// <param name="HeroColor">
/// Solid hero-image color. The upstream sample uses real photo PNGs
/// — see <c>README.md</c> for why this port uses solid colored cards
/// instead.
/// </param>
public sealed record Post(
    string Id,
    string Title,
    string Subtitle,
    PostMetadata Metadata,
    IReadOnlyList<Paragraph> Paragraphs,
    ComposeNet.Color HeroColor);
