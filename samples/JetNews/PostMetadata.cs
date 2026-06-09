namespace Microsoft.AndroidX.Compose.Samples.JetNews;

/// <summary>
/// Static metadata attached to every <see cref="Post"/> — author,
/// publication date, and reading-time estimate.
/// </summary>
public sealed record PostMetadata(string Author, string Date, int ReadTimeMinutes);
