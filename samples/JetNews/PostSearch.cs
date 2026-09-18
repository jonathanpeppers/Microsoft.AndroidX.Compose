namespace AndroidX.Compose.Samples.JetNews;

/// <summary>Searches the in-memory feed without changing its source ordering.</summary>
internal static class PostSearch
{
    internal static IReadOnlyList<Post> Filter(PostsFeed feed, string query)
    {
        ArgumentNullException.ThrowIfNull(feed);
        ArgumentNullException.ThrowIfNull(query);

        query = query.Trim();
        if (query.Length == 0)
            return [];

        var matches = new List<Post>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        AddIfMatch(feed.Highlighted);
        AddMatches(feed.Recommended);
        AddMatches(feed.Popular);
        AddMatches(feed.Recent);
        return matches;

        void AddMatches(IReadOnlyList<Post> posts)
        {
            foreach (var post in posts)
                AddIfMatch(post);
        }

        void AddIfMatch(Post post)
        {
            if (seen.Add(post.Id) && Matches(post, query))
                matches.Add(post);
        }
    }

    static bool Matches(Post post, string query)
    {
        if (Contains(post.Title, query) ||
            Contains(post.Subtitle, query) ||
            Contains(post.Metadata.Author, query))
        {
            return true;
        }

        foreach (var paragraph in post.Paragraphs)
        {
            if (Contains(paragraph.Text, query))
                return true;
        }

        return false;
    }

    static bool Contains(string value, string query) =>
        value.Contains(query, StringComparison.OrdinalIgnoreCase);
}
