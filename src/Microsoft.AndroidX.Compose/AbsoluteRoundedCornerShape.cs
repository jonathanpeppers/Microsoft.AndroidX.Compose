using Android.Runtime;
using AndroidX.Compose.Foundation.Shape;

namespace AndroidX.Compose;

/// <summary>A rounded shape with physical left/right corners that never mirror in RTL.</summary>
/// <remarks>
/// Integer arguments are percentages of the shorter side; use <c>16.Dp()</c>
/// for a density-aware radius. Kotlin validates and normalizes corner sizes.
/// </remarks>
public sealed class AbsoluteRoundedCornerShape : Shape
{
    /// <summary>Creates equal density-aware radii on all four corners.</summary>
    public AbsoluteRoundedCornerShape(Dp cornerSize) : this(cornerSize, cornerSize, cornerSize, cornerSize) { }

    /// <summary>Creates equal percentage radii on all four corners.</summary>
    public AbsoluteRoundedCornerShape(int cornerPercent)
        : base(AbsoluteRoundedCornerShapeKt.AbsoluteRoundedCornerShape(cornerPercent)) { }

    /// <summary>Creates density-aware radii in top-left, top-right, bottom-right, bottom-left order.</summary>
    public AbsoluteRoundedCornerShape(Dp topLeft, Dp topRight, Dp bottomRight, Dp bottomLeft)
        : base(ComposeBridges.AbsoluteRoundedCornerShape4Dp(
            topLeft.Value, topRight.Value, bottomRight.Value, bottomLeft.Value),
            JniHandleOwnership.TransferLocalRef) { }

    /// <summary>Creates percentage radii in top-left, top-right, bottom-right, bottom-left order.</summary>
    public AbsoluteRoundedCornerShape(
        int topLeftPercent, int topRightPercent, int bottomRightPercent, int bottomLeftPercent)
        : base(AbsoluteRoundedCornerShapeKt.AbsoluteRoundedCornerShape(
            topLeftPercent, topRightPercent, bottomRightPercent, bottomLeftPercent)) { }
}
