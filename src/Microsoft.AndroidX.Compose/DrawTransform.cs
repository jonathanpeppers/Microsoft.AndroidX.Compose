using BoundTransform = AndroidX.Compose.UI.Graphics.Drawscope.IDrawTransform;

namespace AndroidX.Compose;

/// <summary>
/// Transform and clipping operations available inside
/// <see cref="DrawScope.WithTransform(Action{DrawTransform}, Action{DrawScope})"/>.
/// </summary>
/// <remarks>
/// Instances are valid only while the transform callback is running and must
/// not be retained.
/// </remarks>
public sealed class DrawTransform
{
    BoundTransform? _jvm;

    internal DrawTransform(BoundTransform jvm) => _jvm = jvm;

    BoundTransform Jvm => _jvm
        ?? throw new InvalidOperationException("DrawTransform is no longer active.");

    /// <summary>Current logical drawing bounds after any applied insets.</summary>
    public Size Size => Size.FromPacked(Jvm.Size);

    /// <summary>Center of the current logical drawing bounds.</summary>
    public Offset Center => Offset.FromPacked(Jvm.Center);

    /// <summary>Intersects or subtracts a rectangle from the current clip.</summary>
    public void ClipRect(Rect bounds, ClipOperation operation = ClipOperation.Intersect) =>
        Jvm.ClipRect(bounds.Left, bounds.Top, bounds.Right, bounds.Bottom, (int)operation);

    /// <summary>Intersects or subtracts a path from the current clip.</summary>
    public void ClipPath(Path path, ClipOperation operation = ClipOperation.Intersect)
    {
        ArgumentNullException.ThrowIfNull(path);
        Jvm.ClipPath(path.Jvm, (int)operation);
    }

    /// <summary>Translates the coordinate space by a pixel offset.</summary>
    public void Translate(Offset offset) => Jvm.Translate(offset.X, offset.Y);

    /// <summary>Rotates the coordinate space clockwise around a pivot.</summary>
    public void Rotate(float degrees, Offset? pivot = null) =>
        Jvm.Rotate(degrees, (pivot ?? Center).Packed);

    /// <summary>Scales both axes around a pivot.</summary>
    public void Scale(float scale, Offset? pivot = null) =>
        Jvm.Scale(scale, scale, (pivot ?? Center).Packed);

    /// <summary>Scales each axis independently around a pivot.</summary>
    public void Scale(float scaleX, float scaleY, Offset? pivot = null) =>
        Jvm.Scale(scaleX, scaleY, (pivot ?? Center).Packed);

    /// <summary>
    /// Insets the logical drawing bounds and translates the coordinate origin.
    /// </summary>
    public void Inset(float left, float top, float right, float bottom) =>
        Jvm.Inset(left, top, right, bottom);

    /// <summary>Concatenates a Compose 4-by-4 transform matrix.</summary>
    public void Transform(float[] matrix)
    {
        ArgumentNullException.ThrowIfNull(matrix);
        if (matrix.Length != 16)
            throw new ArgumentException("A Compose transform matrix must contain 16 values.", nameof(matrix));
        Jvm.Transform(matrix);
    }

    internal void Invalidate() => _jvm = null;
}
