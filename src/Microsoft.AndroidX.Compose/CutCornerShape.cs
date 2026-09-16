using Android.Runtime;
using AndroidX.Compose.Foundation.Shape;

namespace AndroidX.Compose;

/// <summary>A chamfered shape whose start/end corners mirror in right-to-left layouts.</summary>
/// <remarks>
/// Integer arguments are percentages of the shorter side; use <c>16.Dp()</c>
/// for a density-aware cut. Kotlin validates and normalizes corner sizes.
/// </remarks>
[Register("net/compose/CutCornerShape", DoNotGenerateAcw = true)]
public sealed class CutCornerShape : Shape
{
    /// <summary>Creates equal density-aware cuts on all four corners.</summary>
    public CutCornerShape(Dp cornerSize) : this(cornerSize, cornerSize, cornerSize, cornerSize) { }

    /// <summary>Creates equal percentage cuts on all four corners.</summary>
    public CutCornerShape(int cornerPercent) : base(CutCornerShapeKt.CutCornerShape(cornerPercent)) { }

    /// <summary>Creates density-aware cuts in top-start, top-end, bottom-end, bottom-start order.</summary>
    public CutCornerShape(Dp topStart, Dp topEnd, Dp bottomEnd, Dp bottomStart)
        : base(ComposeBridges.CutCornerShape4Dp(topStart.Value, topEnd.Value, bottomEnd.Value, bottomStart.Value),
            JniHandleOwnership.TransferLocalRef) { }

    /// <summary>Creates percentage cuts in top-start, top-end, bottom-end, bottom-start order.</summary>
    public CutCornerShape(int topStartPercent, int topEndPercent, int bottomEndPercent, int bottomStartPercent)
        : base(CutCornerShapeKt.CutCornerShape(
            topStartPercent, topEndPercent, bottomEndPercent, bottomStartPercent)) { }
}
