namespace AndroidX.Compose;

/// <summary>Rule used to determine which regions of a path are filled.</summary>
public enum PathFillType
{
    /// <summary>Uses the non-zero winding rule.</summary>
    NonZero = 0,

    /// <summary>Uses the even-odd winding rule.</summary>
    EvenOdd = 1,
}
