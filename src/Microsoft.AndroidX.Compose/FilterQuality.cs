namespace AndroidX.Compose;

/// <summary>Sampling quality used when scaling image bitmaps.</summary>
public enum FilterQuality
{
    /// <summary>Uses nearest-neighbor sampling.</summary>
    None = 0,

    /// <summary>Uses bilinear sampling.</summary>
    Low = 1,

    /// <summary>Uses medium-quality sampling when supported.</summary>
    Medium = 2,

    /// <summary>Uses high-quality sampling when supported.</summary>
    High = 3,
}
