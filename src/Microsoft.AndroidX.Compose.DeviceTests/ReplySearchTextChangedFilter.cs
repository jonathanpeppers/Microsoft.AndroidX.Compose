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
        LastObserved = $"type={e.EventType}; changes={e.ContentChangeTypes}; window={e.WindowId}; text={string.Join(" | ", text)}";
        // Compose emits text/subtree semantics changes without a ViewTextChanged payload.
        bool semanticsChanged = e.EventType == EventTypes.WindowContentChanged &&
            (e.ContentChangeTypes & (ContentChangeTypes.Text | ContentChangeTypes.Subtree)) != 0;
        return e.WindowId == expectedWindowId &&
            (semanticsChanged ||
             e.EventType == EventTypes.ViewTextChanged && text.Contains(expectedText, StringComparer.Ordinal));
    }
}
