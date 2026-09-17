namespace AndroidX.Compose;

/// <summary>How a shape is oriented along a stamped path effect.</summary>
public enum StampedPathEffectStyle
{
    /// <summary>Translates each stamp without rotating it.</summary>
    Translate = 0,

    /// <summary>Rotates each stamp to follow the path tangent.</summary>
    Rotate = 1,

    /// <summary>Morphs each stamp to follow the path geometry.</summary>
    Morph = 2,
}
