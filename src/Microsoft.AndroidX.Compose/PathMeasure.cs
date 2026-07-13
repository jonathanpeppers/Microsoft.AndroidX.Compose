using BoundPathMeasure = AndroidX.Compose.UI.Graphics.IPathMeasure;
using PathMeasureFactory = AndroidX.Compose.UI.Graphics.AndroidPathMeasure_androidKt;

namespace AndroidX.Compose;

/// <summary>Measures distance, position, tangent, and segments along a path.</summary>
public sealed class PathMeasure : IDisposable
{
    readonly BoundPathMeasure _jvm;

    /// <summary>Creates a path measure, optionally initialized with a path.</summary>
    public PathMeasure(Path? path = null, bool forceClosed = false)
    {
        _jvm = PathMeasureFactory.PathMeasure()
            ?? throw new InvalidOperationException("Compose PathMeasure factory returned null.");
        if (path is not null)
            _jvm.SetPath(path.Jvm, forceClosed);
    }

    /// <summary>Length of the current contour in pixels.</summary>
    public float Length => _jvm.Length;

    /// <summary>Assigns the path to measure, or clears it when <paramref name="path"/> is null.</summary>
    public void SetPath(Path? path, bool forceClosed = false) =>
        _jvm.SetPath(path?.Jvm, forceClosed);

    /// <summary>Returns the position at a distance along the current contour.</summary>
    public Offset GetPosition(float distance) => Offset.FromPacked(_jvm.GetPosition(distance));

    /// <summary>Returns the unit tangent at a distance along the current contour.</summary>
    public Offset GetTangent(float distance) => Offset.FromPacked(_jvm.GetTangent(distance));

    /// <summary>Copies a distance range from the current contour into a destination path.</summary>
    public bool GetSegment(
        float startDistance,
        float stopDistance,
        Path destination,
        bool startWithMoveTo = true)
    {
        ArgumentNullException.ThrowIfNull(destination);
        return _jvm.GetSegment(
            startDistance, stopDistance, destination.Jvm, startWithMoveTo);
    }

    /// <summary>Releases the underlying Compose path-measure peer.</summary>
    public void Dispose() => _jvm.Dispose();
}
