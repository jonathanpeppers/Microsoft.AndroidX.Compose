using BoundDrawStyle = AndroidX.Compose.UI.Graphics.Drawscope.DrawStyle;
using BoundFill = AndroidX.Compose.UI.Graphics.Drawscope.Fill;

namespace AndroidX.Compose;

/// <summary>Factories for fill and stroke styles accepted by drawing primitives.</summary>
public static class DrawingStyle
{
    /// <summary>Paints the interior of a shape.</summary>
    public static BoundDrawStyle Fill { get; } = BoundFill.Instance;

    /// <summary>Paints only a shape's outline.</summary>
    public static BoundDrawStyle Stroke(
        float width,
        StrokeCap cap = StrokeCap.Butt,
        StrokeJoin join = StrokeJoin.Miter,
        float miter = 4f)
    {
        if (width < 0f)
            throw new ArgumentOutOfRangeException(nameof(width));
        if (miter < 0f)
            throw new ArgumentOutOfRangeException(nameof(miter));
        return ComposeBridges.DrawStroke(width, miter, (int)cap, (int)join, null, IntPtr.Zero);
    }
}
