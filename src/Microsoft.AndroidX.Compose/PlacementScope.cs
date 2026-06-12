using Placeable = AndroidX.Compose.UI.Layout.Placeable;

namespace AndroidX.Compose;

/// <summary>
/// The receiver passed to the <c>placementBlock</c> argument of
/// <see cref="MeasureScope.Layout(int, int, Action{PlacementScope})"/>.
/// Mirrors <c>AndroidX.Compose.UI.Layout.Placeable.PlacementScope</c>. Use
/// <see cref="Place"/> for absolute coordinates and
/// <see cref="PlaceRelative"/> for coordinates that flip in RTL layouts.
/// </summary>
public sealed class PlacementScope
{
    internal IntPtr Handle { get; }

    internal PlacementScope(IntPtr handle) => Handle = handle;

    /// <summary>
    /// Place <paramref name="placeable"/> at
    /// <c>(<paramref name="x"/>, <paramref name="y"/>)</c> in absolute
    /// pixel coordinates from the layout's top-left, regardless of
    /// reading direction. <paramref name="zIndex"/> overrides the
    /// natural draw order — higher values render on top.
    /// </summary>
    public void Place(Placeable placeable, int x, int y, float zIndex = 0f)
    {
        ArgumentNullException.ThrowIfNull(placeable);
        ComposeBridges.PlacementScopePlace(Handle, placeable, x, y, zIndex);
    }

    /// <summary>
    /// Place <paramref name="placeable"/> at
    /// <c>(<paramref name="x"/>, <paramref name="y"/>)</c> in pixels.
    /// In RTL layouts <paramref name="x"/> is mirrored against the
    /// layout's width — use <see cref="Place"/> when you need
    /// absolute coordinates regardless of reading direction.
    /// </summary>
    public void PlaceRelative(Placeable placeable, int x, int y, float zIndex = 0f)
    {
        ArgumentNullException.ThrowIfNull(placeable);
        ComposeBridges.PlacementScopePlaceRelative(Handle, placeable, x, y, zIndex);
    }
}
