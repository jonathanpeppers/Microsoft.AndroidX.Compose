using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Views.Accessibility;
using AndroidX.Compose.Samples.Reply;
using AndroidX.Window.Layout;
using NavigationSuiteType = AndroidX.Compose.NavigationSuiteType;
using AndroidProcess = global::Android.OS.Process;

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

            long selectedId = FirstVisibleAvatarEmailId(
                activity,
                viewport);
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
        var activity = await Start();
        try
        {
            var actual = await activity.NavigationTypeObserved.Task.WaitAsync(TimeSpan.FromSeconds(15));
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
            await Finish(activity);
        }
    }

    /// <summary>
    /// The live pane directive shows list and detail together only when the
    /// window has at least two horizontal partitions.
    /// </summary>
    [TestMethod]
    public async Task ListDetailPresentationMatchesAdaptivePaneDirective()
    {
        var activity = await Start();
        try
        {
            int partitions = await activity.PanePartitionsObserved.Task
                .WaitAsync(TimeSpan.FromSeconds(15));
            var windowLayoutInfo = await activity.WindowLayoutInfoObserved.Task
                .WaitAsync(TimeSpan.FromSeconds(15));
            var foldingFeatures = windowLayoutInfo.DisplayFeatures
                .OfType<IFoldingFeature>()
                .ToArray();
            Assert.IsTrue(
                foldingFeatures.Length <= windowLayoutInfo.DisplayFeatures.Count,
                "Folding features were not represented in the raw WindowLayoutInfo display-feature list.");
            foreach (var foldingFeature in foldingFeatures)
            {
                Assert.IsNotNull(foldingFeature.Bounds);
                Assert.IsNotNull(foldingFeature.Orientation);
                Assert.IsNotNull(foldingFeature.State);
                Assert.IsNotNull(foldingFeature.OcclusionType);
                _ = foldingFeature.IsSeparating;
            }
            using var root = Root(activity);
            using var list = FindPane(root, "reply-list-pane");
            using var detail = FindPane(root, "reply-detail-pane");
            Assert.IsNotNull(list, "Reply list pane is missing.");
            if (partitions >= 2)
            {
                Assert.IsNotNull(
                    detail,
                    "A multi-partition window did not display the detail pane.");
                using var emptyDetail = Find(
                    root,
                    node => node.VisibleToUser &&
                        node.Text == "Select an email");
                Assert.IsNotNull(
                    emptyDetail,
                    "Expanded Reply did not show its empty detail state.");
            }
            else
            {
                Assert.IsNull(
                    detail,
                    "A single-partition window displayed both Reply panes.");
            }
        }
        finally
        {
            await Finish(activity);
        }
    }

    /// <summary>
    /// A simulated separating, occluding hinge becomes an excluded gap while
    /// NavHost continues to own detail route and recreation state.
    /// </summary>
    [TestMethod]
    public async Task SimulatedHingeSeparatesPanes_AndRouteRestores()
    {
        int hingeLeft = 0;
        int hingeRight = 0;
        global::AndroidX.Compose.Material3.Adaptive.WindowAdaptiveInfo?
            adaptiveInfo =
            null;
        var activity = await Start(current =>
        {
            var metrics = current.Resources?.DisplayMetrics
                ?? throw new InvalidOperationException(
                    "Reply display metrics are unavailable.");
            hingeLeft = metrics.WidthPixels / 2 - 16;
            hingeRight = metrics.WidthPixels / 2 + 16;
            adaptiveInfo ??= AdaptiveTestWindowInfo.ExpandedWithHinge(
                    hingeLeft,
                    0,
                    hingeRight,
                    metrics.HeightPixels,
                    isVertical: true,
                    isSeparating: true,
                    isOccluding: true);
            return adaptiveInfo;
        });
        try
        {
            Assert.AreEqual(
                2,
                await activity.PanePartitionsObserved.Task
                    .WaitAsync(TimeSpan.FromSeconds(15)),
                "Synthetic expanded adaptive info did not request two panes.");
            Assert.AreEqual(
                1,
                activity.ExcludedBoundsCount,
                "Separating vertical hinge was not excluded.");
            AssertPanesAvoidHinge(activity, hingeLeft, hingeRight);

            long selectedId = await ClickFirstVisibleEmail(activity);
            await AssertRoute(activity, Route.EmailDetailPattern);
            AssertPanesAvoidHinge(activity, hingeLeft, hingeRight);

            selectedId = await ClickFirstVisibleEmail(activity, selectedId);
            await AssertRoute(activity, Route.EmailDetailPattern);
            await AssertDetailId(activity, selectedId);
            await activity.OnUi(
                () => Assert.AreEqual(
                    Route.Inbox,
                    activity.Controller.Jvm?.PreviousBackStackEntry
                        ?.Destination.Route,
                    "Selecting another expanded-list item stacked detail routes."));

            adaptiveInfo = AdaptiveTestWindowInfo.Compact();
            await activity.OnUi(
                () => activity.SetAdaptiveInfo(adaptiveInfo));
            await WaitForPaneState(
                activity,
                current => current.PanePartitions == 1 &&
                    current.ExcludedBoundsCount == 0,
                "compact one-pane directive");
            Assert.AreEqual(1, activity.PanePartitions);
            Assert.AreEqual(0, activity.ExcludedBoundsCount);
            await AssertRoute(activity, Route.EmailDetailPattern);
            await AssertDetailId(activity, selectedId);
            AssertPaneVisibility(
                activity,
                listVisible: false,
                detailVisible: true);

            var metrics = activity.Resources?.DisplayMetrics
                ?? throw new InvalidOperationException(
                    "Reply display metrics are unavailable.");
            int hingeTop = metrics.HeightPixels / 2 - 16;
            int hingeBottom = metrics.HeightPixels / 2 + 16;
            adaptiveInfo = AdaptiveTestWindowInfo.ExpandedWithHinge(
                0,
                hingeTop,
                metrics.WidthPixels,
                hingeBottom,
                isVertical: false,
                isSeparating: true,
                isOccluding: true,
                isTabletop: true);
            await activity.OnUi(
                () => activity.SetAdaptiveInfo(adaptiveInfo));
            await WaitForPaneState(
                activity,
                current => current.VerticalPartitions == 2 &&
                    current.ExcludedBoundsCount == 0,
                "tabletop two-vertical-partition directive");
            Assert.AreEqual(
                2,
                activity.VerticalPartitions,
                "Tabletop posture did not request two vertical partitions.");
            Assert.AreEqual(
                0,
                activity.ExcludedBoundsCount,
                "A horizontal hinge was incorrectly treated as a vertical excluded bound.");
            await AssertRoute(activity, Route.EmailDetailPattern);
            await AssertDetailId(activity, selectedId);
            AssertPaneVisibility(
                activity,
                listVisible: true,
                detailVisible: true);

            adaptiveInfo = AdaptiveTestWindowInfo.ExpandedWithHinge(
                hingeLeft,
                0,
                hingeRight,
                metrics.HeightPixels,
                isVertical: true,
                isSeparating: false,
                isOccluding: false);
            await activity.OnUi(
                () => activity.SetAdaptiveInfo(adaptiveInfo));
            await WaitForPaneState(
                activity,
                current => current.ExcludedBoundsCount == 0,
                "nonseparating fold directive");
            Assert.AreEqual(
                0,
                activity.ExcludedBoundsCount,
                "A nonseparating, nonoccluding hinge was incorrectly excluded.");
            await AssertRoute(activity, Route.EmailDetailPattern);
            await AssertDetailId(activity, selectedId);

            adaptiveInfo = AdaptiveTestWindowInfo.Expanded();
            await activity.OnUi(
                () => activity.SetAdaptiveInfo(adaptiveInfo));
            await WaitForPaneState(
                activity,
                current => current.ExcludedBoundsCount == 0,
                "hinge-free expanded directive");
            Assert.AreEqual(
                0,
                activity.ExcludedBoundsCount,
                "Removed hinge remained in the adaptive directive.");
            await AssertRoute(activity, Route.EmailDetailPattern);
            await AssertDetailId(activity, selectedId);
            AssertPaneVisibility(
                activity,
                listVisible: true,
                detailVisible: true);

            activity = await Recreate(activity);
            await AssertRoute(activity, Route.EmailDetailPattern);
            await AssertDetailId(activity, selectedId);
            Assert.AreEqual(
                0,
                activity.ExcludedBoundsCount,
                "Hinge disappearance was not preserved through recreation.");
            AssertPaneVisibility(
                activity,
                listVisible: true,
                detailVisible: true);

            await Back(activity);
            await AssertRoute(activity, Route.Inbox);
            Assert.AreEqual(0L, activity.State.OpenedEmailId.Value);
            AssertPaneVisibility(
                activity,
                listVisible: true,
                detailVisible: true);
        }
        finally
        {
            await Finish(activity);
        }
    }

    /// <summary>
    /// Adaptive posture changes do not recreate the active destination or
    /// search composition; normal tab departure and recreation semantics are
    /// unchanged.
    /// </summary>
    [TestMethod]
    public async Task AdaptiveTransitionsPreserveSearchAndTabState()
    {
        global::AndroidX.Compose.Material3.Adaptive.WindowAdaptiveInfo
            adaptiveInfo =
            AdaptiveTestWindowInfo.Expanded();
        var activity = await Start(_ => adaptiveInfo);
        try
        {
            await TapSearchEditor(activity);
            await SetEditorText(activity, "Bonjour");
            AssertTextPresent(activity, "Bonjour from Paris");

            adaptiveInfo = AdaptiveTestWindowInfo.Compact();
            await activity.OnUi(
                () => activity.SetAdaptiveInfo(adaptiveInfo));
            await activity.AtNativeIdle();
            AssertEditorText(activity, "Bonjour");
            AssertTextPresent(activity, "Bonjour from Paris");
            await AssertRoute(activity, Route.Inbox);

            var metrics = activity.Resources?.DisplayMetrics
                ?? throw new InvalidOperationException(
                    "Reply display metrics are unavailable.");
            adaptiveInfo = AdaptiveTestWindowInfo.ExpandedWithHinge(
                metrics.WidthPixels / 2 - 16,
                0,
                metrics.WidthPixels / 2 + 16,
                metrics.HeightPixels,
                isVertical: true,
                isSeparating: true,
                isOccluding: true);
            await activity.OnUi(
                () => activity.SetAdaptiveInfo(adaptiveInfo));
            await activity.AtNativeIdle();
            AssertEditorText(activity, "Bonjour");
            AssertTextPresent(activity, "Bonjour from Paris");

            await Back(activity);
            activity.ExpectHostWindow();
            await activity.AtNativeIdle();
            await AssertRoute(activity, Route.Inbox);
            await Tap(activity, "Articles");
            await AssertRoute(activity, Route.Articles);

            adaptiveInfo = AdaptiveTestWindowInfo.Compact();
            await activity.OnUi(
                () => activity.SetAdaptiveInfo(adaptiveInfo));
            await activity.AtNativeIdle();
            await AssertRoute(activity, Route.Articles);

            activity = await Recreate(activity);
            await AssertRoute(activity, Route.Articles);
            await Tap(activity, "Inbox");
            await AssertRoute(activity, Route.Inbox);
            await TapSearchEditor(activity);
            AssertEditorText(activity, "");
            AssertTextPresent(activity, "No search history");
        }
        finally
        {
            await Finish(activity);
        }
    }

    /// <summary>Compact inbox FAB follows scroll direction and remains available on detail.</summary>
    [TestMethod]
    public async Task CompactFabCollapsesOnForwardScroll_ExpandsOnBackwardScroll_AndRemainsOnDetail()
    {
        var activity = await Start();
        try
        {
            if (await activity.NavigationTypeObserved.Task.WaitAsync(TimeSpan.FromSeconds(15)) != NavigationSuiteType.NavigationBar)
            {
                Assert.Inconclusive("Scroll-responsive FAB is intentionally limited to compact bottom navigation.");
                return;
            }

            int expandedWidth = FabWidth(activity);
            using (var list = ScrollableRoot(activity))
                Assert.IsTrue(list.PerformAction(global::Android.Views.Accessibility.Action.ScrollForward));
            await activity.AtNativeIdle();
            int collapsedWidth = FabWidth(activity);
            Assert.IsTrue(collapsedWidth < expandedWidth,
                $"Forward scroll did not collapse the FAB: expanded={expandedWidth}, collapsed={collapsedWidth}.");

            await WaitForStableInbox(activity);
            long visibleEmailId = await ClickFirstVisibleEmail(activity);
            await AssertRoute(activity, Route.EmailDetailPattern);
            await AssertDetailId(activity, visibleEmailId);
            Assert.AreEqual(collapsedWidth, FabWidth(activity),
                "Compact detail did not preserve the collapsed inbox FAB state.");
            activity = await Recreate(activity);
            await AssertRoute(activity, Route.EmailDetailPattern);
            await AssertDetailId(activity, visibleEmailId);
            Assert.AreEqual(collapsedWidth, FabWidth(activity),
                "Compact detail did not restore the collapsed inbox FAB state.");
            await Back(activity);
            await AssertRoute(activity, Route.Inbox);

            using (var list = ScrollableRoot(activity))
                Assert.IsTrue(list.PerformAction(global::Android.Views.Accessibility.Action.ScrollBackward));
            await activity.AtNativeIdle();
            int restoredWidth = FabWidth(activity);
            Assert.IsTrue(restoredWidth > collapsedWidth,
                $"Backward scroll did not expand the FAB: collapsed={collapsedWidth}, restored={restoredWidth}.");

            await WaitForStableInbox(activity);
            long expandedEmailId = await ClickFirstVisibleEmail(activity);
            await AssertRoute(activity, Route.EmailDetailPattern);
            await AssertDetailId(activity, expandedEmailId);
            Assert.AreEqual(restoredWidth, FabWidth(activity),
                "Compact detail did not preserve the expanded inbox FAB state.");
            activity = await Recreate(activity);
            await AssertRoute(activity, Route.EmailDetailPattern);
            await AssertDetailId(activity, expandedEmailId);
            Assert.AreEqual(restoredWidth, FabWidth(activity),
                "Compact detail did not restore the expanded inbox FAB state.");
        }
        finally
        {
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
                n.ViewIdResourceName?.EndsWith("reply-email-detail-title", StringComparison.Ordinal) == true)
                ?? throw new InvalidOperationException("Reply detail toolbar title is missing.");
            Assert.AreEqual(email.Subject, title.Text,
                "Reply detail title tag must retain the actual subject text.");
            Assert.IsTrue(string.IsNullOrEmpty(title.ContentDescription),
                "Reply detail title tag must not replace the subject with a generic content description.");
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
                n.ViewIdResourceName?.EndsWith("reply-email-detail-title", StringComparison.Ordinal) == true);
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

    static async Task<ReplyNavigationTestActivity> Start(
        Func<ReplyNavigationTestActivity,
            global::AndroidX.Compose.Material3.Adaptive.WindowAdaptiveInfo?>?
            adaptiveInfoOverrideFactory = null)
    {
        ReplyNavigationTestActivity.Prepare(adaptiveInfoOverrideFactory);
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
        ReplyNavigationTestActivity.PrepareForRecreation();
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

    static AccessibilityNodeInfo? FindPane(
        AccessibilityNodeInfo root,
        string tag) =>
        Find(
            root,
            node => node.VisibleToUser &&
                node.ViewIdResourceName?.EndsWith(
                    tag,
                    StringComparison.Ordinal) == true);

    static void AssertPanesAvoidHinge(
        ReplyNavigationTestActivity activity,
        int hingeLeft,
        int hingeRight)
    {
        using var root = Root(activity);
        using var list = FindPane(root, "reply-list-pane")
            ?? throw new InvalidOperationException(
                "Reply list pane is not visible.");
        using var detail = FindPane(root, "reply-detail-pane")
            ?? throw new InvalidOperationException(
                "Reply detail pane is not visible.");
        using var listBounds = new Rect();
        using var detailBounds = new Rect();
        list.GetBoundsInScreen(listBounds);
        detail.GetBoundsInScreen(detailBounds);
        const int tolerance = 4;
        Assert.IsTrue(
            listBounds.Right <= hingeLeft + tolerance,
            $"List pane overlaps the simulated hinge: list={listBounds}, hinge={hingeLeft}..{hingeRight}.");
        Assert.IsTrue(
            detailBounds.Left >= hingeRight - tolerance,
            $"Detail pane overlaps the simulated hinge: detail={detailBounds}, hinge={hingeLeft}..{hingeRight}.");
    }

    static void AssertPaneVisibility(
        ReplyNavigationTestActivity activity,
        bool listVisible,
        bool detailVisible)
    {
        using var root = Root(activity);
        using var list = FindPane(root, "reply-list-pane");
        using var detail = FindPane(root, "reply-detail-pane");
        Assert.AreEqual(
            listVisible,
            list is not null,
            $"Reply list pane visibility should be {listVisible}.");
        Assert.AreEqual(
            detailVisible,
            detail is not null,
            $"Reply detail pane visibility should be {detailVisible}.");
    }

    static async Task SetEditorText(
        ReplyNavigationTestActivity activity,
        string text)
    {
        using var root = Root(activity);
        using var editor = Find(root, node => node.VisibleToUser && node.Editable)
            ?? throw new InvalidOperationException(
                "Reply search editor is not visible.");
        using var arguments = new Bundle();
        arguments.PutCharSequence(
            AccessibilityNodeInfo.ActionArgumentSetTextCharsequence,
            text);
        Assert.IsTrue(
            editor.PerformAction(
                global::Android.Views.Accessibility.Action.SetText,
                arguments),
            "Reply search query action was rejected.");
        await activity.AtNativeIdle();
        AssertEditorText(activity, text);
    }

    static async Task TapSearchEditor(
        ReplyNavigationTestActivity activity)
    {
        activity.ExpectOwnedPopup();
        using var root = Root(activity);
        using var editor = Find(
            root,
            node => node.VisibleToUser && node.Editable)
            ?? throw new InvalidOperationException(
                "Reply search editor is not visible.");
        PerformClick(editor, longClick: false);
        await activity.AtNativeIdle();
    }

    static void AssertEditorText(
        ReplyNavigationTestActivity activity,
        string expected)
    {
        using var root = Root(activity);
        using var editor = Find(root, node => node.VisibleToUser && node.Editable)
            ?? throw new InvalidOperationException(
                "Reply search editor is not visible.");
        Assert.AreEqual(expected, editor.Text ?? "");
    }

    static void AssertTextPresent(
        ReplyNavigationTestActivity activity,
        string text)
    {
        using var root = Root(activity);
        using var node = Find(
            root,
            candidate => candidate.VisibleToUser &&
                candidate.Text == text);
        Assert.IsNotNull(
            node,
            $"Reply text '{text}' is not visible.");
    }

    static async Task Tap(ReplyNavigationTestActivity activity, string description)
    {
        await activity.AtNativeIdle();
        using var root = Root(activity);
        using var label = FindNavigationItem(root, description)
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
        try
        {
            Runner.SendPointerSync(down);
        }
        catch (Java.Lang.IllegalArgumentException error)
        {
            throw await PointerInjectionFailure(activity, root.WindowId, bounds, "down", error);
        }
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
            try
            {
                Runner.SendPointerSync(up);
            }
            catch (Java.Lang.IllegalArgumentException error)
            {
                throw await PointerInjectionFailure(activity, root.WindowId, bounds, "up", error);
            }
        }
        await activity.AtNativeIdle();
    }

    static async Task<InvalidOperationException> PointerInjectionFailure(
        ReplyNavigationTestActivity activity,
        int ownedWindowId,
        Rect target,
        string stage,
        Exception inner)
    {
        string focusedView = "unknown";
        bool activityFocused = false;
        int viewWindowId = -1;
        await activity.OnUi(() =>
        {
            activityFocused = activity.View.HasWindowFocus;
            focusedView = activity.Window?.CurrentFocus?.Class?.Name ?? "none";
            using var viewInfo = activity.View.CreateAccessibilityNodeInfo()
                ?? throw new InvalidOperationException("Reply view accessibility node is unavailable.");
            viewWindowId = viewInfo.WindowId;
        });
        using var active = Runner.UiAutomation?.RootInActiveWindow;
        return new InvalidOperationException(
            $"Reply pointer {stage} injection failed: pid={AndroidProcess.MyPid()}, " +
            $"uid={AndroidProcess.MyUid()}, ownedWindow={ownedWindowId}, " +
            $"viewWindow={viewWindowId}, activityFocused={activityFocused}, focusedView={focusedView}, " +
            $"activeWindow={active?.WindowId}, activePackage={active?.PackageName}, target={target}.",
            inner);
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

    static async Task<long> ClickFirstVisibleEmail(
        ReplyNavigationTestActivity activity,
        long excludedId = 0)
    {
        using var root = Root(activity);
        foreach (var email in LocalEmailsDataProvider.AllEmails)
        {
            if (email.Id == excludedId)
                continue;
            using var label = Find(root, node => node.VisibleToUser &&
                node.Text?.Contains(email.Subject, StringComparison.Ordinal) == true);
            if (label is null)
                continue;
            PerformClick(label, longClick: false);
            await activity.AtNativeIdle();
            return email.Id;
        }
        throw new InvalidOperationException("Reply inbox has no visible email to open.");
    }

    static async Task WaitForStableInbox(ReplyNavigationTestActivity activity)
    {
        var previous = CaptureInbox(activity);
        for (int attempt = 0; attempt < 5; attempt++)
        {
            await Task.Delay(100);
            await activity.AtNativeIdle();
            var current = CaptureInbox(activity);
            if (previous.SequenceEqual(current))
                return;
            previous = current;
        }
        throw new InvalidOperationException("Reply inbox viewport did not stabilize before row activation.");
    }

    static async Task WaitForPaneState(
        ReplyNavigationTestActivity activity,
        Func<ReplyNavigationTestActivity, bool> predicate,
        string expected)
    {
        using var timeout = new CancellationTokenSource(
            TimeSpan.FromSeconds(15));
        try
        {
            while (!predicate(activity))
            {
                await Task.Delay(50, timeout.Token);
                await activity.AtNativeIdle();
            }
        }
        catch (System.OperationCanceledException error)
            when (timeout.IsCancellationRequested)
        {
            throw new TimeoutException(
                $"Reply did not publish the expected {expected}: " +
                $"horizontal={activity.PanePartitions}, " +
                $"vertical={activity.VerticalPartitions}, " +
                $"excluded={activity.ExcludedBoundsCount}.",
                error);
        }
    }

    static Task AssertDetailId(ReplyNavigationTestActivity activity, long id) =>
        activity.OnUi(() => Assert.AreEqual(
            id.ToString(),
            activity.Controller.CurrentBackStackEntry?.Arguments?.GetString("emailId"),
            "Reply opened a different email than the row activated from the owned snapshot."));

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

    static long FirstVisibleAvatarEmailId(
        ReplyNavigationTestActivity activity,
        IReadOnlyList<(long Id, int Top)> viewport)
    {
        using var root = Root(activity);
        foreach (var item in viewport)
        {
            var email = LocalEmailsDataProvider.Get(item.Id)
                ?? throw new InvalidOperationException(
                    $"Reply email {item.Id} was not found.");
            using var avatar = Find(
                root,
                node => node.VisibleToUser &&
                    node.ContentDescription == email.Sender.FullName);
            if (avatar is not null)
                return item.Id;
        }
        throw new InvalidOperationException(
            "The restored Reply viewport had no fully visible email avatar.");
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

    static int FabWidth(ReplyNavigationTestActivity activity)
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
            using var bounds = new Rect();
            current.GetBoundsInScreen(bounds);
            return bounds.Width();
        }
        finally { current.Dispose(); }
    }

    static async Task AssertRoute(ReplyNavigationTestActivity activity, string route)
    {
        await activity.OnUi(() => Assert.AreEqual(route, activity.Controller.CurrentBackStackEntry?.Route));
        if (activity.OwnedPopupExpected)
            return;
        using var root = Root(activity);
        string selected = route == Route.EmailDetailPattern ? "Inbox"
            : Label(TopLevelDestinations.All.Single(d => d.Route == route));
        using var label = FindNavigationItem(root, selected)
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
        if (root.PackageName != "net.compose.devicetests" ||
            (!activity.OwnedPopupExpected && root.WindowId != windowId))
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
