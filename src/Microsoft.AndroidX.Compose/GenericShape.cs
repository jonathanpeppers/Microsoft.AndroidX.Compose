using Android.Runtime;
using AndroidX.Compose.UI.Unit;
using BoundGenericShape = AndroidX.Compose.Foundation.Shape.GenericShape;

namespace AndroidX.Compose;

/// <summary>A Compose shape whose outline is built by a managed path callback.</summary>
/// <remarks>
/// The builder runs when Compose needs an outline, not during composition.
/// Coordinates and <see cref="Size"/> are pixels. Layout direction is forwarded
/// unchanged; the builder decides whether to mirror its geometry. Compose closes
/// the resulting path. The supplied path is borrowed and is valid only during
/// the callback; use <c>new Path(path)</c> to retain an independent copy.
/// The native shape retains one callback peer for its lifetime, including after
/// the managed shape wrapper is collected or disposed while Kotlin still uses it.
/// Remember the shape to preserve identity across recomposition, and create a
/// new shape when captured geometry changes: changing a capture alone does not
/// invalidate Compose's cached outline.
/// </remarks>
[Register("net/compose/GenericShape", DoNotGenerateAcw = true)]
public sealed class GenericShape : Shape
{
    /// <summary>Creates a shape with a retained, non-composable path builder.</summary>
    public GenericShape(Action<Path, Size, LayoutDirection> builder)
        : base(Create(builder))
    {
    }

    static BoundGenericShape Create(Action<Path, Size, LayoutDirection> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        var callback = new ShapePathCallback(builder);
        try
        {
            return ComposeBridges.GenericShapeCreate(callback);
        }
        catch
        {
            callback.Dispose();
            throw;
        }
    }
}
