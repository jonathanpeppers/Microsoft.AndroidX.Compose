using BoundPath = AndroidX.Compose.UI.Graphics.IPath;
using AndroidPathFactory = AndroidX.Compose.UI.Graphics.AndroidPath_androidKt;
using NativePath = Android.Graphics.Path;

namespace AndroidX.Compose;

/// <summary>
/// Mutable vector geometry for <see cref="DrawScope.DrawPath(Path, Color, float, AndroidX.Compose.UI.Graphics.Drawscope.DrawStyle?)"/>
/// and related drawing operations.
/// </summary>
public sealed class Path : IDisposable
{
    internal BoundPath Jvm { get; }

    /// <summary>Creates an empty path.</summary>
    public Path()
    {
        Jvm = AndroidPathFactory.Path()
            ?? throw new InvalidOperationException("Compose Path factory returned null.");
    }

    /// <summary>Creates a copy of another path.</summary>
    public Path(Path source)
    {
        ArgumentNullException.ThrowIfNull(source);
        Jvm = AndroidX.Compose.UI.Graphics.PathKt.Copy(source.Jvm)
            ?? throw new InvalidOperationException("Compose Path copy returned null.");
    }

    /// <summary>Gets or sets the winding rule.</summary>
    public PathFillType FillType
    {
        get => (PathFillType)Jvm.FillType;
        set => Jvm.FillType = (int)value;
    }

    /// <summary>Whether this path has no contours.</summary>
    public bool IsEmpty => Jvm.IsEmpty;

    /// <summary>Whether this path is convex.</summary>
    public bool IsConvex => Jvm.IsConvex;

    /// <summary>Starts a new contour at the supplied coordinates.</summary>
    public Path MoveTo(float x, float y) { Jvm.MoveTo(x, y); return this; }

    /// <summary>Moves relative to the current point without adding a segment.</summary>
    public Path RelativeMoveTo(float dx, float dy) { Jvm.RelativeMoveTo(dx, dy); return this; }

    /// <summary>Adds a straight segment.</summary>
    public Path LineTo(float x, float y) { Jvm.LineTo(x, y); return this; }

    /// <summary>Adds a straight segment relative to the current point.</summary>
    public Path RelativeLineTo(float dx, float dy) { Jvm.RelativeLineTo(dx, dy); return this; }

    /// <summary>Adds a quadratic Bezier segment.</summary>
    public Path QuadraticBezierTo(float x1, float y1, float x2, float y2)
    {
        Jvm.QuadraticTo(x1, y1, x2, y2);
        return this;
    }

    /// <summary>Adds a relative quadratic Bezier segment.</summary>
    public Path RelativeQuadraticBezierTo(float dx1, float dy1, float dx2, float dy2)
    {
        Jvm.RelativeQuadraticTo(dx1, dy1, dx2, dy2);
        return this;
    }

    /// <summary>Adds a cubic Bezier segment.</summary>
    public Path CubicTo(float x1, float y1, float x2, float y2, float x3, float y3)
    {
        Jvm.CubicTo(x1, y1, x2, y2, x3, y3);
        return this;
    }

    /// <summary>Adds a relative cubic Bezier segment.</summary>
    public Path RelativeCubicTo(float dx1, float dy1, float dx2, float dy2, float dx3, float dy3)
    {
        Jvm.RelativeCubicTo(dx1, dy1, dx2, dy2, dx3, dy3);
        return this;
    }

    /// <summary>Adds an arc segment within the supplied oval bounds.</summary>
    public Path ArcTo(
        Rect oval,
        float startAngle,
        float sweepAngle,
        bool forceMoveTo = false)
    {
        WithNative(path =>
        {
            using var bounds = NativeRect(oval);
            path.ArcTo(bounds, startAngle, sweepAngle, forceMoveTo);
        });
        return this;
    }

    /// <summary>Adds a rectangular contour.</summary>
    public Path AddRect(Rect rect)
    {
        WithNative(path => path.AddRect(
            rect.Left, rect.Top, rect.Right, rect.Bottom, Clockwise));
        return this;
    }

    /// <summary>Adds an oval contour.</summary>
    public Path AddOval(Rect oval)
    {
        WithNative(path =>
        {
            using var bounds = NativeRect(oval);
            path.AddOval(bounds, Clockwise);
        });
        return this;
    }

    /// <summary>Adds a rounded-rectangle contour.</summary>
    public Path AddRoundRect(Rect rect, CornerRadius cornerRadius)
    {
        WithNative(path =>
        {
            using var bounds = NativeRect(rect);
            path.AddRoundRect(
                bounds, cornerRadius.X, cornerRadius.Y, Clockwise);
        });
        return this;
    }

    /// <summary>Adds a standalone arc contour within the supplied oval bounds.</summary>
    public Path AddArc(Rect oval, float startAngle, float sweepAngle)
    {
        WithNative(path =>
        {
            using var bounds = NativeRect(oval);
            path.AddArc(bounds, startAngle, sweepAngle);
        });
        return this;
    }

    /// <summary>Adds another path at an optional offset.</summary>
    public Path AddPath(Path path, Offset offset = default)
    {
        ArgumentNullException.ThrowIfNull(path);
        Jvm.AddPath(path.Jvm, offset.Packed);
        return this;
    }

    /// <summary>Closes the current contour.</summary>
    public Path Close() { Jvm.Close(); return this; }

    /// <summary>Clears all contours and resets fill state.</summary>
    public Path Reset() { Jvm.Reset(); return this; }

    /// <summary>Clears all contours while retaining reusable storage.</summary>
    public Path Rewind() { Jvm.Rewind(); return this; }

    /// <summary>Translates every contour.</summary>
    public Path Translate(Offset offset) { Jvm.Translate(offset.Packed); return this; }

    /// <summary>Replaces this path with the selected boolean combination.</summary>
    public bool Op(Path first, Path second, PathOperation operation)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        return Jvm.Op(first.Jvm, second.Jvm, (int)operation);
    }

    /// <summary>Releases the underlying Compose path peer.</summary>
    public void Dispose() => Jvm.Dispose();

    void WithNative(Action<NativePath> action)
    {
        var native = AndroidPathFactory.AsAndroidPath(Jvm)
            ?? throw new InvalidOperationException("Compose Path had no Android backing path.");
        try
        {
            action(native);
        }
        finally
        {
            GC.KeepAlive(native);
            GC.KeepAlive(Jvm);
        }
    }

    static Android.Graphics.RectF NativeRect(Rect rect) =>
        new(rect.Left, rect.Top, rect.Right, rect.Bottom);

    static NativePath.Direction Clockwise =>
        NativePath.Direction.Cw
        ?? throw new InvalidOperationException("Android Path clockwise direction was unavailable.");
}
