namespace AndroidX.Compose;

/// <summary>Boolean operation used when combining two paths.</summary>
public enum PathOperation
{
    /// <summary>Area in the first path but not the second.</summary>
    Difference = 0,

    /// <summary>Area shared by both paths.</summary>
    Intersect = 1,

    /// <summary>Area in either path.</summary>
    Union = 2,

    /// <summary>Area in exactly one path.</summary>
    Xor = 3,

    /// <summary>Area in the second path but not the first.</summary>
    ReverseDifference = 4,
}
