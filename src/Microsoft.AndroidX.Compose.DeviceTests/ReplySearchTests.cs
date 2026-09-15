using Android.Content;
using Android.Views;
using Android.Views.Accessibility;
using AndroidX.Compose.Samples.Reply;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Pinned Reply prefix matching and native search/selection lifecycle regressions.</summary>
[TestClass]
[DoNotParallelize]
public class ReplySearchTests
{
    static TestInstrumentation Runner => TestInstrumentation.Current
        ?? throw new InvalidOperationException("Reply search tests require native instrumentation.");

    [TestMethod]
    public void Matching_UsesSubjectOrFullNamePrefixAndOriginalOrder()
    {
        var emails = LocalEmailsDataProvider.AllEmails;
        long[] bonjour = [2];
        long[] ali = [1];
        CollectionAssert.AreEqual(bonjour, Match("bOnJoUr"));
        CollectionAssert.AreEqual(ali, Match("Ali "));
        Assert.AreEqual(0, Match("").Length);
        Assert.AreEqual(0, Match("Paris").Length, "A substring is not a prefix.");
        Assert.AreEqual(0, Match(" Bonjour").Length, "Do not trim the query.");
        Assert.AreEqual(0, Match("Cucumber").Length, "Do not search bodies.");
        Assert.AreEqual(0, Match("no-such-email").Length);
        var expected = emails.Where(e => e.Subject.StartsWith("Re", StringComparison.OrdinalIgnoreCase) ||
            e.Sender.FullName.StartsWith("Re", StringComparison.OrdinalIgnoreCase)).ToArray();
        var actual = ReplySearchSession.FindMatches(emails, "Re");
        CollectionAssert.AreEqual(expected, actual.ToArray());
        for (int i = 0; i < expected.Length; i++)
            Assert.AreSame(expected[i], actual[i], "Selection must use the source email, not a copied/renumbered result.");

        long[] Match(string query) => ReplySearchSession.FindMatches(emails, query).Select(e => e.Id).ToArray();
    }

    [TestMethod]
    public async Task Search_RetainsQueryOnNativeBack_ClearsOnArrow_SelectsEmailAndResetsOnReturn()
    {
        var activity = await Start();
        try
        {
            await Click(activity, node => node.Text == "Search emails", "collapsed search");
            AssertPresent("No search history");
            await SetText(activity, "Bonjour");
            AssertPresent("Bonjour from Paris");
            int passes = activity.Passes;
            Runner.RunOnMainSync(() => activity.Tick.Value++);
            await Settle(activity);
            Assert.IsTrue(activity.Passes > passes);
            AssertEditor("Bonjour");
            AssertPresent("Bonjour from Paris");

            if (!OperatingSystem.IsAndroidVersionAtLeast(30))
                throw new PlatformNotSupportedException("Native search IME regression requires Android 11 or later.");
            using (var editor = Find(node => node.Editable)
                ?? throw new InvalidOperationException("Search editor missing before IME action."))
            {
                var imeAction = AccessibilityNodeInfo.AccessibilityAction.ActionImeEnter
                    ?? throw new InvalidOperationException("Native IME action is unavailable.");
                Assert.IsTrue(editor.PerformAction((global::Android.Views.Accessibility.Action)imeAction.Id));
            }
            await Settle(activity);
            using (var result = Find(node => node.Text == "Bonjour from Paris"))
                Assert.IsNull(result, "IME Search should collapse, not leave results open.");
            await Click(activity, node => node.Text == "Bonjour", "IME-retained query");
            AssertPresent("Bonjour from Paris");

            // With the IME visible, the first Back is owned by the keyboard.
            Runner.SendKeyDownUpSync(Keycode.Back);
            await Settle(activity);
            AssertEditor("Bonjour");
            Runner.SendKeyDownUpSync(Keycode.Back);
            await Settle(activity);
            Assert.IsTrue(activity.InInbox.Value, "Search dismissal must not navigate.");
            await Click(activity, node => node.Text == "Bonjour", "retained query");
            AssertEditor("Bonjour");
            await Click(activity, node => node.ContentDescription == "Back", "search Back arrow");
            await Click(activity, node => node.Text == "Search emails", "cleared search");
            AssertPresent("No search history");
            await SetText(activity, "no-such-email");
            AssertPresent("No item found");
            await SetText(activity, "Bonjour");
            await Click(activity, node => node.Text == "Bonjour from Paris", "search result");
            Assert.AreEqual(2L, activity.SelectedId);
            Assert.AreEqual(1, activity.SelectionCalls);
            Assert.IsFalse(activity.InInbox.Value);
            AssertPresent("Selected email 2");

            Runner.RunOnMainSync(() => activity.InInbox.Value = true);
            await Settle(activity);
            await Click(activity, node => node.Text == "Search emails", "returned search");
            AssertPresent("No search history");
            AssertEditor("");
        }
        finally
        {
            Runner.RunOnMainSync(activity.Finish);
            await activity.Destroyed.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Runner.WaitForIdleSync();
        }
    }

    static async Task<ReplySearchTestActivity> Start()
    {
        using var intent = new Intent(global::Android.App.Application.Context, typeof(ReplySearchTestActivity));
        intent.AddFlags(ActivityFlags.NewTask);
        var started = Runner.StartActivitySync(intent) as ReplySearchTestActivity
            ?? throw new InvalidOperationException("Reply search activity did not start.");
        await Settle(started);
        Assert.IsTrue(started.HasWindowFocus, "Reply search activity did not own input focus.");
        return started;
    }

    static async Task Settle(ReplySearchTestActivity activity)
    {
        Runner.WaitForIdleSync();
        var frame = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var secondFrame = new Java.Lang.Runnable(() => frame.TrySetResult());
        using var firstFrame = new Java.Lang.Runnable(() => Require(activity.Window?.DecorView).PostOnAnimation(secondFrame));
        Runner.RunOnMainSync(() => Require(activity.Window?.DecorView).PostOnAnimation(firstFrame));
        await frame.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Runner.WaitForIdleSync();
        Require(Runner.UiAutomation).WaitForIdle(200, 5000);
        Assert.IsTrue(Require(activity.Window?.DecorView).IsLaidOut);
    }

    static async Task Click(ReplySearchTestActivity activity, Func<AccessibilityNodeInfo, bool> predicate, string name)
    {
        using var node = Find(predicate) ?? throw new InvalidOperationException($"Missing {name}.");
        var target = node;
        try
        {
            while (!target.Clickable)
            {
                var parent = target.Parent ?? throw new InvalidOperationException($"{name} has no clickable ancestor.");
                if (!ReferenceEquals(target, node))
                    target.Dispose();
                target = parent;
            }
            Assert.IsTrue(target.PerformAction(global::Android.Views.Accessibility.Action.Click), $"Could not click {name}.");
        }
        finally
        {
            if (!ReferenceEquals(target, node))
                target.Dispose();
        }
        await Settle(activity);
    }

    static async Task SetText(ReplySearchTestActivity activity, string text)
    {
        using var editor = Find(node => node.Editable)
            ?? throw new InvalidOperationException("Expanded search has no native editable field.");
        using var args = new Bundle();
        args.PutCharSequence(AccessibilityNodeInfo.ActionArgumentSetTextCharsequence, text);
        Assert.IsTrue(editor.PerformAction(global::Android.Views.Accessibility.Action.SetText, args));
        await Settle(activity);
        AssertEditor(text);
    }

    static void AssertEditor(string text)
    {
        using var editor = Find(node => node.Editable)
            ?? throw new InvalidOperationException("Search editor missing.");
        Assert.AreEqual(text, editor.Text ?? "");
    }

    static void AssertPresent(string text)
    {
        using var node = Find(node => node.Text == text);
        Assert.IsNotNull(node, $"Search text '{text}' is missing from the owned native hierarchy.");
    }

    static AccessibilityNodeInfo? Find(Func<AccessibilityNodeInfo, bool> predicate)
    {
        using var root = Require(Runner.UiAutomation).RootInActiveWindow
            ?? throw new InvalidOperationException("Search has no active accessibility root.");
        Assert.AreEqual("net.compose.devicetests", root.PackageName, "Do not inspect another app.");
        return FindIn(root, predicate);
    }

    static AccessibilityNodeInfo? FindIn(AccessibilityNodeInfo node, Func<AccessibilityNodeInfo, bool> predicate)
    {
        if (predicate(node))
            return OperatingSystem.IsAndroidVersionAtLeast(33)
                ? new AccessibilityNodeInfo(node)
                : AccessibilityNodeInfo.Obtain(node);
        for (int i = 0; i < node.ChildCount; i++)
        {
            using var child = node.GetChild(i);
            if (child is not null && FindIn(child, predicate) is { } found)
                return found;
        }
        return null;
    }

    static T Require<T>(T? value) where T : class =>
        value ?? throw new InvalidOperationException($"Reply search test {typeof(T).Name} is unavailable.");
}
