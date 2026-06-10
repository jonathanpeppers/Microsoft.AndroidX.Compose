namespace AndroidX.Compose.Samples.JetNews;

/// <summary>
/// Public route strings recognised by the <see cref="NavHost"/>. Kept
/// in one place so they don't drift between Navigate calls and
/// <see cref="Composable"/> destinations.
/// </summary>
public static class Routes
{
    /// <summary>The home feed (highlighted post + recommendations).</summary>
    public const string Home = "home";

    /// <summary>The interests screen (topics / people / publications).</summary>
    public const string Interests = "interests";

    /// <summary>The article screen — takes a <c>{postId}</c> placeholder.</summary>
    public const string PostPattern = "post/{postId}";

    /// <summary>Build the article route for a specific post id.</summary>
    public static string Post(string postId) => "post/" + postId;
}
