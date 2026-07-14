using Android.Runtime;
using Placeable = AndroidX.Compose.UI.Layout.Placeable;

namespace AndroidX.Compose;

/// <summary>
/// A child of a <see cref="Layout"/> that can be measured against a set
/// of <see cref="Constraints"/>. Mirrors
/// <c>androidx.compose.ui.layout.Measurable</c>.
/// </summary>
/// <remarks>
/// You receive <see cref="Measurable"/>s from the
/// <see cref="Layout"/> measure-policy callback and call
/// <see cref="Measure"/> on each one to produce a <see cref="Placeable"/>
/// you can later position via
/// <see cref="MeasureScope.Layout(int, int, Action{PlacementScope})"/>'s
/// placement block. <see cref="Measure"/> is single-shot — calling it
/// twice on the same instance during a measure pass throws.
/// </remarks>
public sealed class Measurable
{
    internal IntPtr Handle { get; }

    internal Measurable(IntPtr handle) => Handle = handle;

    /// <summary>
    /// Measure this child against <paramref name="constraints"/> and
    /// return a <see cref="Placeable"/> that can be positioned inside the
    /// enclosing layout's <see cref="MeasureScope.Layout(int, int, Action{PlacementScope})"/>
    /// placement block.
    /// </summary>
    public Placeable Measure(Constraints constraints)
    {
        IntPtr placeable = ComposeBridges.MeasurableMeasure(Handle, constraints.Value);
        // TransferLocalRef hands the local ref to the peer cache, which
        // promotes it to a global ref. The bound Placeable type provides
        // Width/Height/MeasuredWidth/MeasuredHeight directly.
        return Java.Lang.Object.GetObject<Placeable>(
            placeable, JniHandleOwnership.TransferLocalRef)!;
    }
}
