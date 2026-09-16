namespace AndroidX.Compose;

/// <summary>Operation used when combining a new shape with the current drawing clip.</summary>
public enum ClipOperation
{
    /// <summary>Subtracts the supplied shape from the current clip.</summary>
    Difference = 0,

    /// <summary>Retains only the intersection with the supplied shape.</summary>
    Intersect = 1,
}
