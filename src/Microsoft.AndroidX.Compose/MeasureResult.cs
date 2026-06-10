using Android.Runtime;

namespace AndroidX.Compose;

/// <summary>
/// Opaque result of <see cref="MeasureScope.Layout(int, int, Action{PlacementScope})"/>.
/// Returned by a <see cref="Layout"/> measure-policy callback so Compose
/// can pick up the chosen size and the registered placement block.
/// Mirrors <c>androidx.compose.ui.layout.MeasureResult</c>.
/// </summary>
public sealed class MeasureResult : Java.Lang.Object
{
    internal MeasureResult(IntPtr handle, JniHandleOwnership transfer)
        : base(handle, transfer) { }
}
