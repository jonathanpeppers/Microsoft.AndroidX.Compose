using Android.Runtime;

namespace AndroidX.Compose;

/// <summary>
/// The receiver passed to a <see cref="Layout"/> measure-policy callback.
/// Mirrors <c>androidx.compose.ui.layout.MeasureScope</c>. Use
/// <see cref="Layout(int, int, Action{PlacementScope})"/> to
/// commit the chosen layout size and position the children that were
/// measured via <see cref="Measurable.Measure"/>.
/// </summary>
public sealed class MeasureScope
{
    internal IntPtr Handle { get; }

    internal MeasureScope(IntPtr handle) => Handle = handle;

    /// <summary>
    /// Declare that this layout is <paramref name="width"/> ×
    /// <paramref name="height"/> pixels and place its children via the
    /// <paramref name="placementBlock"/> callback. Must be called exactly
    /// once per measure-policy invocation; the returned
    /// <see cref="MeasureResult"/> is the value the policy should hand
    /// back to <see cref="Layout"/>.
    /// </summary>
    public MeasureResult Layout(int width, int height, Action<PlacementScope> placementBlock)
    {
        ArgumentNullException.ThrowIfNull(placementBlock);
        var lambda = new PlacementBlockLambda(placementBlock);
        IntPtr handle = ComposeBridges.MeasureScopeLayout(Handle, width, height, lambda);
        return new MeasureResult(handle, JniHandleOwnership.TransferLocalRef);
    }
}
