using AndroidX.Compose;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Native acceptance for plain-node indicators and explicit collapse thresholds.</summary>
[TestClass]
[DoNotParallelize]
public class FlowOverflowOptionsTests
{
    [TestMethod]
    [DataRow(3, true)] [DataRow(3, false)]
    [DataRow(4, true)] [DataRow(4, false)]
    public async Task PlainNodeExpandIndicatorUsesNativeActionAndDisappears(int style, bool horizontal)
    {
        var activity = await FlowOverflowTests.Start(style, horizontal, 2);
        try
        {
            await ExpectDraw(activity, 0, 2);
            FlowTestAdmission.OnUi(() =>
            {
                Assert.AreEqual((true, 0, horizontal ? ScopeKind.Row : ScopeKind.Column),
                    activity.NodeIndicators[0]);
            });
            await FlowOverflowTests.Click(activity, "flow-expand");
            await ExpectDraw(activity, 1, 8);
            FlowTestAdmission.OnUi(() =>
            {
                Assert.IsFalse(activity.NodeIndicators.ContainsKey(1), "Expand-only node was drawn after all items fit.");
                Assert.AreEqual(1, activity.Clicks);
            });
        }
        finally { await FlowOverflowTests.Finish(activity); }
    }

    [TestMethod]
    [DataRow(3, true)] [DataRow(3, false)]
    [DataRow(4, true)] [DataRow(4, false)]
    public async Task PlainNodeCollapseRespectsBothExplicitThresholds(int style, bool horizontal)
    {
        var activity = await FlowOverflowTests.Start(style, horizontal, 3, thresholdPhase: 1);
        try
        {
            await ExpectDraw(activity, 0, 2);
            await FlowOverflowTests.Click(activity, "flow-expand");
            await ExpectDraw(activity, 1, 8);
            FlowTestAdmission.OnUi(() =>
            {
                Assert.IsFalse(activity.NodeIndicators.ContainsKey(1),
                    "Collapse appeared below the explicit four-line threshold.");
                activity.ThresholdPhase.Value = 2;
                activity.Generation.Value++;
            });
            await ExpectDraw(activity, 2, 8);
            FlowTestAdmission.OnUi(() =>
            {
                Assert.IsFalse(activity.NodeIndicators.ContainsKey(2),
                    "Collapse appeared below the explicit 200dp cross-axis threshold.");
                activity.ThresholdPhase.Value = 3;
                activity.Generation.Value++;
            });
            await ExpectDraw(activity, 3, 8);
            await FlowOverflowTests.WaitFor(() => activity.NodeIndicators.ContainsKey(3),
                "Collapse did not draw above the explicit two-line/96dp thresholds.");
            FlowTestAdmission.OnUi(() =>
            {
                Assert.AreEqual((false, 0, horizontal ? ScopeKind.Row : ScopeKind.Column),
                    activity.NodeIndicators[3]);
            });
            await FlowOverflowTests.Click(activity, "flow-collapse");
            await ExpectDraw(activity, 4, 2);
            FlowTestAdmission.OnUi(() =>
            {
                Assert.AreEqual((true, 1, horizontal ? ScopeKind.Row : ScopeKind.Column),
                    activity.NodeIndicators[4], "The plain expand node lost its remembered click state.");
                Assert.AreEqual(2, activity.Clicks);
            });
        }
        finally { await FlowOverflowTests.Finish(activity); }
    }

    static async Task ExpectDraw(FlowOverflowTestActivity activity, int generation, int count)
    {
        await FlowOverflowTests.WaitFor(() =>
            activity.DrawnItems.TryGetValue(generation, out var items) && items.Count >= count,
            $"Plain-node flow did not draw generation {generation}.");
        FlowTestAdmission.OnUi(() =>
        {
            activity.Admission.RequireLive(activity);
            Assert.AreEqual(count, activity.DrawnItems[generation].Count);
            Assert.AreEqual(8, activity.ComposedItems[generation].Count);
            Assert.IsTrue(activity.OuterRestored);
        });
        Console.WriteLine($"FLOW_NODE instance={activity.Admission.InstanceId} pid={activity.Admission.NativePid} " +
            $"window={activity.Admission.WindowId} generation={generation} drawn={count}");
    }
}
