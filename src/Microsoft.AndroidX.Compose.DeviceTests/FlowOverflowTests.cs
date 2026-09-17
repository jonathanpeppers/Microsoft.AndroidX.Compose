using Android.Views.Accessibility;
using AndroidX.Compose;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Exact pinned-native parity, clipping, interaction, scope and recomposition regressions.</summary>
[TestClass]
[DoNotParallelize]
public class FlowOverflowTests
{
    static TestInstrumentation Runner => TestInstrumentation.Current
        ?? throw new InvalidOperationException("Flow tests require native instrumentation.");

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public Task NativeControl_CharacterizesPinnedCountLifetime(bool horizontal) =>
        Exercise(2, horizontal, 3);

    [TestMethod]
    [DataRow(0, true)] [DataRow(0, false)]
    [DataRow(1, true)] [DataRow(1, false)]
    public Task Managed_MatchesPinnedNativeCountLifetime(int style, bool horizontal) =>
        Exercise(style, horizontal, 3);

    [TestMethod]
    [DataRow(0, true)] [DataRow(0, false)]
    [DataRow(1, true)] [DataRow(1, false)]
    public async Task ExpandOnly_StopsShowingAnIndicatorWhenAllItemsFit(int style, bool horizontal)
    {
        var activity = await Start(style, horizontal, 2);
        try
        {
            await Expect(activity, 0, 8, 2, true, horizontal);
            await Click(activity, "flow-expand");
            await WaitFor(() => activity.DrawnItems.TryGetValue(1, out var items) && items.Count == 8,
                "Expanded flow did not draw all eight regular cells.");
            Assert.AreEqual(1, activity.Clicks);
            Assert.AreEqual(0, activity.Last?.Generation, "Expand-only indicator should no longer be placed.");
        }
        finally { await Finish(activity); }
    }

    [TestMethod]
    [DataRow(0, true, 0)] [DataRow(0, false, 0)]
    [DataRow(1, true, 0)] [DataRow(1, false, 0)]
    [DataRow(0, true, 1)] [DataRow(0, false, 1)]
    [DataRow(1, true, 1)] [DataRow(1, false, 1)]
    public async Task OmittedAndExplicitClip_DrawExactlyThreeRegularItems(int style, bool horizontal, int policy)
    {
        var activity = await Start(style, horizontal, policy);
        try
        {
            await WaitFor(() => activity.DrawnItems.TryGetValue(0, out var items) && items.Count >= 3,
                "Clipped flow did not place its three regular cells.");
            Runner.WaitForIdleSync();
            FlowTestAdmission.OnUi(() =>
            {
                activity.Admission.RequireLive(activity);
                int[] expected = [0, 1, 2];
                CollectionAssert.AreEquivalent(expected, activity.DrawnItems[0].ToArray());
                Assert.IsNull(activity.Last);
                Assert.IsTrue(activity.OuterRestored);
            });
        }
        finally { await Finish(activity); }
    }

    static async Task Exercise(int style, bool horizontal, int policy)
    {
        var activity = await Start(style, horizontal, policy);
        try
        {
            var first = await Expect(activity, 0, 8, 2, true, horizontal);
            object? firstItemState = null;
            FlowTestAdmission.OnUi(() => firstItemState = activity.ComposedItems[0][0]);
            Assert.AreEqual(0, first.Counter);
            Assert.IsTrue(activity.PrematureReadRejected, "Shown count must preserve the native pre-measure failure.");
            await Click(activity, "flow-expand");
            await Expect(activity, 1, 8, 8, false, horizontal);
            await Click(activity, "flow-collapse");
            var collapsed = await Expect(activity, 2, 8, 2, true, horizontal);
            Assert.AreSame(first.Identity, collapsed.Identity, "Indicator remember was reset during expansion.");
            Assert.AreEqual(1, collapsed.Counter, "Indicator-local click state was lost during expansion.");
            FlowTestAdmission.OnUi(() =>
            {
                activity.Total.Value = 5;
                activity.Generation.Value++;
            });
            // Foundation 1.11.3 retains this native scope until maxLines changes.
            // The list really contains five items; the scope's cached total is still eight.
            await Expect(activity, 3, 8, 2, true, horizontal);
            FlowTestAdmission.OnUi(() =>
            {
                Assert.AreEqual(5, activity.Total.Value);
                Assert.AreEqual(5, activity.ComposedItems[3].Count, "The updated item content did not compose.");
                Assert.AreSame(firstItemState, activity.ComposedItems[3][0], "Item remember was reset by a content update.");
            });
            await Click(activity, "flow-expand");
            await Expect(activity, 4, 5, 5, false, horizontal);
            int rootPasses = activity.RootPasses;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Java.Lang.JavaSystem.Gc();
            FlowTestAdmission.OnUi(() => activity.Tick.Value++);
            await WaitFor(() => activity.Last?.Tick == 1, "Indicator-only state did not recompose after GC.");
            var refreshed = await Expect(activity, 4, 5, 5, false, horizontal);
            Assert.AreEqual(1, refreshed.Tick);
            Assert.AreEqual(1, refreshed.Counter);
            Assert.AreEqual(rootPasses, activity.RootPasses, "Indicator-local state should not restart the flow root.");
            Assert.AreEqual(3, activity.Clicks);
        }
        finally { await Finish(activity); }
    }

    static async Task<FlowOverflowSnapshot> Expect(FlowOverflowTestActivity activity,
        int generation, int total, int shown, bool expand, bool horizontal)
    {
        await WaitFor(() => activity.Last?.Generation == generation && activity.Last.Expand == expand,
            $"Indicator did not draw generation {generation} ({(expand ? "expand" : "collapse")}).");
        Runner.WaitForIdleSync();
        FlowOverflowSnapshot? snapshot = null;
        string trace = "FLOW snapshot unavailable.";
        try
        {
            FlowTestAdmission.OnUi(() =>
            {
                activity.Admission.RequireLive(activity);
                snapshot = activity.Last ?? throw new InvalidOperationException("No flow draw snapshot.");
                trace = $"FLOW pid={snapshot.NativePid} direction={(horizontal ? "row" : "column")} " +
                    $"instance={snapshot.InstanceId} window={activity.Admission.WindowId} " +
                    $"generation={generation} requestedItems={activity.Total.Value} " +
                    $"nativeScope={snapshot.Total}/{snapshot.Shown} expectedPinnedScope={total}/{shown} expand={snapshot.Expand}";
                Assert.AreEqual(activity.Admission.InstanceId, snapshot.InstanceId, "Snapshot belongs to a different fixture.");
                Assert.AreEqual(activity.Admission.NativePid, snapshot.NativePid, "Snapshot belongs to a different native process.");
                Assert.AreEqual(total, snapshot.Total, "Total count differs from the pinned native scope contract.");
                Assert.AreEqual(shown, snapshot.Shown, "Shown count differs from the pinned native scope contract.");
                Assert.AreEqual(horizontal ? ScopeKind.Row : ScopeKind.Column, snapshot.Kind);
                Assert.IsTrue(activity.OuterRestored);
                string key = expand ? "expand" : "collapse";
                Assert.AreEqual(ScopeKind.Box, activity.ScopeChecks[key + "-before"]);
                Assert.AreEqual(ScopeKind.Box, activity.ScopeChecks[key + "-after"]);
                Assert.AreEqual((4, 1), activity.NestedCounts[key], "Nested flow inherited the outer counts.");
                Assert.AreEqual(shown, activity.DrawnItems[generation].Count, "Drawn items disagree with the native shown count.");
            });
        }
        finally { Console.WriteLine(trace); }
        return snapshot ?? throw new InvalidOperationException("Flow snapshot was not captured.");
    }

    internal static async Task<FlowOverflowTestActivity> Start(int style, bool horizontal, int policy,
        int thresholdPhase = 0)
    {
        var context = Runner.TargetContext ?? throw new InvalidOperationException("Flow test target context missing.");
        FlowOverflowTestActivity.Ready = FlowOverflowTestActivity.NewReady();
        using var intent = new global::Android.Content.Intent(context, typeof(FlowOverflowTestActivity));
        intent.AddFlags(global::Android.Content.ActivityFlags.NewTask);
        intent.PutExtra("style", style);
        intent.PutExtra("horizontal", horizontal);
        intent.PutExtra("policy", policy);
        intent.PutExtra("thresholdPhase", thresholdPhase);
        FlowTestAdmission.OnUi(() => context.StartActivity(intent));
        var activity = await FlowOverflowTestActivity.Ready.Task.WaitAsync(TimeSpan.FromSeconds(15));
        try
        {
            await activity.Admission.Admit(activity);
            return activity;
        }
        catch
        {
            await Finish(activity);
            throw;
        }
    }

    internal static async Task Finish(FlowOverflowTestActivity activity)
    {
        FlowTestAdmission.OnUi(() =>
        {
            activity.Admission.Ending = true;
            activity.Finish();
        });
        await activity.Destroyed.Task.WaitAsync(TimeSpan.FromSeconds(15));
    }

    internal static async Task WaitFor(Func<bool> predicate, string message)
    {
        for (int i = 0; i < 150; i++)
        {
            bool ready = false;
            FlowTestAdmission.OnUi(() => ready = predicate());
            if (ready) return;
            await Task.Delay(100);
        }
        Assert.Fail(message);
    }

    internal static async Task Click(FlowOverflowTestActivity activity, string description)
    {
        for (int attempt = 0; attempt < 30; attempt++)
        {
            using var root = activity.Admission.AcquireRoot(activity);
            var matches = new List<AccessibilityNodeInfo>();
            try
            {
                int remaining = 100;
                int labels = 0;
                FindTargets(root, description, matches, activity.Admission.WindowId, ref remaining, ref labels);
                if (matches.Count > 1 || (matches.Count == 0 && attempt == 29))
                {
                    Console.WriteLine($"FLOW_LOOKUP_FAILURE target={description} matches={matches.Count}");
                    try { Console.WriteLine(FlowTestEvidence.Describe(activity, root, description)); }
                    catch (Exception error) { Console.WriteLine("FLOW_DIAGNOSTIC_CAPTURE_ERROR " + error); }
                }
                Assert.IsTrue(matches.Count <= 1, "Overflow click target is ambiguous.");
                if (matches.Count == 1)
                {
                    var target = matches[0];
                    Assert.AreEqual("net.compose.devicetests", target.PackageName);
                    Assert.AreEqual(activity.Admission.WindowId, target.WindowId);
                    Assert.IsTrue(target.Enabled && target.VisibleToUser, "Overflow target is not actionable.");
                    FlowTestAdmission.OnUi(() => activity.Admission.RequireLive(activity));
                    Assert.IsTrue(target.PerformAction(global::Android.Views.Accessibility.Action.Click),
                        $"Native overflow action was rejected: {description}.");
                    return;
                }
            }
            finally { foreach (var match in matches) match.Dispose(); }
            await Task.Delay(100);
        }
        Assert.Fail($"No actionable native overflow indicator '{description}'.");
    }

    static void FindTargets(AccessibilityNodeInfo node, string description, List<AccessibilityNodeInfo> matches,
        int windowId, ref int remaining, ref int labels, int depth = 0)
    {
        if (remaining-- <= 0 || depth > 12)
            throw new InvalidOperationException("Cannot establish a unique overflow target within the bounded tree.");
        if (node.PackageName != "net.compose.devicetests" || node.WindowId != windowId)
            throw new InvalidOperationException("Overflow lookup reached a foreign package or native window.");
        if (node.ContentDescription == description)
        {
            Assert.IsTrue(++labels == 1, "Overflow description label is ambiguous.");
            using var parent = node.Clickable ? null : node.Parent;
            var target = node.Clickable ? node : parent;
            if (target is not null && target.Clickable && target.Enabled && target.VisibleToUser && node.VisibleToUser &&
                target.PackageName == node.PackageName && target.WindowId == node.WindowId &&
                target.ActionList?.Any(action => action.Id == (int)global::Android.Views.Accessibility.Action.Click) == true)
            {
                using var labelBounds = new global::Android.Graphics.Rect();
                using var targetBounds = new global::Android.Graphics.Rect();
                node.GetBoundsInScreen(labelBounds);
                target.GetBoundsInScreen(targetBounds);
                if (labelBounds.Equals(targetBounds))
                    matches.Add((OperatingSystem.IsAndroidVersionAtLeast(33)
                        ? new AccessibilityNodeInfo(target) : AccessibilityNodeInfo.Obtain(target))
                        ?? throw new InvalidOperationException("Could not copy the overflow accessibility target."));
            }
        }
        for (int i = 0; i < node.ChildCount; i++)
        {
            using var child = node.GetChild(i);
            if (child is not null)
                FindTargets(child, description, matches, windowId, ref remaining, ref labels, depth + 1);
        }
    }
}
