namespace AndroidX.Compose;

/// <summary>Geometry used where two stroked path segments meet.</summary>
public enum StrokeJoin
{
    /// <summary>Extends edges to a sharp corner, limited by the miter value.</summary>
    Miter = 0,

    /// <summary>Rounds the outside corner.</summary>
    Round = 1,

    /// <summary>Bevels the outside corner.</summary>
    Bevel = 2,
}
