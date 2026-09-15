using Android.Views.Accessibility;

namespace Microsoft.AndroidX.Compose.DeviceTests;

internal sealed class ReplySearchTextChangedFilter(string expectedText, int expectedWindowId)
    : Java.Lang.Object, UiAutomation.IAccessibilityEventFilter
{
    internal string LastObserved { get; private set; } = "No owned accessibility event observed.";

    public bool Accept(AccessibilityEvent? e)
    {
        if (e?.PackageName != "net.compose.devicetests")
            return false;

        string[] text = e.Text?.Select(value => value?.ToString() ?? "").ToArray() ?? [];
        LastObserved = $"type={e.EventType}; window={e.WindowId}; text={string.Join(" | ", text)}";
        return e.EventType == EventTypes.ViewTextChanged &&
            e.WindowId == expectedWindowId &&
            text.Contains(expectedText, StringComparer.Ordinal);
    }
}
