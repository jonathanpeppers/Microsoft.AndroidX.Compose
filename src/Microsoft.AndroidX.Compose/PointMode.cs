namespace AndroidX.Compose;

/// <summary>How a sequence of points is connected when drawn.</summary>
public enum PointMode
{
    /// <summary>Draws each offset as an independent point.</summary>
    Points = 0,

    /// <summary>Draws each consecutive pair as an independent line.</summary>
    Lines = 1,

    /// <summary>Draws one connected polyline through every offset.</summary>
    Polygon = 2,
}
