using Android.Runtime;
using AndroidX.Compose.Foundation.Shape;

namespace AndroidX.Compose;

/// <summary>A chamfered shape with physical left/right corners that never mirror in RTL.</summary>
/// <remarks>
/// Integer arguments are percentages of the shorter side; use <c>16.Dp()</c>
/// for a density-aware cut. Kotlin validates and normalizes corner sizes.
/// </remarks>
public sealed class AbsoluteCutCornerShape : Shape
{
    /// <summary>Creates equal density-aware cuts on all four corners.</summary>
    public AbsoluteCutCornerShape(Dp cornerSize) : this(cornerSize, cornerSize, cornerSize, cornerSize) { }

    /// <summary>Creates equal percentage cuts on all four corners.</summary>
    public AbsoluteCutCornerShape(int cornerPercent)
        : base(AbsoluteCutCornerShapeKt.AbsoluteCutCornerShape(cornerPercent)) { }

    /// <summary>Creates density-aware cuts in top-left, top-right, bottom-right, bottom-left order.</summary>
    public AbsoluteCutCornerShape(Dp topLeft, Dp topRight, Dp bottomRight, Dp bottomLeft)
        : base(ComposeBridges.AbsoluteCutCornerShape4Dp(
            topLeft.Value, topRight.Value, bottomRight.Value, bottomLeft.Value),
            JniHandleOwnership.TransferLocalRef) { }

    /// <summary>Creates percentage cuts in top-left, top-right, bottom-right, bottom-left order.</summary>
    public AbsoluteCutCornerShape(
        int topLeftPercent, int topRightPercent, int bottomRightPercent, int bottomLeftPercent)
        : base(AbsoluteCutCornerShapeKt.AbsoluteCutCornerShape(
            topLeftPercent, topRightPercent, bottomRightPercent, bottomLeftPercent)) { }
}
