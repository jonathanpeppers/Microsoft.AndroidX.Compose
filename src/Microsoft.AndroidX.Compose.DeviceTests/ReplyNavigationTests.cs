using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Views.Accessibility;
using AndroidX.Compose.Samples.Reply;
using NavigationSuiteType = AndroidX.Compose.NavigationSuiteType;

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
                    await Tap(activity, Label(destination));
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
                await Tap(activity, Label(destination));
                await Tap(activity, "Inbox");
                AssertViewport(viewport, CaptureInbox(activity));
            }

            long selectedId = viewport[0].Id;
            await ClickAvatar(activity, selectedId);
            await activity.OnUi(() => Assert.IsTrue(activity.State.SelectedEmailIds.Contains(selectedId)));
            AssertEmailSelected(activity, selectedId, expected: true);
            await ClickEmail(activity, selectedId, longClick: true);
            await activity.OnUi(() => Assert.IsFalse(activity.State.SelectedEmailIds.Contains(selectedId)));
            AssertEmailSelected(activity, selectedId, expected: false);
            await ClickEmail(activity, selectedId, longClick: true);
            await activity.OnUi(() => Assert.IsTrue(activity.State.SelectedEmailIds.Contains(selectedId)));
            AssertEmailSelected(activity, selectedId, expected: true);
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

    /// <summary>Actual window width selects the compact, medium, or expanded navigation presentation.</summary>
    [TestMethod]
    public async Task AdaptiveNavigationMatchesWindowWidth()
    {
        var observed = new TaskCompletionSource<NavigationSuiteType>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        ReplyApp.NavigationTypeObserver = type => observed.TrySetResult(type);
        var activity = await Start();
        try
        {
            var actual = await observed.Task.WaitAsync(TimeSpan.FromSeconds(15));
            float density = 0;
            int width = 0;
            int height = 0;
            await activity.OnUi(() =>
            {
                density = activity.Resources?.DisplayMetrics?.Density
                    ?? throw new InvalidOperationException("Reply display density is unavailable.");
                width = activity.View.Width;
                height = activity.View.Height;
            });
            float widthDp = width / density;
            float heightDp = height / density;
            var expected = ReplyApp.ResolveNavigationType(
                widthAtLeastMedium: widthDp >= 600,
                heightAtLeastMedium: heightDp >= 480,
                widthAtLeastLarge: widthDp >= 1200);
            Assert.AreEqual(expected, actual,
                $"Reply window was {widthDp:F1} x {heightDp:F1}dp ({width}x{height}px at {density:F2}x).");
            using var root = Root(activity);
            string inboxLabel = Label(TopLevelDestinations.All[0]);
            using var inbox = FindNavigationItem(root, inboxLabel)
                ?? throw new InvalidOperationException("Reply Inbox navigation item is missing.");
            using var itemBounds = new Rect();
            using var windowBounds = new Rect();
            inbox.GetBoundsInScreen(itemBounds);
            root.GetBoundsInScreen(windowBounds);
            if (actual == NavigationSuiteType.NavigationBar)
                Assert.IsTrue(itemBounds.CenterY() > windowBounds.CenterY(),
                    $"Compact navigation did not render at the bottom: item={itemBounds}, window={windowBounds}.");
            else
                Assert.IsTrue(itemBounds.CenterX() < windowBounds.CenterX(),
                    $"Rail/drawer navigation did not render at the start: item={itemBounds}, window={windowBounds}.");
            Console.WriteLine(
                $"Reply adaptive navigation: size={widthDp:F1}x{heightDp:F1}dp, type={actual}");
        }
        finally
        {
            ReplyApp.NavigationTypeObserver = null;
            await Finish(activity);
        }
    }

    /// <summary>Compact inbox FAB follows scroll direction and remains available on detail.</summary>
    [TestMethod]
    public async Task CompactFabCollapsesOnForwardScroll_ExpandsOnBackwardScroll_AndRemainsOnDetail()
    {
        var observed = new TaskCompletionSource<NavigationSuiteType>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        ReplyApp.NavigationTypeObserver = type => observed.TrySetResult(type);
        var activity = await Start();
        try
        {
            if (await observed.Task.WaitAsync(TimeSpan.FromSeconds(15)) != NavigationSuiteType.NavigationBar)
            {
                Assert.Inconclusive("Scroll-responsive FAB is intentionally limited to compact bottom navigation.");
                return;
            }

            int expandedWidth = FabBounds(activity).Width();
            using (var list = ScrollableRoot(activity))
                Assert.IsTrue(list.PerformAction(global::Android.Views.Accessibility.Action.ScrollForward));
            await activity.AtNativeIdle();
            int collapsedWidth = FabBounds(activity).Width();
            Assert.IsTrue(collapsedWidth < expandedWidth,
                $"Forward scroll did not collapse the FAB: expanded={expandedWidth}, collapsed={collapsedWidth}.");

            long visibleEmailId = CaptureInbox(activity)[0].Id;
            await ClickEmail(activity, visibleEmailId);
            await AssertRoute(activity, Route.EmailDetailPattern);
            Assert.AreEqual(collapsedWidth, FabBounds(activity).Width(),
                "Compact detail did not preserve the collapsed inbox FAB state.");
            await Back(activity);
            await AssertRoute(activity, Route.Inbox);

            using (var list = ScrollableRoot(activity))
                Assert.IsTrue(list.PerformAction(global::Android.Views.Accessibility.Action.ScrollBackward));
            await activity.AtNativeIdle();
            int restoredWidth = FabBounds(activity).Width();
            Assert.IsTrue(restoredWidth > collapsedWidth,
                $"Backward scroll did not expand the FAB: collapsed={collapsedWidth}, restored={restoredWidth}.");

            await ClickEmail(activity, visibleEmailId);
            await AssertRoute(activity, Route.EmailDetailPattern);
            Assert.AreEqual(restoredWidth, FabBounds(activity).Width(),
                "Compact detail did not preserve the expanded inbox FAB state.");
        }
        finally
        {
            ReplyApp.NavigationTypeObserver = null;
            await Finish(activity);
        }
    }

    /// <summary>The centered detail title is the first lazy-list item and scrolls with its thread.</summary>
    [TestMethod]
    public async Task DetailToolbarIsCenteredAndScrollsWithThread()
    {
        var activity = await Start();
        try
        {
            var email = LocalEmailsDataProvider.AllEmails[0];
            await ClickEmail(activity, email.Id);
            await AssertRoute(activity, Route.EmailDetailPattern);
            using var root = Root(activity);
            using var title = Find(root, n => n.VisibleToUser &&
                n.ContentDescription == String(Resource.String.reply_email_detail_title))
                ?? throw new InvalidOperationException("Reply detail toolbar title is missing.");
            using var list = ScrollableRoot(activity);
            using var titleBounds = new Rect();
            using var contentBounds = new Rect();
            title.GetBoundsInScreen(titleBounds);
            list.GetBoundsInScreen(contentBounds);
            Assert.IsTrue(Math.Abs(contentBounds.CenterX() - titleBounds.CenterX()) <= 4,
                $"Reply full-screen detail title is not centered: content={contentBounds}, title={titleBounds}.");

            Assert.IsTrue(list.PerformAction(global::Android.Views.Accessibility.Action.ScrollForward));
            await activity.AtNativeIdle();
            using var after = Root(activity);
            using var scrolledTitle = Find(after, n => n.VisibleToUser &&
                n.ContentDescription == String(Resource.String.reply_email_detail_title));
            Assert.IsNull(scrolledTitle, "Reply detail toolbar remained pinned instead of scrolling with the thread.");
        }
        finally { await Finish(activity); }
    }

    /// <summary>Adaptive navigation matching rejects identical non-interactive content labels.</summary>
    [TestMethod]
    public void NavigationItemMatchRejectsContentHeading()
    {
        Assert.IsTrue(IsNavigationItemMatch(visible: true, clickable: true, selected: false, exactLabel: true));
        Assert.IsTrue(IsNavigationItemMatch(visible: true, clickable: false, selected: true, exactLabel: true));
        Assert.IsFalse(IsNavigationItemMatch(visible: true, clickable: false, selected: false, exactLabel: true));
        Assert.IsFalse(IsNavigationItemMatch(visible: true, clickable: true, selected: false, exactLabel: false));
    }

    /// <summary>Pinned Reply navigation policy includes compact height and a 1200 dp drawer threshold.</summary>
    [TestMethod]
    public void NavigationPolicyMatchesPinnedBreakpoints()
    {
        Assert.AreEqual(NavigationSuiteType.NavigationBar,
            ResolveAt(widthDp: 599, heightDp: 900));
        Assert.AreEqual(NavigationSuiteType.NavigationRail,
            ResolveAt(widthDp: 600, heightDp: 480));
        Assert.AreEqual(NavigationSuiteType.NavigationBar,
            ResolveAt(widthDp: 1200, heightDp: 479));
        Assert.AreEqual(NavigationSuiteType.NavigationRail,
            ResolveAt(widthDp: 1199, heightDp: 900));
        Assert.AreEqual(NavigationSuiteType.NavigationDrawer,
            ResolveAt(widthDp: 1200, heightDp: 900));

        static NavigationSuiteType ResolveAt(int widthDp, int heightDp) =>
            ReplyApp.ResolveNavigationType(
                widthAtLeastMedium: widthDp >= 600,
                heightAtLeastMedium: heightDp >= 480,
                widthAtLeastLarge: widthDp >= 1200);
    }

    /// <summary>Selected-row semantics association rejects viewport-wide and sibling-row nodes.</summary>
    [TestMethod]
    public void SelectedBoundsAssociationRejectsViewportAndOtherRows()
    {
        using var viewport = new Rect(0, 383, 1080, 2140);
        using var subject = new Rect(95, 507, 271, 570);
        using var row = new Rect(42, 363, 1038, 697);
        using var otherRow = new Rect(42, 719, 1038, 1173);
        using var offscreenRow = new Rect(42, -400, 1038, -20);
        using var oversizedAncestor = new Rect(0, 383, 1080, 2140);

        Assert.IsTrue(IsEmailSelectionBounds(row, subject, viewport));
        Assert.IsFalse(IsEmailSelectionBounds(otherRow, subject, viewport));
        Assert.IsFalse(IsEmailSelectionBounds(offscreenRow, subject, viewport));
        Assert.IsFalse(IsEmailSelectionBounds(oversizedAncestor, subject, viewport));
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

    static async Task ClickAvatar(ReplyNavigationTestActivity activity, long id)
    {
        var email = LocalEmailsDataProvider.Get(id)
            ?? throw new InvalidOperationException($"Reply email {id} was not found.");
        using var root = Root(activity);
        using var avatar = Find(root, n => n.VisibleToUser &&
            n.ContentDescription == email.Sender.FullName)
            ?? throw new InvalidOperationException($"Reply email {id} avatar was not visible.");
        PerformClick(avatar, longClick: false);
        await activity.AtNativeIdle();
    }

    static void AssertEmailSelected(ReplyNavigationTestActivity activity, long id, bool expected)
    {
        var email = LocalEmailsDataProvider.Get(id)
            ?? throw new InvalidOperationException($"Reply email {id} was not found.");
        using var root = Root(activity);
        using var label = Find(root, n => n.VisibleToUser &&
            n.Text?.Contains(email.Subject, StringComparison.Ordinal) == true)
            ?? throw new InvalidOperationException($"Reply email {id} was not visible.");
        using var labelBounds = new Rect();
        label.GetBoundsInScreen(labelBounds);
        using var list = ScrollableRoot(activity);
        using var viewportBounds = new Rect();
        list.GetBoundsInScreen(viewportBounds);
        Console.WriteLine(
            $"Reply email {id} semantics target: subject={labelBounds}, viewport={viewportBounds}");
        LogSelectionNodes(root);
        int candidateCount = Count(root, node =>
            IsCheckedEmailNode(node, labelBounds, viewportBounds));
        if (expected)
        {
            Assert.AreEqual(1, candidateCount,
                $"Reply email {id} must have exactly one bounded checked accessibility node.");
            return;
        }
        Assert.AreEqual(0, candidateCount,
            $"Reply email {id} unexpectedly publishes checked accessibility state.");
    }

    static void LogSelectionNodes(AccessibilityNodeInfo root)
    {
        if (root.VisibleToUser && (root.Selected || root.Checkable))
        {
            using var bounds = new Rect();
            root.GetBoundsInScreen(bounds);
            Console.WriteLine(
                $"Reply selection node: bounds={bounds}, selected={root.Selected}, " +
                $"checkable={root.Checkable}, checked={IsChecked(root)}, " +
                $"class={root.ClassName}, text={root.Text}, description={root.ContentDescription}");
        }
        for (int i = 0; i < root.ChildCount; i++)
        {
            using var child = root.GetChild(i);
            if (child is not null)
                LogSelectionNodes(child);
        }
    }

    static bool IsCheckedEmailNode(
        AccessibilityNodeInfo node,
        Rect subjectBounds,
        Rect viewportBounds)
    {
        // Compose maps SemanticsProperties.Selected to Android's checked state
        // for non-tab roles; only Role.Tab maps to AccessibilityNodeInfo.Selected.
        if (!node.VisibleToUser || !node.Checkable || !IsChecked(node))
            return false;
        using var bounds = new Rect();
        node.GetBoundsInScreen(bounds);
        return IsEmailSelectionBounds(bounds, subjectBounds, viewportBounds);
    }

    static bool IsChecked(AccessibilityNodeInfo node) =>
        OperatingSystem.IsAndroidVersionAtLeast(36)
            ? node.CheckedState == CheckedState.True
            : node.Checked;

    static bool IsEmailSelectionBounds(Rect candidate, Rect subject, Rect viewport) =>
        candidate.Left < viewport.Right &&
        candidate.Right > viewport.Left &&
        candidate.Top < viewport.Bottom &&
        candidate.Bottom > viewport.Top &&
        candidate.Width() <= viewport.Width() &&
        candidate.Height() < viewport.Height() / 2 &&
        viewport.Contains(subject.CenterX(), subject.CenterY()) &&
        candidate.Contains(subject.CenterX(), subject.CenterY());

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

    static AccessibilityNodeInfo ScrollableRoot(ReplyNavigationTestActivity activity)
    {
        using var root = Root(activity);
        return Find(root, n => n.VisibleToUser && n.Scrollable)
            ?? throw new InvalidOperationException("Reply has no native scrollable node.");
    }

    static AccessibilityNodeInfo? FindNavigationItem(AccessibilityNodeInfo root, string label)
    {
        if (root.VisibleToUser &&
            (root.ContentDescription == label || root.Text == label) &&
            FindNavigationAncestor(root) is { } candidate)
            return candidate;
        for (int i = 0; i < root.ChildCount; i++)
        {
            using var child = root.GetChild(i);
            if (child is not null && FindNavigationItem(child, label) is { } found)
                return found;
        }
        return null;
    }

    static AccessibilityNodeInfo? FindNavigationAncestor(AccessibilityNodeInfo labelNode)
    {
        var current = Copy(labelNode);
        while (!IsNavigationItemMatch(
            current.VisibleToUser,
            current.Clickable,
            current.Selected,
            exactLabel: true))
        {
            var parent = current.Parent;
            current.Dispose();
            if (parent is null)
                return null;
            current = parent;
        }
        return current;
    }

    static bool IsNavigationItemMatch(
        bool visible,
        bool clickable,
        bool selected,
        bool exactLabel) =>
        visible && exactLabel && (clickable || selected);

    static Rect FabBounds(ReplyNavigationTestActivity activity)
    {
        using var root = Root(activity);
        using var edit = Find(root, n => n.VisibleToUser &&
            n.ContentDescription == String(Resource.String.reply_edit))
            ?? throw new InvalidOperationException("Reply Compose FAB is missing.");
        var current = Copy(edit);
        try
        {
            while (!current.Clickable)
            {
                var parent = current.Parent
                    ?? throw new InvalidOperationException("Reply Compose FAB has no clickable ancestor.");
                current.Dispose();
                current = parent;
            }
            var bounds = new Rect();
            current.GetBoundsInScreen(bounds);
            return bounds;
        }
        finally { current.Dispose(); }
    }

    static async Task AssertRoute(ReplyNavigationTestActivity activity, string route)
    {
        await activity.OnUi(() => Assert.AreEqual(route, activity.Controller.CurrentBackStackEntry?.Route));
        using var root = Root(activity);
        string selected = route == Route.EmailDetailPattern ? "Inbox"
            : Label(TopLevelDestinations.All.Single(d => d.Route == route));
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

    static string Label(ReplyTopLevelDestination destination) =>
        String(destination.LabelResourceId);

    static string String(int resourceId) =>
        global::Android.App.Application.Context.GetString(resourceId)
        ?? throw new InvalidOperationException(
            $"Reply string resource {resourceId} was unavailable.");

    static AccessibilityNodeInfo Copy(AccessibilityNodeInfo node) =>
        OperatingSystem.IsAndroidVersionAtLeast(33)
            ? new AccessibilityNodeInfo(node)
            : AccessibilityNodeInfo.Obtain(node)
                ?? throw new InvalidOperationException("Could not copy a Reply accessibility node.");

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

    static int Count(AccessibilityNodeInfo root, Func<AccessibilityNodeInfo, bool> predicate)
    {
        int count = predicate(root) ? 1 : 0;
        for (int i = 0; i < root.ChildCount; i++)
        {
            using var child = root.GetChild(i);
            if (child is not null)
                count += Count(child, predicate);
        }
        return count;
    }
}
