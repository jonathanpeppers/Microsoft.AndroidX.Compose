namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Checks record identity during collection mutations.</summary>
[TestClass]
[DoNotParallelize]
public class CollectionKeysTests
{
    [TestMethod]
    [DataRow(0, 0)] [DataRow(0, 1)] [DataRow(0, 2)]
    [DataRow(1, 0)] [DataRow(1, 1)] [DataRow(1, 2)]
    [DataRow(2, 0)] [DataRow(2, 1)] [DataRow(2, 2)]
    [DataRow(3, 0)] [DataRow(3, 1)] [DataRow(3, 2)]
    [DataRow(4, 0)] [DataRow(4, 1)] [DataRow(4, 2)]
    [DataRow(5, 0)] [DataRow(5, 1)] [DataRow(5, 2)]
    [DataRow(6, 0)] [DataRow(6, 1)] [DataRow(6, 2)]
    [DataRow(7, 0)] [DataRow(7, 1)] [DataRow(7, 2)]
    public async Task StableKeys_RetainStateAndViewportOnInsertDeleteReorder(int surface, int style)
    {
        await ExerciseMutations(surface, style, keyed: true);
    }

    [TestMethod]
    [DataRow(0, 0)] [DataRow(0, 1)] [DataRow(0, 2)]
    [DataRow(1, 0)] [DataRow(1, 1)] [DataRow(1, 2)]
    [DataRow(2, 0)] [DataRow(2, 1)] [DataRow(2, 2)]
    [DataRow(3, 0)] [DataRow(3, 1)] [DataRow(3, 2)]
    [DataRow(4, 0)] [DataRow(4, 1)] [DataRow(4, 2)]
    [DataRow(5, 0)] [DataRow(5, 1)] [DataRow(5, 2)]
    [DataRow(6, 0)] [DataRow(6, 1)] [DataRow(6, 2)]
    [DataRow(7, 0)] [DataRow(7, 1)] [DataRow(7, 2)]
    public async Task NullKeys_PreservePositionalStateAndViewport(int surface, int style)
    {
        await ExerciseMutations(surface, style, keyed: false);
    }

    [TestMethod]
    [DataRow(0)] [DataRow(1)] [DataRow(2)] [DataRow(3)]
    [DataRow(4)] [DataRow(5)] [DataRow(6)] [DataRow(7)]
    public async Task OriginalSignatures_PreservePositionalStateAndViewport(int surface)
    {
        await ExerciseMutations(surface, style: 3, keyed: false);
    }

    [TestMethod]
    [DataRow(6)] [DataRow(7)]
    public async Task ConditionalPager_ReappearsAndKeepsLiveCountAcrossKeyTransitions(int surface)
    {
        CollectionKeysTestActivity.Reset(surface, style: 0, keyed: true, conditionalPager: true);
        var context = global::Android.App.Application.Context;
        using var intent = new global::Android.Content.Intent(context, typeof(CollectionKeysTestActivity));
        intent.AddFlags(global::Android.Content.ActivityFlags.NewTask);
        context.StartActivity(intent);
        await WaitFor(() => CollectionKeysTestActivity.Current is not null, "Activity did not start.");
        var activity = CollectionKeysTestActivity.Current
            ?? throw new InvalidOperationException("Collection test activity did not start.");
        try
        {
            await WaitFor(() => CollectionKeysTestActivity.Observed.ContainsKey(21), "Initial page did not render.");
            // Publishing an empty keyed snapshot is legal even if the next parent pass removes the pager.
            activity.RunOnUiThread(() =>
            {
                CollectionKeysTestActivity.Mutate([]);
                CollectionKeysTestActivity.PageState.SetRenderedPageCount(0);
            });
            await WaitFor(() => CollectionKeysTestActivity.EmptyGeneration == 1, "Pager did not leave composition.");
            Assert.AreEqual(0, CollectionKeysTestActivity.PageState.PageCount);

            bool[] keyModes = [true, false, true];
            foreach (bool keyed in keyModes)
            {
                int generation = CollectionKeysTestActivity.Generation + 1;
                activity.RunOnUiThread(() => CollectionKeysTestActivity.Mutate([101]));
                await WaitFor(() => CollectionKeysTestActivity.Observed.GetValueOrDefault(101).Generation == generation,
                    "Pager did not return after adding a record.");
                activity.RunOnUiThread(() => CollectionKeysTestActivity.SetKeyed(keyed));
                await WaitFor(() => CollectionKeysTestActivity.Observed.GetValueOrDefault(101).Generation == generation + 1,
                    "Pager did not render after changing key mode.");
                Assert.AreEqual(1, CollectionKeysTestActivity.PageState.PageCount);
                Assert.AreEqual(1, CollectionKeysTestActivity.PageState.Jvm.PageCount);
                activity.RunOnUiThread(() => CollectionKeysTestActivity.Mutate([]));
                await WaitFor(() => CollectionKeysTestActivity.EmptyGeneration == generation + 2,
                    "Pager did not return to empty content.");
                Assert.AreEqual(0, CollectionKeysTestActivity.PageState.PageCount);
            }
        }
        finally
        {
            activity.RunOnUiThread(activity.Finish);
            await WaitFor(() => CollectionKeysTestActivity.Current is null, "Activity did not finish.");
        }
    }

    [TestMethod]
    [DataRow(0)] [DataRow(1)] [DataRow(2)] [DataRow(3)]
    [DataRow(4)] [DataRow(5)] [DataRow(6)] [DataRow(7)]
    public async Task SaveableState_FollowsKeyAfterScrollingAwayAndInserting(int surface)
    {
        CollectionKeysTestActivity.Reset(surface, style: 0, keyed: true);
        var context = global::Android.App.Application.Context;
        using var intent = new global::Android.Content.Intent(context, typeof(CollectionKeysTestActivity));
        intent.AddFlags(global::Android.Content.ActivityFlags.NewTask);
        context.StartActivity(intent);
        await WaitFor(() => CollectionKeysTestActivity.Current is not null, "Activity did not start.");
        var activity = CollectionKeysTestActivity.Current
            ?? throw new InvalidOperationException("Collection test activity did not start.");
        try
        {
            await WaitFor(() => CollectionKeysTestActivity.RowStates.ContainsKey(21),
                "Initial record did not render.");
            activity.RunOnUiThread(() => CollectionKeysTestActivity.RowStates[21].Saved.Value = 84);
            await WaitFor(() => CollectionKeysTestActivity.Observed.GetValueOrDefault(21).Saved == 84,
                "Edited saveable state did not render.");
            await ScrollOnUiThread(activity, 40);
            await WaitFor(() => CollectionKeysTestActivity.FirstIndex == 40
                && CollectionKeysTestActivity.RowStates.ContainsKey(41), "Distant record did not render.");
            activity.RunOnUiThread(() => CollectionKeysTestActivity.Mutate([0, .. Enumerable.Range(1, 50)]));
            await WaitFor(() => CollectionKeysTestActivity.FirstIndex == 41,
                "Distant viewport did not follow its key.");
            await ScrollOnUiThread(activity, 21);
            await WaitFor(() => CollectionKeysTestActivity.FirstIndex == 21
                && CollectionKeysTestActivity.Observed.GetValueOrDefault(21).Generation == 1,
                "Original record did not return to the viewport.");
            Assert.AreEqual(84, CollectionKeysTestActivity.Observed[21].Saved,
                "Saveable state must return with record 21, not its former position.");
        }
        finally
        {
            activity.RunOnUiThread(activity.Finish);
            await WaitFor(() => CollectionKeysTestActivity.Current is null, "Activity did not finish.");
        }
    }

    static async Task ExerciseMutations(int surface, int style, bool keyed)
    {
        CollectionKeysTestActivity.Reset(surface, style, keyed);
        var context = global::Android.App.Application.Context;
        using var intent = new global::Android.Content.Intent(context, typeof(CollectionKeysTestActivity));
        intent.AddFlags(global::Android.Content.ActivityFlags.NewTask);
        context.StartActivity(intent);
        await WaitFor(() => CollectionKeysTestActivity.Current is not null, "Activity did not start.");
        var activity = CollectionKeysTestActivity.Current
            ?? throw new InvalidOperationException("Collection test activity did not start.");
        try
        {
            await WaitFor(() => CollectionKeysTestActivity.RowStates.ContainsKey(21)
                && CollectionKeysTestActivity.FirstIndex == 20, "Initial rows did not render.");
            var original = CollectionKeysTestActivity.RowStates[21];
            activity.RunOnUiThread(() =>
            {
                original.Local.Value = 42;
                original.Saved.Value = 84;
            });
            await WaitFor(() => CollectionKeysTestActivity.Observed.GetValueOrDefault(21) == (0, 42, 84),
                "Edited record state did not render.");

            List<int> items = [0, .. Enumerable.Range(1, 50)];
            await MutateAndCheck([.. items], keyed ? 21 : 20, "insertion");
            items.Remove(1);
            await MutateAndCheck([.. items], 20, "deletion");
            (items[19], items[20]) = (items[20], items[19]);
            await MutateAndCheck([.. items], keyed ? 19 : 20, "reorder");

            async Task MutateAndCheck(IReadOnlyList<int> updated, int expectedIndex, string operation)
            {
                int expectedItem = updated[expectedIndex];
                int generation = CollectionKeysTestActivity.Generation + 1;
                activity.RunOnUiThread(() => CollectionKeysTestActivity.Mutate(updated));
                await WaitFor(() => CollectionKeysTestActivity.FirstIndex == expectedIndex
                    && CollectionKeysTestActivity.Observed.TryGetValue(expectedItem, out var observed)
                    && observed.Generation == generation, $"{operation}: viewport or record did not update.");
                var observed = CollectionKeysTestActivity.Observed[expectedItem];
                Assert.AreEqual(42, observed.Local, $"{operation}: local state changed owner.");
                Assert.AreEqual(84, observed.Saved, $"{operation}: saveable state changed owner.");
                Assert.AreSame(original.Local, CollectionKeysTestActivity.RowStates[expectedItem].Local);
                if (keyed)
                    Assert.AreEqual(21, expectedItem, $"{operation}: viewport lost record 21.");
                else
                    Assert.AreEqual(20, CollectionKeysTestActivity.FirstIndex, "Default key must remain positional.");
            }
        }
        finally
        {
            activity.RunOnUiThread(activity.Finish);
            await WaitFor(() => CollectionKeysTestActivity.Current is null, "Activity did not finish.");
        }
    }

    static async Task ScrollOnUiThread(CollectionKeysTestActivity activity, int index)
    {
        var completion = new TaskCompletionSource<Task>(TaskCreationOptions.RunContinuationsAsynchronously);
        activity.RunOnUiThread(() =>
        {
            try
            {
                completion.SetResult(CollectionKeysTestActivity.ScrollToAsync(index));
            }
            catch (Exception ex)
            {
                completion.SetException(ex);
            }
        });
        await await completion.Task;
    }

    internal static async Task WaitFor(Func<bool> predicate, string message)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(10);
        while (DateTime.UtcNow < deadline)
        {
            if (predicate())
                return;
            await Task.Delay(20);
        }
        Assert.Fail($"{message} FirstIndex={CollectionKeysTestActivity.FirstIndex}; " +
            $"Generation={CollectionKeysTestActivity.Generation}; " +
            $"Observed={string.Join(", ", CollectionKeysTestActivity.Observed.OrderBy(entry => entry.Key))}");
    }
}
