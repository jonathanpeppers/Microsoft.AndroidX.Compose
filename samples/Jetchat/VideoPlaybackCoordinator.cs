namespace AndroidX.Compose.Samples.Jetchat;

internal static class VideoPlaybackCoordinator
{
    static readonly object Gate = new();
    static VideoPlaybackSession? s_active;

    internal static void Activate(VideoPlaybackSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        lock (Gate)
        {
            if (!ReferenceEquals(s_active, session))
                s_active?.Release();
            s_active = session;
        }
    }

    internal static void Deactivate(VideoPlaybackSession session)
    {
        lock (Gate)
        {
            if (ReferenceEquals(s_active, session))
                s_active = null;
        }
    }

    internal static void PauseActive()
    {
        lock (Gate)
            s_active?.Pause();
    }

    internal static void ReleaseActive()
    {
        lock (Gate)
        {
            s_active?.Release();
            s_active = null;
        }
    }
}
