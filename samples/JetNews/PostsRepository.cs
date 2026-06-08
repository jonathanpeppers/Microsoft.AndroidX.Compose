using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ComposeNet.Samples.JetNews;

/// <summary>
/// In-memory <see cref="IPostsRepository"/> implementation that
/// reads from the static <see cref="PostsRepo"/> seed data, with a
/// short artificial delay on <see cref="GetFeedAsync"/> so the UDF
/// loading state has something to render.
/// </summary>
public sealed class PostsRepository : IPostsRepository
{
    /// <summary>Process-wide singleton — the sample doesn't have DI.</summary>
    public static PostsRepository Instance { get; } = new();

    /// <inheritdoc/>
    public async Task<PostsFeed> GetFeedAsync(CancellationToken cancellationToken = default)
    {
        // 600 ms is long enough for the UI to clearly cross
        // Loading -> HasPosts, short enough that pull-to-refresh
        // doesn't feel like a hang.
        await Task.Delay(600, cancellationToken).ConfigureAwait(false);
        return PostsRepo.Feed;
    }

    /// <inheritdoc/>
    public Task<Post?> GetPostAsync(string id, CancellationToken cancellationToken = default)
    {
        foreach (var p in PostsRepo.All)
            if (p.Id == id) return Task.FromResult<Post?>(p);
        return Task.FromResult<Post?>(null);
    }

    /// <inheritdoc/>
    public IReadOnlyList<Post> All => PostsRepo.All;
}
