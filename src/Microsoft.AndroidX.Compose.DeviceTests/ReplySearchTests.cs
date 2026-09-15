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

    /// <summary>Checks pinned IDs, prefix semantics, ordering, and source object identity.</summary>
    [TestMethod]
    public void Matching_UsesSubjectOrFullNamePrefixAndOriginalOrder()
    {
        long[] bonjour = [2];
        long[] ali = [1];
        long[] allison = [2, 10, 11];
        long[] google = [0, 7];
        CollectionAssert.AreEqual(bonjour, Match("bOnJoUr"));
        CollectionAssert.AreEqual(ali, Match("Ali "));
        CollectionAssert.AreEqual(allison, Match("All"));
        CollectionAssert.AreEqual(google, Match("Google"));
        Assert.AreEqual(0, Match("").Length);
        Assert.AreEqual(0, Match("Paris").Length, "A substring is not a prefix.");
        Assert.AreEqual(0, Match(" Bonjour").Length, "Do not trim the query.");
        Assert.AreEqual(0, Match("Cucumber").Length, "Do not search bodies.");
        Assert.AreEqual(0, Match("no-such-email").Length);
        var actual = ReplySearchSession.FindMatches(LocalEmailsDataProvider.AllEmails, "All");
        for (int i = 0; i < allison.Length; i++)
            Assert.AreSame(LocalEmailsDataProvider.Get(allison[i]), actual[i],
                "Selection must use the source email, not a copied/renumbered result.");

        static long[] Match(string query) =>
            ReplySearchSession.FindMatches(LocalEmailsDataProvider.AllEmails, query).Select(e => e.Id).ToArray();
    }

    /// <summary>Exercises input, IME, dismissal, selection, and composition departure/re-entry.</summary>
    [TestMethod]
    public async Task Search_RetainsQueryOnNativeBack_ClearsOnArrow_SelectsEmailAndResetsOnReturn()
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(30))
        {
            Assert.Inconclusive("Native search IME regression requires Android 11 or later.");
            return;
        }

        ReportStage("Starting real search activity");
        var activity = await Start();
        try
        {
            var collapsedBounds = EditorHorizontalBounds();
            await Click(activity, node => node.Text == "Search emails", "collapsed search");
            AssertPresent("No search history");
            AssertPopupHorizontalBounds(collapsedBounds);
            await SetText(activity, "Bonjour");
            AssertPresent("Bonjour from Paris");
            int passes = activity.Passes;
            Runner.RunOnMainSync(() => activity.Tick.Value++);
            await Settle(activity);
            Assert.IsTrue(activity.Passes > passes);
            AssertEditor("Bonjour");
            AssertPresent("Bonjour from Paris");

            using (var editor = Find(node => node.Editable)
                ?? throw new InvalidOperationException("Search editor missing before IME action."))
            {
                var imeAction = AccessibilityNodeInfo.AccessibilityAction.ActionImeEnter
                    ?? throw new InvalidOperationException("Native IME action is unavailable.");
                Assert.IsTrue(editor.PerformAction((global::Android.Views.Accessibility.Action)imeAction.Id));
            }
            await Settle(activity);
            AssertSearchContentAbsent("IME Search");
            await Click(activity, node => node.Text == "Bonjour", "IME-retained query");
            AssertPresent("Bonjour from Paris");

            ReportStage("System Back collapses expanded search", activity);
            Runner.SendKeyDownUpSync(Keycode.Back);
            await Settle(activity);
            ReportStage("After search Back", activity);
            AssertEditor("Bonjour");
            Assert.IsTrue(activity.InInbox.Value, "Search dismissal must not navigate.");
            AssertSearchContentAbsent("System Back");
            await Click(activity, node => node.Text == "Bonjour", "retained query");
            AssertEditor("Bonjour");
            await Click(activity, node => node.ContentDescription == "Back", "search Back arrow");
            AssertSearchContentAbsent("Leading Back arrow");
            await Click(activity, node => node.Text == "Search emails", "cleared search");
            AssertPresent("No search history");
            await SetText(activity, "no-such-email");
            AssertPresent("No item found");
            await SetText(activity, "Bonjour");
            AssertPresent("Bonjour from Paris");
            AssertPopupHorizontalBounds(collapsedBounds);
            await SetText(activity, "no-such-email");
            AssertPresent("No item found");
            Runner.RunOnMainSync(() => activity.InInbox.Value = false);
            await Settle(activity);
            Runner.RunOnMainSync(() => activity.InInbox.Value = true);
            await Settle(activity);
            AssertSearchContentAbsent("Navigation return");
            await Click(activity, node => node.Text == "Search emails", "search after navigation away/back");
            AssertPresent("No search history");
            await SetText(activity, "Bonjour");
            await Click(activity, node => node.Text == "Bonjour from Paris", "search result");
            Assert.AreEqual(2L, activity.SelectedId);
            Assert.AreEqual(1, activity.SelectionCalls);
            Assert.IsFalse(activity.InInbox.Value);
            AssertPresent("Selected email 2");

            Runner.RunOnMainSync(() => activity.InInbox.Value = true);
            await Settle(activity);
            AssertSearchContentAbsent("Return from selected email");
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

    /// <summary>Verifies that only expanded search consumes Back, using native window dispatch.</summary>
    [TestMethod]
    public async Task NativeBack_DismissesPopupThenReachesUnderlyingActivity()
    {
        ReportStage("Starting native Back ownership activity");
        var activity = await Start();
        try
        {
            var collapsedBounds = EditorHorizontalBounds();
            await Click(activity, node => node.Text == "Search emails", "Back ownership search");
            await SetText(activity, "Bonjour");
            AssertPresent("Bonjour from Paris");
            AssertPopupHorizontalBounds(collapsedBounds);
            ReportStage("Before first native Back: expanded popup", activity);
            Runner.SendKeyDownUpSync(Keycode.Back);
            await Settle(activity);
            ReportStage("After first native Back: collapsed search", activity);
            AssertEditor("Bonjour");
            Assert.IsTrue(activity.InInbox.Value);
            Assert.AreEqual(0, activity.SelectionCalls);
            AssertSearchContentAbsent("First native Back");

            ReportStage("Before second native Back: underlying activity", activity);
            Runner.SendKeyDownUpSync(Keycode.Back);
            await activity.Destroyed.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Runner.WaitForIdleSync();
            ReportStage("After second native Back: activity destroyed", activity);
            Assert.IsTrue(activity.IsDestroyed, "Collapsed search must not consume the next Back.");
        }
        finally
        {
            if (!activity.IsDestroyed)
            {
                Runner.RunOnMainSync(activity.Finish);
                await activity.Destroyed.Task.WaitAsync(TimeSpan.FromSeconds(10));
            }
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
        Assert.IsFalse(activity.IsFinishing || activity.IsDestroyed,
            "The search activity left its lifecycle during an operation that should not navigate.");
        Assert.IsTrue(Require(activity.Window?.DecorView).IsLaidOut,
            "The owned search activity no longer has a laid-out decor.");
    }

    static async Task Click(ReplySearchTestActivity activity, Func<AccessibilityNodeInfo, bool> predicate, string name)
    {
        ReportStage("Click " + name);
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
        ReportStage("Set query " + text);
        using var editor = Find(node => node.Editable)
            ?? throw new InvalidOperationException("Expanded search has no native editable field.");
        ReportStage($"Query request: {text}; previous={editor.Text}; window={editor.WindowId}");
        using var args = new Bundle();
        args.PutCharSequence(AccessibilityNodeInfo.ActionArgumentSetTextCharsequence, text);
        bool accepted = false;
        using var command = new Java.Lang.Runnable(() =>
            accepted = editor.PerformAction(global::Android.Views.Accessibility.Action.SetText, args));
        using var filter = new ReplySearchTextChangedFilter(text, editor.WindowId);
        try
        {
            using var changed = Require(Runner.UiAutomation).ExecuteAndWaitForEvent(command, filter, 5000)
                ?? throw new InvalidOperationException("Native query action returned no text-change event.");
            Assert.IsTrue(accepted, "Native query action was rejected.");
            ReportStage($"Query acknowledged: {text}; accepted={accepted}; eventTime={changed.EventTime}; window={changed.WindowId}");
        }
        catch (Java.Util.Concurrent.TimeoutException)
        {
            ReportStage($"Query acknowledgement timed out; accepted={accepted}; expectedWindow={editor.WindowId}; {filter.LastObserved}");
            throw;
        }
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

    static void AssertSearchContentAbsent(string action)
    {
        using var content = Find(node => node.Text is
            "Bonjour from Paris" or "No search history" or "No item found");
        Assert.IsNull(content, $"{action} left expanded search content in the owned native hierarchy.");
    }

    static (int Left, int Right) EditorHorizontalBounds()
    {
        using var editor = Find(node => node.Editable)
            ?? throw new InvalidOperationException("Collapsed search editor is missing.");
        using var bounds = new global::Android.Graphics.Rect();
        editor.GetBoundsInScreen(bounds);
        return (bounds.Left, bounds.Right);
    }

    static void AssertPopupHorizontalBounds((int Left, int Right) collapsed)
    {
        using var root = OwnedRoot();
        using var bounds = new global::Android.Graphics.Rect();
        root.GetBoundsInScreen(bounds);
        Assert.AreEqual(collapsed.Left, bounds.Left, "Popup left edge differs from the collapsed input.");
        Assert.AreEqual(collapsed.Right, bounds.Right, "Popup overflows the collapsed input's available width.");
    }

    static AccessibilityNodeInfo? Find(Func<AccessibilityNodeInfo, bool> predicate)
    {
        using var root = OwnedRoot();
        return FindIn(root, predicate);
    }

    static AccessibilityNodeInfo OwnedRoot()
    {
        var automation = Require(Runner.UiAutomation);
        if (OperatingSystem.IsAndroidVersionAtLeast(34))
            Assert.IsTrue(automation.ClearCache(), "Native accessibility client cache was not cleared.");
        var root = automation.RootInActiveWindow
            ?? throw new InvalidOperationException("Search has no active accessibility root.");
        if (root.PackageName != "net.compose.devicetests")
        {
            root.Dispose();
            throw new InvalidOperationException("Do not inspect another app's accessibility root.");
        }
        return root;
    }

    static AccessibilityNodeInfo? FindIn(AccessibilityNodeInfo node, Func<AccessibilityNodeInfo, bool> predicate)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(34))
            Assert.IsTrue(node.Refresh(), "The native accessibility node is no longer available.");
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

    static void ReportStage(string stage, ReplySearchTestActivity? activity = null)
    {
        using var status = new Bundle();
        status.PutString("searchStage", stage);
        status.PutInt("pid", global::Android.OS.Process.MyPid());
        if (activity is not null)
        {
            bool active = false;
            Runner.RunOnMainSync(() =>
            {
                status.PutBoolean("finishing", activity.IsFinishing);
                status.PutBoolean("destroyed", activity.IsDestroyed);
                status.PutBoolean("activityWindowFocused", activity.HasWindowFocus);
                status.PutBoolean("decorLaidOut", Require(activity.Window?.DecorView).IsLaidOut);
                active = !activity.IsFinishing && !activity.IsDestroyed;
            });
            if (active)
            {
                using var root = OwnedRoot();
                status.PutInt("activeWindowId", root.WindowId);
                status.PutString("activeWindowClass", root.ClassName);
            }
        }
        Runner.SendStatus(0, status);
    }
}
