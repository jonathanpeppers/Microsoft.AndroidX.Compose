namespace AndroidX.Compose;

/// <summary>
/// Determines which vertical folding features a pane scaffold keeps content
/// away from.
/// </summary>
public enum AdaptiveHingePolicy
{
    /// <summary>Avoid every vertical hinge, including flat non-occluding folds.</summary>
    AlwaysAvoid = 0,

    /// <summary>Avoid vertical hinges that separate the window into regions.</summary>
    AvoidSeparating = 1,

    /// <summary>Avoid only vertical hinges that occlude content.</summary>
    AvoidOccluding = 2,

    /// <summary>Do not reserve pane space for vertical hinges.</summary>
    NeverAvoid = 3,
}
