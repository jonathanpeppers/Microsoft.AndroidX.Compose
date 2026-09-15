using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Views.Accessibility;
using AndroidX.Compose.Samples.Reply;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Exercises Reply's actual tab callbacks, native list state, detail Back/Up, and recreation.</summary>
[TestClass]
[DoNotParallelize]
public class ReplyNavigationTests
{
    static TestInstrumentation Runner => TestInstrumentation.Current
        ?? throw new InvalidOperationException("Reply navigation tests require native instrumentation.");

    /// <summary>Repeated tab taps leave only the start destination beneath the selected tab.</summary>
    [TestMethod]
    public async Task CurrentTabDoesNotDuplicate_AndSystemBackSelectsInbox()
    {
        var activity = await Start();
        try
        {
            foreach (var destination in TopLevelDestinations.All)
            {
                for (int repeat = 0; repeat < 4; repeat++)
                {
                    await Tap(activity, destination.IconTextId);
                    await AssertRoute(activity, destination.Route);
                    string? previous = null;
                    await activity.OnUi(() => previous = activity.Controller.Jvm?.PreviousBackStackEntry?.Destination.Route);
                    Assert.AreEqual(destination.Route == Route.Inbox ? null : Route.Inbox, previous,
                        $"Repeated {destination.Route} tap added another tab entry.");
                }
            }
            await Back(activity);
            await AssertRoute(activity, Route.Inbox);
            await activity.OnUi(() => Assert.IsNull(activity.Controller.Jvm?.PreviousBackStackEntry));
        }
        finally { await Finish(activity); }
    }

    /// <summary>Tab changes, row detail, system Back and Up preserve the exact visible inbox context.</summary>
    [TestMethod]
    public async Task InboxViewportAndSelectionSurviveTabsAndDetail()
    {
        var activity = await Start();
        try
        {
            await ScrollInbox(activity);
            var viewport = CaptureInbox(activity);
            Assert.IsTrue(viewport[0].Id > 0, "The inbox did not move away from its initial viewport.");
            foreach (var destination in TopLevelDestinations.All.Skip(1))
            {
                await Tap(activity, destination.IconTextId);
                await Tap(activity, "Inbox");
                AssertViewport(viewport, CaptureInbox(activity));
            }

            long selectedId = viewport[0].Id;
            await ClickEmail(activity, selectedId, longClick: true);
            await activity.OnUi(() => Assert.IsTrue(activity.State.SelectedEmailIds.Contains(selectedId)));
            bool[] backActions = [true, false];
            foreach (bool systemBack in backActions)
            {
                await ClickEmail(activity, selectedId);
                await AssertRoute(activity, Route.EmailDetailPattern);
                await activity.OnUi(() => Assert.AreEqual(selectedId, activity.State.OpenedEmailId.Value));
                if (systemBack)
                    await Back(activity);
                else
                    await Tap(activity, "Back");
                await AssertRoute(activity, Route.Inbox);
                await activity.OnUi(() =>
                {
                    Assert.AreEqual(0L, activity.State.OpenedEmailId.Value);
                    Assert.IsTrue(activity.State.SelectedEmailIds.Contains(selectedId),
                        "Pinned Reply does not clear selection on detail Back.");
                });
                AssertViewport(viewport, CaptureInbox(activity));
            }

            // At the root, selection must not consume Back to clear itself.
            var selection = activity.State.SelectedEmailIds;
            await activity.OnUi(activity.ExpectLifecycleEnd);
            Runner.SendKeyDownUpSync(Keycode.Back);
            await activity.Destroyed.Task.WaitAsync(TimeSpan.FromSeconds(15));
            Assert.IsTrue(selection.Contains(selectedId));
        }
        finally { await Finish(activity); }
    }

    /// <summary>Saved Inbox detail and native list position restore through tab switching and a new activity.</summary>
    [TestMethod]
    public async Task SavedDetailAndInboxViewportRestoreAfterRecreation()
    {
        var activity = await Start();
        try
        {
            await ScrollInbox(activity);
            var viewport = CaptureInbox(activity);
            long id = viewport[0].Id;
            await ClickEmail(activity, id, longClick: true);
            await ClickEmail(activity, id);
            await AssertRoute(activity, Route.EmailDetailPattern);
            for (int repeat = 0; repeat < 3; repeat++)
            {
                await Tap(activity, "Inbox");
                await AssertRoute(activity, Route.EmailDetailPattern);
                await activity.OnUi(() =>
                {
                    Assert.AreEqual(id.ToString(), activity.Controller.CurrentBackStackEntry?.Arguments?.GetString("emailId"));
                    Assert.AreEqual(Route.Inbox, activity.Controller.Jvm?.PreviousBackStackEntry?.Destination.Route,
                        "Reselecting Inbox duplicated its saved email detail.");
                });
            }
            await Tap(activity, "Articles");
            await AssertRoute(activity, Route.Articles);
            var oldController = activity.Controller;
            activity = await Recreate(activity);
            Assert.AreNotSame(oldController, activity.Controller);
            await AssertRoute(activity, Route.Articles);
            await Tap(activity, "Inbox");
            await AssertRoute(activity, Route.EmailDetailPattern);
            await activity.OnUi(() =>
            {
                Assert.AreEqual(id.ToString(), activity.Controller.CurrentBackStackEntry?.Arguments?.GetString("emailId"));
                Assert.IsTrue(activity.State.SelectedEmailIds.Contains(id));
            });
            await Tap(activity, "Groups");
            await Back(activity);
            await AssertRoute(activity, Route.EmailDetailPattern);
            await activity.OnUi(() => Assert.AreEqual(id.ToString(),
                activity.Controller.CurrentBackStackEntry?.Arguments?.GetString("emailId")));
            activity = await Recreate(activity);
            await AssertRoute(activity, Route.EmailDetailPattern);
            await Back(activity);
            await AssertRoute(activity, Route.Inbox);
            AssertViewport(viewport, CaptureInbox(activity));
            activity = await Recreate(activity);
            await AssertRoute(activity, Route.Inbox);
            AssertViewport(viewport, CaptureInbox(activity));
        }
        finally { await Finish(activity); }
    }

    static async Task<ReplyNavigationTestActivity> Start()
    {
        ReplyNavigationTestActivity.Prepare();
        using var intent = new Intent(global::Android.App.Application.Context, typeof(ReplyNavigationTestActivity));
        intent.AddFlags(ActivityFlags.NewTask);
        Runner.RunOnMainSync(() => global::Android.App.Application.Context.StartActivity(intent));
        var activity = await ReplyNavigationTestActivity.Started.Task.WaitAsync(TimeSpan.FromSeconds(15));
        try
        {
            await activity.AtNativeIdle();
            return activity;
        }
        catch
        {
            await Finish(activity);
            throw;
        }
    }

    static async Task<ReplyNavigationTestActivity> Recreate(ReplyNavigationTestActivity old)
    {
        ReplyNavigationTestActivity.Prepare();
        await old.OnUi(() =>
        {
            old.ExpectLifecycleEnd();
            old.Recreate();
        });
        var replacement = await ReplyNavigationTestActivity.Started.Task.WaitAsync(TimeSpan.FromSeconds(15));
        await old.Destroyed.Task.WaitAsync(TimeSpan.FromSeconds(15));
        Assert.AreNotSame(old, replacement);
        try
        {
            await replacement.AtNativeIdle();
            return replacement;
        }
        catch
        {
            await Finish(replacement);
            throw;
        }
    }

    static async Task Finish(ReplyNavigationTestActivity activity)
    {
        if (activity.Destroyed.Task.IsCompleted)
            return;
        await activity.OnUi(() =>
        {
            activity.ExpectLifecycleEnd();
            activity.Finish();
        });
        await activity.Destroyed.Task.WaitAsync(TimeSpan.FromSeconds(15));
    }

    static async Task Back(ReplyNavigationTestActivity activity)
    {
        await activity.AtNativeIdle();
        Runner.SendKeyDownUpSync(Keycode.Back);
        await activity.AtNativeIdle();
    }

    static async Task Tap(ReplyNavigationTestActivity activity, string description)
    {
        await activity.AtNativeIdle();
        using var root = Root(activity);
        using var label = Find(root, n => n.VisibleToUser && n.ContentDescription == description)
            ?? throw new InvalidOperationException($"Reply '{description}' node was not present.");
        using var bounds = new Rect();
        using var window = new Rect();
        label.GetBoundsInScreen(bounds);
        root.GetBoundsInScreen(window);
        Assert.IsTrue(bounds.Width() > 0 && bounds.Height() > 0 &&
            window.Contains(bounds.CenterX(), bounds.CenterY()), "Reply tap target is outside its owned window.");
        Console.WriteLine($"Reply tap '{description}', window={root.WindowId}, bounds={bounds}");
        Runner.RunOnMainSync(() => Assert.IsTrue(activity.View.HasWindowFocus));

        // Selected tabs omit redundant accessibility click actions; exercise a real re-tap.
        long downTime = SystemClock.UptimeMillis();
        using var down = MotionEvent.Obtain(downTime, downTime, MotionEventActions.Down,
            bounds.CenterX(), bounds.CenterY(), 0)
            ?? throw new InvalidOperationException("Could not create the Reply pointer down.");
        down.SetSource(InputSourceType.Touchscreen);
        Runner.SendPointerSync(down);
        try
        {
            Runner.WaitForIdleSync();
        }
        finally
        {
            using var up = MotionEvent.Obtain(downTime, SystemClock.UptimeMillis(), MotionEventActions.Up,
                bounds.CenterX(), bounds.CenterY(), 0)
                ?? throw new InvalidOperationException("Could not create the Reply pointer up.");
            up.SetSource(InputSourceType.Touchscreen);
            Runner.SendPointerSync(up);
        }
        await activity.AtNativeIdle();
    }

    static async Task ClickEmail(ReplyNavigationTestActivity activity, long id, bool longClick = false)
    {
        var email = LocalEmailsDataProvider.Get(id)
            ?? throw new InvalidOperationException($"Reply email {id} was not found.");
        using var root = Root(activity);
        using var label = Find(root, n => n.Text?.Contains(email.Subject, StringComparison.Ordinal) == true)
            ?? throw new InvalidOperationException($"Reply email {id} was not visible.");
        PerformClick(label, longClick);
        await activity.AtNativeIdle();
    }

    static void PerformClick(AccessibilityNodeInfo node, bool longClick)
    {
        if (longClick ? node.LongClickable : node.Clickable)
        {
            Assert.IsTrue(node.PerformAction(longClick
                ? global::Android.Views.Accessibility.Action.LongClick
                : global::Android.Views.Accessibility.Action.Click));
            return;
        }
        using var parent = node.Parent
            ?? throw new InvalidOperationException("Reply action has no clickable ancestor.");
        PerformClick(parent, longClick);
    }

    static async Task ScrollInbox(ReplyNavigationTestActivity activity)
    {
        for (int page = 0; page < 2; page++)
        {
            using var root = Root(activity);
            using var list = Find(root, n => n.Scrollable)
                ?? throw new InvalidOperationException("Reply inbox has no native scrollable node.");
            Assert.IsTrue(list.PerformAction(global::Android.Views.Accessibility.Action.ScrollForward));
            await activity.AtNativeIdle();
        }
    }

    static async Task AssertRoute(ReplyNavigationTestActivity activity, string route)
    {
        await activity.OnUi(() => Assert.AreEqual(route, activity.Controller.CurrentBackStackEntry?.Route));
        using var root = Root(activity);
        string selected = route == Route.EmailDetailPattern ? "Inbox"
            : TopLevelDestinations.All.Single(d => d.Route == route).IconTextId;
        using var label = Find(root, n => n.ContentDescription == selected)
            ?? throw new InvalidOperationException($"Selected tab '{selected}' is missing.");
        AssertSelected(label);
    }

    static void AssertSelected(AccessibilityNodeInfo node)
    {
        if (node.Selected)
            return;
        using var parent = node.Parent
            ?? throw new InvalidOperationException("Reply tab has no selected native ancestor.");
        AssertSelected(parent);
    }

    static (long Id, int Top)[] CaptureInbox(ReplyNavigationTestActivity activity)
    {
        using var root = Root(activity);
        List<(long Id, int Top)> visible = [];
        foreach (var email in LocalEmailsDataProvider.AllEmails)
        {
            using var node = Find(root, n => n.VisibleToUser &&
                n.Text?.Contains(email.Subject, StringComparison.Ordinal) == true);
            if (node is null)
                continue;
            using var bounds = new Rect();
            node.GetBoundsInScreen(bounds);
            visible.Add((email.Id, bounds.Top));
        }
        Assert.IsTrue(visible.Count > 0, "The native inbox viewport was empty.");
        var result = visible.OrderBy(item => item.Top).ToArray();
        Console.WriteLine("Reply viewport: " + string.Join(", ", result.Select(item => $"{item.Id}@{item.Top}")));
        return result;
    }

    static void AssertViewport((long Id, int Top)[] expected, (long Id, int Top)[] actual) =>
        CollectionAssert.AreEqual(expected, actual, "Inbox email identities or pixel offsets were not restored.");

    static AccessibilityNodeInfo Root(ReplyNavigationTestActivity activity)
    {
        var automation = Runner.UiAutomation
            ?? throw new InvalidOperationException("Native UI automation is unavailable.");
        automation.WaitForIdle(100, 5000);
        var root = automation.RootInActiveWindow
            ?? throw new InvalidOperationException("Reply has no active native accessibility root.");
        int windowId = -1;
        Runner.RunOnMainSync(() =>
        {
            using var info = activity.View.CreateAccessibilityNodeInfo()
                ?? throw new InvalidOperationException("Reply view has no accessibility window.");
            windowId = info.WindowId;
        });
        if (root.PackageName != "net.compose.devicetests" || root.WindowId != windowId)
        {
            root.Dispose();
            throw new InvalidOperationException("The active window does not belong to the Reply test host.");
        }
        return root;
    }

    static AccessibilityNodeInfo? Find(AccessibilityNodeInfo root, Func<AccessibilityNodeInfo, bool> predicate)
    {
        if (predicate(root))
            return OperatingSystem.IsAndroidVersionAtLeast(33)
                ? new AccessibilityNodeInfo(root) : AccessibilityNodeInfo.Obtain(root);
        for (int i = 0; i < root.ChildCount; i++)
        {
            using var child = root.GetChild(i);
            if (child is not null && Find(child, predicate) is { } found)
                return found;
        }
        return null;
    }
}
