namespace AndroidX.Compose;

/// <summary>Controls how Compose resolves a resource font when displaying text.</summary>
public enum FontLoadingStrategy
{
    /// <summary>Load synchronously before the first frame. Recommended for bundled fonts.</summary>
    Blocking = 0,

    /// <summary>Try loading locally; use the next matching family entry if unavailable.</summary>
    OptionalLocal = 1,

    /// <summary>Load in the background, displaying a fallback until ready; text may reflow.</summary>
    Async = 2,
}
