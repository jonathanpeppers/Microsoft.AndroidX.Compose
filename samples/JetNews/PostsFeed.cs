using System.Collections.Generic;

namespace ComposeNet.Samples.JetNews;

/// <summary>
/// Shape of the home feed: one highlighted post, plus three lists
/// fed into the home screen's sections. Mirrors the upstream sample's
/// <c>PostsFeed</c>.
/// </summary>
public sealed record PostsFeed(
    Post Highlighted,
    IReadOnlyList<Post> Recommended,
    IReadOnlyList<Post> Popular,
    IReadOnlyList<Post> Recent);
