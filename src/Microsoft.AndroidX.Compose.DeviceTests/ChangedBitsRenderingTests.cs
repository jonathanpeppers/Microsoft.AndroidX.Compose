namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Checks actual rendering and callback dispatch when Static permits a parent to skip.</summary>
[TestClass]
[DoNotParallelize]
public class ChangedBitsRenderingTests
{
    [TestMethod]
    public async Task StaticParentSkips_TrackedContentAndCallbacksStillUpdate()
    {
        using var intent = new global::Android.Content.Intent(
            global::Android.App.Application.Context, typeof(ChangedBitsTestActivity));
        intent.AddFlags(global::Android.Content.ActivityFlags.NewTask);
        global::Android.App.Application.Context.StartActivity(intent);
        ChangedBitsTestActivity? activity = null;
        try
        {
            for (int attempt = 0; attempt < 200; attempt++)
            {
                activity = ChangedBitsTestActivity.Current;
                if (activity is not null && Volatile.Read(ref activity.CompletedRevision) == 0)
                    break;
                await Task.Delay(50);
            }
            Assert.IsNotNull(activity);
            Assert.AreEqual(0, Volatile.Read(ref activity.CompletedRevision), "Initial rendering did not finish.");
            int stable = activity.StableExecutions;
            int changing = activity.ChangingExecutions;
            int boundaries = activity.BoundaryExecutions;
            var retained = activity.RetainedCallback;
            var retainedTyped = activity.RetainedTypedCallback;
            Assert.IsNotNull(retained);
            Assert.IsNotNull(retainedTyped);
            for (int revision = 1; revision <= 5; revision++)
            {
                int target = revision;
                activity.RunOnUiThread(() => activity.Revision.Value = target);
                for (int attempt = 0; attempt < 200; attempt++)
                {
                    if (Volatile.Read(ref activity.CompletedRevision) == target &&
                        activity.ContentRevision == target &&
                        activity.GeneratedContentRevision == target &&
                        activity.DirectContentRevision == target)
                        break;
                    await Task.Delay(50);
                }
                Assert.AreEqual(target, activity.ContentRevision, "Tracked content under a skipped parent went stale.");
                Assert.AreEqual(target, activity.GeneratedContentRevision, "Generated tree facade content went stale.");
                Assert.AreEqual(target, activity.DirectContentRevision, "Direct catalog content went stale.");
                Assert.AreEqual(stable, activity.StableExecutions, "Unchanged generated sibling should skip.");
                Assert.AreEqual(changing + target, activity.ChangingExecutions, "Real input changes must execute.");
                Assert.AreEqual(boundaries, activity.BoundaryExecutions, "Static parent should skip.");
                Assert.IsTrue(activity.BoundarySkips >= target);
                Assert.IsTrue(activity.StablePeers);
                Assert.AreSame(retained, activity.RetainedCallback);
                Assert.AreSame(retainedTyped, activity.RetainedTypedCallback);
                retained.Invoke();
                using var argument = Java.Lang.Integer.ValueOf(10);
                retainedTyped.Invoke(argument);
                Assert.AreEqual(target, activity.CallbackRevision);
                Assert.AreEqual(target + 10, activity.TypedCallbackRevision);
            }
            Console.WriteLine($"5 input updates: stable-body delta={activity.StableExecutions - stable}, " +
                $"changing-body delta={activity.ChangingExecutions - changing}, " +
                $"static-parent delta={activity.BoundaryExecutions - boundaries}, skips={activity.BoundarySkips}. " +
                "Debug render counts only; no timing or speedup claim.");
            int forced = activity.ForcedExecutions;
            activity.RunOnUiThread(() => activity.ForceRevision.Value = 1);
            for (int attempt = 0; attempt < 200 &&
                Volatile.Read(ref activity.CompletedForceRevision) != 1; attempt++)
                await Task.Delay(50);
            Assert.AreEqual(1, Volatile.Read(ref activity.CompletedForceRevision));
            Assert.AreEqual(forced + 1, activity.ForcedExecutions,
                "Invalidation must force an unchanged-parameter generated restart core to execute.");
            Assert.AreEqual(stable, activity.StableExecutions);
        }
        finally
        {
            activity?.RunOnUiThread(activity.Finish);
        }
    }
}
