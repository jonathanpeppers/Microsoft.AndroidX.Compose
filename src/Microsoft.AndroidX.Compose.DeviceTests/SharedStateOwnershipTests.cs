namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Verifies shared native state remains registered for activity save and restore.</summary>
[TestClass]
[DoNotParallelize]
public class SharedStateOwnershipTests
{
    [TestMethod]
    [DataRow("native")]
    [DataRow("tree")]
    [DataRow("direct")]
    [DataRow("owned-tree")]
    [DataRow("owned-direct")]
    [DataRow("owned-omitted-tree")]
    [DataRow("owned-omitted-direct")]
    public async Task RepeatedRenderThenRecreation_RestoresSharedTime(string mode)
    {
        var activity = await StartActivity(mode);
        try
        {
            var originalState = activity.State;
            for (int pass = 1; pass <= 3; pass++)
            {
                int nextPass = pass;
                await OnUiThread(activity, () =>
                {
                    activity.State.Hour = 19;
                    activity.State.Minute = 27;
                    activity.Pass.Value = nextPass;
                });
                await WaitFor(() => activity.CompletedPass == nextPass,
                    "Shared-state parent did not execute again.");
                Assert.IsTrue(activity.SiblingsSharePeer, "Sibling consumers must use one state peer.");
            }

            await OnUiThread(activity, activity.Recreate);
            await WaitFor(() => SharedStateOwnershipTestActivity.Current is { } current
                && !ReferenceEquals(current, activity) && current.CompletedPass >= 0,
                "Shared-state activity did not recreate.");
            activity = SharedStateOwnershipTestActivity.Current
                ?? throw new InvalidOperationException("Recreated activity was not available.");

            Assert.AreNotSame(originalState, activity.State, "Restore must use a fresh managed wrapper.");
            Assert.AreEqual(19, activity.State.Hour, "Native save provider lost the selected hour.");
            Assert.AreEqual(27, activity.State.Minute, "Native save provider lost the selected minute.");
            Assert.IsTrue(activity.SiblingsSharePeer);
        }
        finally
        {
            await OnUiThread(activity, activity.Finish);
            await WaitFor(() => !ReferenceEquals(SharedStateOwnershipTestActivity.Current, activity),
                "Shared-state activity did not finish.");
        }
    }

    [TestMethod]
    [DataRow("owned-tree")]
    [DataRow("owned-direct")]
    [DataRow("owned-omitted-tree")]
    [DataRow("owned-omitted-direct")]
    public async Task AncestorOwner_PreservesPeerWhenConsumersLeaveAndReturn(string mode)
    {
        var activity = await StartActivity(mode);
        try
        {
            var peer = activity.State.Jvm
                ?? throw new InvalidOperationException("Shared state was not bound.");
            for (int pass = 1; pass <= 3; pass++)
            {
                int nextPass = pass;
                await OnUiThread(activity, () =>
                {
                    activity.State.Hour = 19;
                    activity.State.Minute = 27;
                    activity.ShowFirst.Value = nextPass == 3;
                    activity.ShowSecond.Value = nextPass != 2;
                    activity.Pass.Value = nextPass;
                });
                await WaitFor(() => activity.CompletedPass == nextPass,
                    "Shared consumers did not leave or re-enter.");
                Assert.AreSame(peer, activity.State.Jvm, "A live ancestor must retain the exact peer.");
                Assert.AreEqual(19, activity.State.Hour);
                Assert.AreEqual(27, activity.State.Minute);
                Assert.IsTrue(activity.SiblingsSharePeer);
            }

            await OnUiThread(activity, activity.Recreate);
            await WaitFor(() => SharedStateOwnershipTestActivity.Current is { } current
                && !ReferenceEquals(current, activity) && current.CompletedPass >= 0,
                "Owned-state activity did not recreate.");
            activity = SharedStateOwnershipTestActivity.Current
                ?? throw new InvalidOperationException("Recreated owned-state activity was not available.");
            Assert.AreEqual(19, activity.State.Hour, "Owner save provider did not survive consumer removal.");
            Assert.AreEqual(27, activity.State.Minute);
        }
        finally
        {
            await OnUiThread(activity, activity.Finish);
            await WaitFor(() => !ReferenceEquals(SharedStateOwnershipTestActivity.Current, activity),
                "Owned-state activity did not finish.");
        }
    }

    [TestMethod]
    [DataRow("tree")]
    [DataRow("direct")]
    public async Task ImplicitOwnerRemoval_TransfersValuesToRemainingSibling(string mode)
    {
        var activity = await StartActivity(mode);
        try
        {
            var originalPeer = activity.State.Jvm
                ?? throw new InvalidOperationException("Implicit owner was not bound.");
            await OnUiThread(activity, () =>
            {
                activity.State.Hour = 19;
                activity.State.Minute = 27;
                activity.ShowFirst.Value = false;
                activity.Pass.Value = 1;
            });
            await WaitFor(() => activity.CompletedPass == 1 && activity.State.Jvm is { } peer
                && !ReferenceEquals(peer, originalPeer),
                "Remaining consumer did not acquire a new native owner.");
            var successor = activity.State.Jvm;
            Assert.AreEqual(19, activity.State.Hour);
            Assert.AreEqual(27, activity.State.Minute);

            await OnUiThread(activity, () =>
            {
                activity.ShowFirst.Value = true;
                activity.Pass.Value = 2;
            });
            await WaitFor(() => activity.CompletedPass == 2, "Original consumer did not return.");
            Assert.AreSame(successor, activity.State.Jvm, "Returning consumer must use the surviving owner.");
            Assert.IsTrue(activity.SiblingsSharePeer);

            await OnUiThread(activity, () =>
            {
                activity.ShowFirst.Value = false;
                activity.ShowSecond.Value = false;
                activity.Pass.Value = 3;
            });
            await WaitFor(() => activity.CompletedPass == 3 && activity.State.Jvm is null,
                "Last owner did not release its binding.");
            Assert.AreEqual(19, activity.State.Hour);
            Assert.AreEqual(27, activity.State.Minute);
            await OnUiThread(activity, () =>
            {
                activity.State.Minute = 42;
                activity.ShowFirst.Value = true;
                activity.ShowSecond.Value = true;
                activity.Pass.Value = 4;
            });
            await WaitFor(() => activity.CompletedPass == 4 && activity.State.Jvm is not null,
                "Hidden shared state did not acquire a new owner.");
            Assert.AreEqual(19, activity.State.Hour);
            Assert.AreEqual(42, activity.State.Minute);
            Assert.AreNotSame(successor, activity.State.Jvm);
            Assert.IsTrue(activity.SiblingsSharePeer);
        }
        finally
        {
            await OnUiThread(activity, activity.Finish);
            await WaitFor(() => !ReferenceEquals(SharedStateOwnershipTestActivity.Current, activity),
                "Implicit-owner activity did not finish.");
        }
    }

    static async Task<SharedStateOwnershipTestActivity> StartActivity(string mode)
    {
        var context = global::Android.App.Application.Context;
        using var intent = new global::Android.Content.Intent(context, typeof(SharedStateOwnershipTestActivity));
        intent.PutExtra("mode", mode);
        intent.AddFlags(global::Android.Content.ActivityFlags.NewTask);
        context.StartActivity(intent);
        await WaitFor(() => SharedStateOwnershipTestActivity.Current is { CompletedPass: >= 0 },
            "Shared-state activity did not start.");
        return SharedStateOwnershipTestActivity.Current
            ?? throw new InvalidOperationException("Shared-state activity was not available.");
    }

    static Task OnUiThread(SharedStateOwnershipTestActivity activity, Action action)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        activity.RunOnUiThread(() =>
        {
            try
            {
                action();
                completion.SetResult();
            }
            catch (Exception ex)
            {
                completion.SetException(ex);
            }
        });
        return completion.Task;
    }

    static async Task WaitFor(Func<bool> condition, string message)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(15);
        while (DateTime.UtcNow < deadline)
        {
            if (condition())
                return;
            await Task.Delay(20);
        }
        Assert.Fail(message);
    }
}
