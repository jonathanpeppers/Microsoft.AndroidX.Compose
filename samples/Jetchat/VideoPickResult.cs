namespace AndroidX.Compose.Samples.Jetchat;

/// <summary>Result delivered by the Jetchat video picker.</summary>
public sealed class VideoPickResult
{
    /// <summary>App-readable URI of the selected video, or <see langword="null"/>.</summary>
    public string? VideoUri { get; }

    /// <summary>User-facing import error, or <see langword="null"/>.</summary>
    public string? Error { get; }

    /// <summary>Whether the picker closed without selecting a video.</summary>
    public bool WasCancelled { get; }

    VideoPickResult(string? videoUri, string? error, bool wasCancelled)
    {
        VideoUri = videoUri;
        Error = error;
        WasCancelled = wasCancelled;
    }

    internal static VideoPickResult Cancelled { get; } = new(null, null, true);

    internal static VideoPickResult Selected(string videoUri) => new(videoUri, null, false);

    internal static VideoPickResult Failed(string error) => new(null, error, false);
}
