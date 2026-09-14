using Android.Content;
using Android.Views;

namespace Microsoft.AndroidX.Compose.DeviceTests;

internal sealed class ScaffoldInsetsRootView(Context context) : FrameLayout(context)
{
    internal Action? InsetsApplied { get; set; }

    /// <summary>Observes platform delivery on the test-owned parent and preserves normal child dispatch.</summary>
    public override WindowInsets? DispatchApplyWindowInsets(WindowInsets? insets)
    {
        ArgumentNullException.ThrowIfNull(insets);
        var dispatched = base.DispatchApplyWindowInsets(insets);
        InsetsApplied?.Invoke();
        return dispatched;
    }
}
