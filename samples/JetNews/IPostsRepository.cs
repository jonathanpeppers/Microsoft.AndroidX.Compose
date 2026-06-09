
namespace ComposeNet.Samples.JetNews;

/// <summary>
/// Async data source for the JetNews feed and per-post lookup. The
/// interface deliberately mirrors what a real repository would
/// expose — a small async surface backed by network / disk — so the
/// home view model can drive the UDF pattern without depending on
/// the static <see cref="PostsRepo"/> seed.
/// </summary>
public interface IPostsRepository
{
    /// <summary>
    /// Asynchronously returns the home feed. The default
    /// <see cref="PostsRepository"/> simulates a network call with a
    /// short delay so the UDF loading branch is observable in the
    /// running app.
    /// </summary>
    Task<PostsFeed> GetFeedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously returns a single post by id, or <c>null</c>
    /// if the post is not in the feed.
    /// </summary>
    Task<Post?> GetPostAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Full chronological listing of every known post. Synchronous
    /// because the seed data is in-memory; production
    /// implementations would expose this as
    /// <see cref="Task{IReadOnlyList}"/>.
    /// </summary>
    IReadOnlyList<Post> All { get; }
}
