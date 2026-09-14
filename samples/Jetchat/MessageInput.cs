using AndroidX.Compose.UI.Text;
using AndroidX.Compose.UI.Text.Input;

namespace AndroidX.Compose.Samples.Jetchat;

internal static class MessageInput
{
    internal static void Send(MutableState<TextFieldValue> input, Action<string> send,
        Action resetScroll, Action dismissSelector)
    {
        if (string.IsNullOrWhiteSpace(input.Value.Text))
            return;
        send(input.Value.Text);
        input.Value = ComposeExtensions.NewTextFieldValue();
        resetScroll();
        dismissSelector();
    }

    internal static TextFieldValue Insert(TextFieldValue current, string text)
    {
        int start = (int)(current.Selection >> 32);
        int end = (int)current.Selection;
        int min = Math.Min(start, end);
        int max = Math.Max(start, end);
        var updated = string.Concat(current.Text.AsSpan(0, min), text, current.Text.AsSpan(max));
        // Upstream addText replaces the selection, then moves to the buffer end.
        return current.Copy(updated, TextRangeKt.TextRange(updated.Length), current.Composition);
    }
}
