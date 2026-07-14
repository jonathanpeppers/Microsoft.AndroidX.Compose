namespace AndroidX.Compose;

/// <summary>Shape placed at the ends of stroked lines and paths.</summary>
public enum StrokeCap
{
    /// <summary>Ends exactly at the path endpoint.</summary>
    Butt = 0,

    /// <summary>Adds a semicircular cap beyond the endpoint.</summary>
    Round = 1,

    /// <summary>Adds a square cap beyond the endpoint.</summary>
    Square = 2,
}
