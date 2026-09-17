using Android.Content;
using Android.Views;
using Android.Views.Accessibility;
using AndroidX.Compose.Samples.Jetchat;
using NativeAction = Android.Views.Accessibility.Action;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>J12 restoration and J03/J04 editing through the real sample's native accessibility actions.</summary>
[TestClass]
[DoNotParallelize]
public class JetchatRestorationTests
{
    static TestInstrumentation Runner => TestInstrumentation.Current
        ?? throw new InvalidOperationException("Jetchat restoration requires native instrumentation.");

    /// <summary>Admits the actual editor and proves native caret/focus observation without recreation.</summary>
    [TestMethod]
    public async Task NativeEditor_AdmissionAndSelectionControl()
    {
        var activity = await Start("light");
        try
        {
            await Click(activity, n => n.Editable, "editor");
            await SetText(activity, "  ab\U0001F600cd  ");
            await SetSelection(activity, 4, 4);
            var before = Editor(activity);
            Assert.IsTrue(before.Focused);
            await Click(activity, n => n.ContentDescription == "Show Emoji selector", "emoji toggle");
            var selected = Editor(activity);
            Assert.AreEqual(before.Text, selected.Text);
            Assert.AreEqual(before.Start, selected.Start);
            Assert.AreEqual(before.End, selected.End);
            Assert.IsFalse(selected.Focused);
            AssertPanel(activity, true);
            await Click(activity, n => n.Editable, "editor");
            Assert.IsTrue(Editor(activity).Focused);
            AssertPanel(activity, false);
        }
        finally { await Finish(activity); }
    }

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
            var before = Editor(activity);
            Assert.AreEqual("  Retain386 abcd  ", before.Text);
            Assert.IsFalse(before.Focused, "The emoji panel must take focus from the editor.");
            AssertPanel(activity, true);

            activity = await Recreate(activity);
            Assert.AreEqual(before, Editor(activity), "Restoration must retain native text/caret, not the old wrapper.");
            AssertPanel(activity, true);
            await Click(activity, n => n.ContentDescription == "Emoji \U0001F600", "emoji glyph");
            string expected = before.Text[..before.Start] + "\U0001F600" + before.Text[before.End..];
            Assert.AreEqual(expected, Editor(activity).Text);
            Assert.AreEqual(expected.Length, Editor(activity).Start, "Emoji insertion moves the caret to the buffer end.");
            await Click(activity, n => n.Text == "Send", "Send");
            Assert.AreEqual("", Editor(activity).Text);
            AssertPanel(activity, false);
            using (var message = Find(activity, n => !n.Editable && n.Text == expected))
                Assert.IsNotNull(message, "Visible Send must insert the exact untrimmed restored draft.");

            activity = await Recreate(activity);
            Assert.AreEqual("", Editor(activity).Text, "Sending must replace the saved draft, not resurrect it.");
            AssertPanel(activity, false);
            await Click(activity, n => n.Editable, "editor");
            await SetText(activity, "  Ime386  ");
            await ImeSend(activity);
            Assert.AreEqual("", Editor(activity).Text);
            Assert.IsTrue(Editor(activity).Focused, "IME Send must leave the native editor focused.");
            using (var message = Find(activity, n => !n.Editable && n.Text == "  Ime386  "))
                Assert.IsNotNull(message, "IME Send must also preserve surrounding spaces.");
            await SetText(activity, "   ");
            await ImeSend(activity);
            Assert.AreEqual("   ", Editor(activity).Text, "Whitespace-only Send must not clear the editor.");
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
            AssertPanel(activity, true);
            Runner.SendKeyDownUpSync(Keycode.Back);
            await Settle(activity);
            AssertPanel(activity, false);
            Assert.AreEqual(" owner386 ", Editor(activity).Text);

            await Click(activity, n => n.ContentDescription == "Show Emoji selector", "emoji toggle");
            activity = await Recreate(activity);
            AssertPanel(activity, true);
            await Click(activity, n => n.Editable, "editor");
            AssertPanel(activity, false);
            Assert.IsTrue(Editor(activity).Focused);
            Assert.AreEqual(" owner386 ", Editor(activity).Text);
            await Click(activity, n => n.ContentDescription == "Show Emoji selector", "emoji toggle");
            await Click(activity, n => n.ContentDescription == "Open navigation drawer", "drawer");
            await Click(activity, n => n.Text == "droidcon-nyc", "cosmetic channel");
            Assert.AreEqual(" owner386 ", Editor(activity).Text, "Drawer highlight is not a different conversation.");
            AssertPanel(activity, true);
            await Finish(activity);
            activity = await Start("light");
            Assert.AreEqual("", Editor(activity).Text, "A new activity/composition must not inherit another owner's draft.");
            AssertPanel(activity, false);
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
            AssertPanel(activity, true);
            await activity.OnUi(() => owners.Value = new("#second", 1, []));
            await Settle(activity);
            Assert.AreEqual("", Editor(activity).Text);
            AssertPanel(activity, false);
            await Click(activity, n => n.Editable, "editor");
            await SetText(activity, " second386 ");
            await ImeSend(activity);
            Assert.AreEqual("", Editor(activity).Text);
            Assert.IsTrue(Editor(activity).Focused);
            await activity.OnUi(() =>
            {
                Assert.HasCount(0, first.Messages);
                Assert.HasCount(1, owners.Value.Messages);
                Assert.AreEqual(" second386 ", owners.Value.Messages[0].Content);
            });
        }
        finally { await Finish(activity); }
    }

    /// <summary>An in-flight picker result reconnects to the recreated activity's retained owner.</summary>
    [TestMethod]
    public async Task VideoPicker_InFlightResultReconnectsAfterRecreation()
    {
        var activity = await Start("light");
        try
        {
            var picker = activity.VideoPickerState
                ?? throw new InvalidOperationException("Video picker view model is unavailable.");
            Assert.IsTrue(picker.Begin());
            picker.Disconnect();
            picker.Complete(VideoPickResult.Selected(VideoAttachmentStore.SeedVideoUri(activity)));
            using (var stalePreview = Find(
                activity,
                node => node.ContentDescription == "Attached video preview"))
                Assert.IsNull(stalePreview, "The disconnected old composition must not consume the result.");
            activity = await Recreate(activity);
            Assert.AreSame(picker, activity.VideoPickerState);
            using var preview = Find(
                activity,
                node => node.ContentDescription == "Attached video preview");
            Assert.IsNotNull(
                preview,
                "The replacement composition must consume the buffered picker result.");
        }
        finally { await Finish(activity); }
    }

    static async Task<JetchatRestorationTestActivity> Start(string palette, bool ownerSwitch = false)
    {
        _ = Runner.UiAutomation ?? throw new InvalidOperationException("UiAutomation is unavailable.");
        JetchatRestorationTestActivity.Prepare();
        using var intent = new Intent(global::Android.App.Application.Context, typeof(JetchatRestorationTestActivity));
        intent.AddFlags(ActivityFlags.NewTask);
        intent.PutExtra("test-palette", palette);
        intent.PutExtra("test-owner-switch", ownerSwitch);
        Runner.RunOnMainSync(() => global::Android.App.Application.Context.StartActivity(intent));
        var activity = await JetchatRestorationTestActivity.Started.Task.WaitAsync(TimeSpan.FromSeconds(20));
        try
        {
            await Settle(activity);
            Assert.IsFalse(activity.Restored);
            Report(activity, "started");
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
            await Settle(replacement);
            Assert.AreNotSame(old, replacement);
            Assert.AreNotEqual(old.InstanceId, replacement.InstanceId);
            Assert.IsTrue(old.IsDestroyed && !old.Resumed);
            Assert.IsTrue(replacement.Restored && replacement.Resumed && replacement.HasWindowFocus);
            Report(replacement, $"recreated; old={old.InstanceId}; oldDestroyed={old.IsDestroyed}; oldResumed={old.Resumed}");
            _ = Editor(replacement);
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
        Report(activity, $"click {name}");
        using var node = Find(activity, predicate) ?? throw new InvalidOperationException($"Jetchat {name} is missing.");
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
        using var editor = Find(activity, n => n.Editable) ?? throw new InvalidOperationException("Jetchat editor is missing.");
        using var args = new Bundle();
        args.PutCharSequence(AccessibilityNodeInfo.ActionArgumentSetTextCharsequence, text);
        Assert.IsTrue(editor.PerformAction(NativeAction.SetText, args));
        await Settle(activity);
        Assert.AreEqual(text, Editor(activity).Text);
    }

    static async Task SetSelection(JetchatRestorationTestActivity activity, int start, int end)
    {
        using var editor = Find(activity, n => n.Editable) ?? throw new InvalidOperationException("Jetchat editor is missing.");
        using var args = new Bundle();
        args.PutInt(AccessibilityNodeInfo.ActionArgumentSelectionStartInt, start);
        args.PutInt(AccessibilityNodeInfo.ActionArgumentSelectionEndInt, end);
        Assert.IsTrue(editor.PerformAction(NativeAction.SetSelection, args));
        await Settle(activity);
        Assert.AreEqual(start, Editor(activity).Start);
        Assert.AreEqual(end, Editor(activity).End);
    }

    static async Task ImeSend(JetchatRestorationTestActivity activity)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(30))
        {
            Assert.Inconclusive("Native accessibility IME actions require Android 11 or later.");
            return;
        }
        using var editor = Find(activity, n => n.Editable) ?? throw new InvalidOperationException("Jetchat editor is missing.");
        var action = AccessibilityNodeInfo.AccessibilityAction.ActionImeEnter
            ?? throw new InvalidOperationException("Native IME action is unavailable.");
        Assert.IsTrue(editor.ActionList?.Any(a => a.Id == action.Id) == true, "The editor must advertise its native IME Send action.");
        Assert.IsTrue(editor.PerformAction((NativeAction)action.Id));
        await Settle(activity);
    }

    static async Task Settle(JetchatRestorationTestActivity activity)
    {
        await activity.AtNativeIdle();
        var frame = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var secondFrame = new Java.Lang.Runnable(() => frame.TrySetResult());
        using var firstFrame = new Java.Lang.Runnable(() => activity.ComposeRoot.PostOnAnimation(secondFrame));
        await activity.OnUi(() => activity.ComposeRoot.PostOnAnimation(firstFrame));
        await frame.Task.WaitAsync(TimeSpan.FromSeconds(10));
        var automation = Runner.UiAutomation ?? throw new InvalidOperationException("UiAutomation is unavailable.");
        automation.WaitForIdle(200, 5000);
        await activity.AtNativeIdle();
    }

    static (string Text, int Start, int End, bool Focused) Editor(JetchatRestorationTestActivity activity)
    {
        (string Text, int Start, int End, bool Focused) snapshot = ("", 0, 0, false);
        Runner.RunOnMainSync(() => snapshot = activity.ReadNativeEditor());
        using var node = Find(activity, n => n.Editable) ?? throw new InvalidOperationException("Native Jetchat editor is missing.");
        Assert.AreEqual(snapshot.Text, node.Text ?? "", "Native semantics and the visible accessible editor text must agree.");
        Report(activity, $"nativeEditor={snapshot}; accessibilitySelection={node.TextSelectionStart}..{node.TextSelectionEnd}");
        return snapshot;
    }

    static void AssertPanel(JetchatRestorationTestActivity activity, bool present)
    {
        using var panel = Find(activity, n => n.ContentDescription == "Emoji selector");
        Assert.AreEqual(present, panel is not null, "Native emoji selector presence differs.");
    }

    static AccessibilityNodeInfo? Find(JetchatRestorationTestActivity activity, Func<AccessibilityNodeInfo, bool> predicate)
    {
        var automation = Runner.UiAutomation ?? throw new InvalidOperationException("UiAutomation is unavailable.");
        automation.WaitForIdle(200, 5000);
        if (OperatingSystem.IsAndroidVersionAtLeast(34))
            Assert.IsTrue(automation.ClearCache());
        using var root = automation.RootInActiveWindow ?? throw new InvalidOperationException("No native active window.");
        Assert.AreEqual("net.compose.devicetests", root.PackageName, "Do not inspect an unowned native window.");
        int windowId = -1;
        Runner.RunOnMainSync(() =>
        {
            Assert.IsTrue(activity.Resumed && activity.HasWindowFocus && !activity.IsDestroyed && !activity.IsFinishing,
                "Accessibility observation requires this activity to remain resumed and focused.");
            using var owned = activity.ComposeRoot.CreateAccessibilityNodeInfo()
                ?? throw new InvalidOperationException("The owned ComposeView has no accessibility window.");
            windowId = owned.WindowId;
        });
        Assert.IsTrue(windowId >= 0 && root.WindowId >= 0, "An invalid native window ID cannot establish ownership.");
        Assert.AreEqual(windowId, root.WindowId, "The active accessibility root must belong to this activity instance.");
        return FindIn(root, n => n.VisibleToUser && predicate(n));
    }

    static void Report(JetchatRestorationTestActivity activity, string phase)
    {
        using var status = new Bundle();
        status.PutString("jetchatStage", phase);
        status.PutString("activityInstance", activity.InstanceId.ToString());
        status.PutInt("pid", global::Android.OS.Process.MyPid());
        Runner.RunOnMainSync(() =>
        {
            status.PutBoolean("resumed", activity.Resumed);
            status.PutBoolean("focusedWindow", activity.HasWindowFocus);
            status.PutBoolean("restoredBundle", activity.Restored);
        });
        Runner.SendStatus(0, status);
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
