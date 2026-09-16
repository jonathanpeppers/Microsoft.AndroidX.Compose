using Android.Content;
using Android.Views;
using Android.Views.Accessibility;
using NativeAction = Android.Views.Accessibility.Action;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>J12 restoration and J03/J04 editing through the real sample's native accessibility actions.</summary>
[TestClass]
[DoNotParallelize]
public class JetchatRestorationTests
{
    static TestInstrumentation Runner => TestInstrumentation.Current
        ?? throw new InvalidOperationException("Jetchat restoration requires native instrumentation.");

    /// <summary>A destroyed activity is replaced with the saved native draft, caret and emoji panel.</summary>
    [TestMethod]
    [DataRow("light")]
    [DataRow("dark")]
    public async Task DraftAndEmoji_RestoreThenSendAndClear(string palette)
    {
        var activity = await Start(palette);
        try
        {
            await Click(activity, n => n.Editable, "editor");
            await SetText(activity, "  Retain386 abcd  ");
            await SetSelection(activity, 13, 13);
            await Click(activity, n => n.ContentDescription == "Show Emoji selector", "emoji toggle");
            var before = Editor();
            Assert.AreEqual("  Retain386 abcd  ", before.Text);
            Assert.IsFalse(before.Focused, "The emoji panel must take focus from the editor.");
            AssertPanel(true);

            activity = await Recreate(activity);
            Assert.AreEqual(before, Editor(), "Restoration must retain native text/caret, not the old wrapper.");
            AssertPanel(true);
            await Click(activity, n => n.ContentDescription == "Emoji \U0001F600", "emoji glyph");
            string expected = before.Text[..before.Start] + "\U0001F600" + before.Text[before.End..];
            Assert.AreEqual(expected, Editor().Text);
            Assert.AreEqual(expected.Length, Editor().Start, "Emoji insertion moves the caret to the buffer end.");
            await Click(activity, n => n.Text == "Send", "Send");
            Assert.AreEqual("", Editor().Text);
            AssertPanel(false);
            using (var message = Find(n => !n.Editable && n.Text == expected))
                Assert.IsNotNull(message, "Visible Send must insert the exact untrimmed restored draft.");

            activity = await Recreate(activity);
            Assert.AreEqual("", Editor().Text, "Sending must replace the saved draft, not resurrect it.");
            AssertPanel(false);
            await Click(activity, n => n.Editable, "editor");
            await SetText(activity, "  Ime386  ");
            await ImeSend(activity);
            Assert.AreEqual("", Editor().Text);
            Assert.IsTrue(Editor().Focused, "IME Send must leave the native editor focused.");
            using (var message = Find(n => !n.Editable && n.Text == "  Ime386  "))
                Assert.IsNotNull(message, "IME Send must also preserve surrounding spaces.");
            await SetText(activity, "   ");
            await ImeSend(activity);
            Assert.AreEqual("   ", Editor().Text, "Whitespace-only Send must not clear the editor.");
        }
        finally { await Finish(activity); }
    }

    /// <summary>Restored selectors still dismiss on Back/editor focus and do not leak to a new composition.</summary>
    [TestMethod]
    public async Task RestoredSelector_BackFocusNavigationAndFreshActivity()
    {
        var activity = await Start("light");
        try
        {
            await SetText(activity, " owner386 ");
            await Click(activity, n => n.ContentDescription == "Show Emoji selector", "emoji toggle");
            activity = await Recreate(activity);
            AssertPanel(true);
            Runner.SendKeyDownUpSync(Keycode.Back);
            await activity.AtNativeIdle();
            AssertPanel(false);
            Assert.AreEqual(" owner386 ", Editor().Text);

            await Click(activity, n => n.ContentDescription == "Show Emoji selector", "emoji toggle");
            activity = await Recreate(activity);
            AssertPanel(true);
            await Click(activity, n => n.Editable, "editor");
            AssertPanel(false);
            Assert.IsTrue(Editor().Focused);
            Assert.AreEqual(" owner386 ", Editor().Text);
            await Click(activity, n => n.ContentDescription == "Show Emoji selector", "emoji toggle");
            await Click(activity, n => n.ContentDescription == "Open navigation drawer", "drawer");
            await Click(activity, n => n.Text == "droidcon-nyc", "cosmetic channel");
            Assert.AreEqual(" owner386 ", Editor().Text, "Drawer highlight is not a different conversation.");
            AssertPanel(true);
            await Finish(activity);
            activity = await Start("light");
            Assert.AreEqual("", Editor().Text, "A new activity/composition must not inherit another owner's draft.");
            AssertPanel(false);
        }
        finally { await Finish(activity); }
    }

    /// <summary>A real channel-key change resets the UI and rebinds IME callbacks to the replacement owner.</summary>
    [TestMethod]
    public async Task ChangedConversationKey_ResetsDraftSelectorAndImeTarget()
    {
        var activity = await Start("light", ownerSwitch: true);
        try
        {
            var owners = activity.Owners ?? throw new InvalidOperationException("Conversation owners are unavailable.");
            var first = owners.Value;
            await SetText(activity, " first386 ");
            await Click(activity, n => n.ContentDescription == "Show Emoji selector", "emoji toggle");
            AssertPanel(true);
            await activity.OnUi(() => owners.Value = new("#second", 1, []));
            await Settle(activity);
            Assert.AreEqual("", Editor().Text);
            AssertPanel(false);
            await Click(activity, n => n.Editable, "editor");
            await SetText(activity, " second386 ");
            await ImeSend(activity);
            Assert.AreEqual("", Editor().Text);
            Assert.IsTrue(Editor().Focused);
            await activity.OnUi(() =>
            {
                Assert.HasCount(0, first.Messages);
                Assert.HasCount(1, owners.Value.Messages);
                Assert.AreEqual(" second386 ", owners.Value.Messages[0].Content);
            });
        }
        finally { await Finish(activity); }
    }

    static async Task<JetchatRestorationTestActivity> Start(string palette, bool ownerSwitch = false)
    {
        JetchatRestorationTestActivity.Prepare();
        using var intent = new Intent(global::Android.App.Application.Context, typeof(JetchatRestorationTestActivity));
        intent.AddFlags(ActivityFlags.NewTask);
        intent.PutExtra("test-palette", palette);
        intent.PutExtra("test-owner-switch", ownerSwitch);
        Runner.RunOnMainSync(() => global::Android.App.Application.Context.StartActivity(intent));
        var activity = await JetchatRestorationTestActivity.Started.Task.WaitAsync(TimeSpan.FromSeconds(20));
        try
        {
            await activity.AtNativeIdle();
            Assert.IsFalse(activity.Restored);
            return activity;
        }
        catch { await Finish(activity); throw; }
    }

    static async Task<JetchatRestorationTestActivity> Recreate(JetchatRestorationTestActivity old)
    {
        JetchatRestorationTestActivity.Prepare();
        await old.OnUi(old.Recreate);
        var replacement = await JetchatRestorationTestActivity.Started.Task.WaitAsync(TimeSpan.FromSeconds(20));
        try
        {
            await old.Destroyed.Task.WaitAsync(TimeSpan.FromSeconds(20));
            await replacement.AtNativeIdle();
            Assert.AreNotSame(old, replacement);
            Assert.AreNotEqual(old.InstanceId, replacement.InstanceId);
            Assert.IsTrue(old.IsDestroyed && !old.Resumed);
            Assert.IsTrue(replacement.Restored && replacement.Resumed && replacement.HasWindowFocus);
            Console.WriteLine($"J12 pid={(global::Android.OS.Process.MyPid())}, old={old.InstanceId} destroyed, " +
                $"new={replacement.InstanceId} resumed, savedBundle={replacement.Restored}, editor={Editor()}");
            return replacement;
        }
        catch { await Finish(replacement); throw; }
    }

    static async Task Finish(JetchatRestorationTestActivity activity)
    {
        if (!activity.Destroyed.Task.IsCompleted)
        {
            await activity.OnUi(activity.Finish);
            await activity.Destroyed.Task.WaitAsync(TimeSpan.FromSeconds(20));
        }
        JetchatRestorationTestActivity.Prepare();
    }

    static async Task Click(JetchatRestorationTestActivity activity,
        Func<AccessibilityNodeInfo, bool> predicate, string name)
    {
        await activity.AtNativeIdle();
        using var node = Find(predicate) ?? throw new InvalidOperationException($"Jetchat {name} is missing.");
        var target = node;
        try
        {
            while (!target.Clickable)
            {
                var parent = target.Parent ?? throw new InvalidOperationException($"{name} has no clickable ancestor.");
                if (!ReferenceEquals(target, node)) target.Dispose();
                target = parent;
            }
            Assert.IsTrue(target.PerformAction(NativeAction.Click), $"Native {name} click failed.");
        }
        finally { if (!ReferenceEquals(target, node)) target.Dispose(); }
        await Settle(activity);
    }

    static async Task SetText(JetchatRestorationTestActivity activity, string text)
    {
        using var editor = Find(n => n.Editable) ?? throw new InvalidOperationException("Jetchat editor is missing.");
        using var args = new Bundle();
        args.PutCharSequence(AccessibilityNodeInfo.ActionArgumentSetTextCharsequence, text);
        Assert.IsTrue(editor.PerformAction(NativeAction.SetText, args));
        await Settle(activity);
        Assert.AreEqual(text, Editor().Text);
    }

    static async Task SetSelection(JetchatRestorationTestActivity activity, int start, int end)
    {
        using var editor = Find(n => n.Editable) ?? throw new InvalidOperationException("Jetchat editor is missing.");
        using var args = new Bundle();
        args.PutInt(AccessibilityNodeInfo.ActionArgumentSelectionStartInt, start);
        args.PutInt(AccessibilityNodeInfo.ActionArgumentSelectionEndInt, end);
        Assert.IsTrue(editor.PerformAction(NativeAction.SetSelection, args));
        await Settle(activity);
        Assert.AreEqual(start, Editor().Start);
        Assert.AreEqual(end, Editor().End);
    }

    static async Task ImeSend(JetchatRestorationTestActivity activity)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(30))
            throw new PlatformNotSupportedException("Native accessibility IME actions require Android 11 or later.");
        using var editor = Find(n => n.Editable) ?? throw new InvalidOperationException("Jetchat editor is missing.");
        var action = AccessibilityNodeInfo.AccessibilityAction.ActionImeEnter
            ?? throw new InvalidOperationException("Native IME action is unavailable.");
        Assert.IsTrue(editor.ActionList?.Any(a => a.Id == action.Id) == true, "The editor must advertise its native IME Send action.");
        Assert.IsTrue(editor.PerformAction((NativeAction)action.Id));
        await Settle(activity);
    }

    static async Task Settle(JetchatRestorationTestActivity activity)
    {
        await activity.AtNativeIdle();
        var automation = Runner.UiAutomation ?? throw new InvalidOperationException("UiAutomation is unavailable.");
        automation.WaitForIdle(200, 5000);
        await activity.AtNativeIdle();
    }

    static (string Text, int Start, int End, bool Focused) Editor()
    {
        using var node = Find(n => n.Editable) ?? throw new InvalidOperationException("Native Jetchat editor is missing.");
        return (node.Text ?? "", node.TextSelectionStart, node.TextSelectionEnd, node.Focused);
    }

    static void AssertPanel(bool present)
    {
        using var panel = Find(n => n.ContentDescription == "Emoji selector");
        Assert.AreEqual(present, panel is not null, "Native emoji selector presence differs.");
    }

    static AccessibilityNodeInfo? Find(Func<AccessibilityNodeInfo, bool> predicate)
    {
        var automation = Runner.UiAutomation ?? throw new InvalidOperationException("UiAutomation is unavailable.");
        if (OperatingSystem.IsAndroidVersionAtLeast(34))
            Assert.IsTrue(automation.ClearCache());
        using var root = automation.RootInActiveWindow ?? throw new InvalidOperationException("No native active window.");
        Assert.AreEqual("net.compose.devicetests", root.PackageName, "Do not inspect an unowned native window.");
        return FindIn(root, n => n.VisibleToUser && predicate(n));
    }

    static AccessibilityNodeInfo? FindIn(AccessibilityNodeInfo node, Func<AccessibilityNodeInfo, bool> predicate)
    {
        if (predicate(node))
            return OperatingSystem.IsAndroidVersionAtLeast(33)
                ? new AccessibilityNodeInfo(node) : AccessibilityNodeInfo.Obtain(node);
        for (int i = 0; i < node.ChildCount; i++)
        {
            using var child = node.GetChild(i);
            if (child is not null && FindIn(child, predicate) is { } found) return found;
        }
        return null;
    }
}
