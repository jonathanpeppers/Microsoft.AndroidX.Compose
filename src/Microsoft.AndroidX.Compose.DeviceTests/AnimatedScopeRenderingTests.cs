using AndroidX.Compose;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Rendering, independent recomposition, nested receivers and native animation disposal.</summary>
[TestClass]
[DoNotParallelize]
public class AnimatedScopeRenderingTests
{
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task NativeChildren_RestoreScopesAndOutliveTheParentEffect(bool direct)
    {
        var instrumentation = TestInstrumentation.Current
            ?? throw new InvalidOperationException("Animation tests require native instrumentation.");
        var context = instrumentation.TargetContext
            ?? throw new InvalidOperationException("Instrumentation target context unavailable.");
        AnimatedScopeTestActivity.Ready = AnimatedScopeTestActivity.NewReady();
        using var intent = new global::Android.Content.Intent(context, typeof(AnimatedScopeTestActivity));
        intent.AddFlags(global::Android.Content.ActivityFlags.NewTask);
        intent.PutExtra("direct", direct);
        instrumentation.RunOnMainSync(() => context.StartActivity(intent));
        var activity = await AnimatedScopeTestActivity.Ready.Task.WaitAsync(TimeSpan.FromSeconds(15));
        try
        {
            await Task.WhenAll(activity.Measured.Task, activity.Committed.Task, activity.Entered.Task)
                .WaitAsync(TimeSpan.FromSeconds(15));
            object? beforeIdentity = null;
            int parentPasses = 0;
            instrumentation.RunOnMainSync(() =>
            {
                Assert.IsTrue(activity.OutsideScopeRestored);
                CheckScopes(activity, 0);
                beforeIdentity = activity.Identities["outer-before"];
                parentPasses = activity.RootPasses;
                activity.Committed = AnimatedScopeTestActivity.NewSignal();
                activity.Tick.Value++;
            });
            await activity.Committed.Task.WaitAsync(TimeSpan.FromSeconds(15));
            instrumentation.RunOnMainSync(() =>
            {
                Assert.AreEqual(parentPasses, activity.RootPasses,
                    "Only leaf state changed: children must execute without the outer animation call.");
                Assert.AreSame(beforeIdentity, activity.Identities["outer-before"]);
                CheckScopes(activity, 0);
            });
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Java.Lang.JavaSystem.Gc();
            Task? oldContentDisposed = null;
            instrumentation.RunOnMainSync(() =>
            {
                oldContentDisposed = Task.WhenAll(activity.Disposals["content-0"].Task, activity.Disposals["nested-0"].Task);
                activity.Step.Value = 1;
            });
            await (oldContentDisposed ?? throw new InvalidOperationException("Outgoing content disposal was not captured."))
                .WaitAsync(TimeSpan.FromSeconds(15));
            instrumentation.RunOnMainSync(() =>
            {
                CheckScopes(activity, 1);
                Assert.AreSame(beforeIdentity, activity.Identities["outer-before"]);
                Assert.AreNotEqual(activity.Scopes["content-0"], activity.Scopes["content-1"],
                    "Outgoing and incoming AnimatedContent values own distinct native scopes.");
                activity.Visible.Value = false;
            });
            await activity.ExitStarted.Task.WaitAsync(TimeSpan.FromSeconds(15));
            Task? allDisposed = null;
            instrumentation.RunOnMainSync(() =>
            {
                Assert.IsTrue(activity.ExitWasLive, "Child must still be installed when its native exit is running.");
                Assert.IsGreaterThanOrEqualTo(800_000_000L, activity.ExitDurationNanos,
                    "The native transition must include the 800 ms child, not only the 100 ms parent fade.");
                allDisposed = Task.WhenAll(activity.Disposals.Values.Select(d => d.Task));
                Console.WriteLine($"Animated scopes direct={direct}: native exit duration={activity.ExitDurationNanos}, " +
                    $"live={activity.ExitWasLive}, independent child recomposition, measured={activity.Placed.Count}.");
            });
            await (allDisposed ?? throw new InvalidOperationException("Animated disposal tasks were not captured."))
                .WaitAsync(TimeSpan.FromSeconds(15));
            instrumentation.RunOnMainSync(() =>
            {
                activity.Entered = AnimatedScopeTestActivity.NewSignal();
                activity.Visible.Value = true;
            });
            await activity.Entered.Task.WaitAsync(TimeSpan.FromSeconds(15));
            instrumentation.RunOnMainSync(() =>
            {
                CheckScopes(activity, 1);
                Assert.AreNotSame(beforeIdentity, activity.Identities["outer-before"],
                    "A fully exited and re-entered child must have a fresh composition identity.");
                Assert.IsTrue(activity.OutsideScopeRestored);
            });
        }
        finally
        {
            instrumentation.RunOnMainSync(activity.Finish);
            await activity.Destroyed.Task.WaitAsync(TimeSpan.FromSeconds(15));
        }
    }

    static void CheckScopes(AnimatedScopeTestActivity activity, int value)
    {
        var outer = activity.Scopes["outer-before"];
        var content = activity.Scopes[$"content-{value}"];
        var nested = activity.Scopes[$"nested-{value}"];
        Assert.AreEqual(outer, activity.Scopes["outer-after"], "Nested animated children must restore the parent scope.");
        Assert.AreNotEqual(outer, content);
        Assert.AreNotEqual(content, nested);
        Assert.AreNotEqual(outer, nested);
        Assert.IsNull(RenderContext.CurrentAnimatedVisibilityScope, "Animated scope must not leak into UI events.");
    }
}
