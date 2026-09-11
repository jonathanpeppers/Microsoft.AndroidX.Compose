namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Checks state ownership, forgetting, and isolated restart after structural edits.</summary>
[TestClass]
[DoNotParallelize]
public class CompositionIdentityTests
{
    public TestContext? TestContext { get; set; }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task HundredRowFootprint_RecordsCompositionCosts(bool ordinal)
    {
        var activity = await StartActivity(ordinal ? "footprint-ordinal" : "footprint-baseline", directContent: true);
        try
        {
            await WaitFor(() => CompositionIdentityTestActivity.ParentPasses > 0,
                "Footprint composition did not complete.");
            for (int pass = 1; pass <= 15; pass++)
                await ChangeStructure(activity, () => CompositionIdentityTestActivity.Phase.Value = pass);
            var samples = CompositionIdentityTestActivity.FootprintSamples.ToArray();
            Assert.AreEqual(16, samples.Length);
            var steady = samples.Skip(6).ToArray();
            var context = TestContext
                ?? throw new InvalidOperationException("Test context must be supplied by the test runner.");
            int owners = global::AndroidX.Compose.ComposableCallSite.Occurrences.GetOwners().Length;
            Assert.AreEqual(ordinal ? 104 : 4, owners);
            context.WriteLine(
                $"100 rendered Text rows ({(ordinal ? "ordinal" : "baseline")}): " +
                $"initial {samples[0].Bytes} managed bytes/{Milliseconds(samples[0].Ticks):F3} ms; " +
                $"steady median {steady.Select(s => s.Bytes).Order().ElementAt(5)} managed bytes/" +
                $"{Milliseconds(steady.Select(s => s.Ticks).Order().ElementAt(5)):F3} ms; " +
                $"{owners} retained occurrence peers.");
            context.WriteLine("Raw managed-bytes/composition-ms samples (initial, then 15 updates): " +
                string.Join("; ", samples.Select(s => $"{s.Bytes}/{Milliseconds(s.Ticks):F3}")));
        }
        finally
        {
            await OnUi(activity, activity.Finish);
            await WaitFor(() => global::AndroidX.Compose.ComposableCallSite.Occurrences.CompositionCount == 0,
                "Footprint composition was retained after finishing.");
        }

        static double Milliseconds(long ticks) =>
            ticks * 1000d / System.Diagnostics.Stopwatch.Frequency;
    }

    [TestMethod]
    [DataRow(0, false, false)]
    [DataRow(1, false, false)]
    [DataRow(0, true, false)]
    [DataRow(1, true, false)]
    [DataRow(0, false, true)]
    [DataRow(1, false, true)]
    [DataRow(0, true, true)]
    [DataRow(1, true, true)]
    public async Task RepeatedParents_RestoreSelectiveChildrenByOccurrence(int first, bool readd, bool nested)
    {
        var activity = await StartActivity(nested ? "selective-nested" : "selective", directContent: true);
        int[] initialIndices = nested ? [first, first + 2] : [first];
        int firstPhase = initialIndices.Sum(i => 1 << i);
        int allPhase = nested ? 15 : 3;
        int count = nested ? 4 : 2;
        try
        {
            await WaitFor(() => CompositionIdentityTestActivity.ParentPasses > 0,
                "Initial selective composition did not complete.");
            await ChangeStructure(activity, () => CompositionIdentityTestActivity.Phase.Value = firstPhase);
            var existing = initialIndices.ToDictionary(i => i, i => CompositionIdentityTestActivity.Probes[$"loop-{i}"]);
            await Seed(existing.Keys);
            await ChangeStructure(activity, () => CompositionIdentityTestActivity.Phase.Value = allPhase);
            foreach (var (index, probe) in existing)
            {
                Assert.AreSame(probe, CompositionIdentityTestActivity.Probes[$"loop-{index}"]);
                Assert.AreEqual(0, probe.Disposals);
            }
            await Seed(Enumerable.Range(0, count));

            if (readd)
            {
                var retained = Enumerable.Range(0, count).Except(initialIndices)
                    .ToDictionary(i => i, i => CompositionIdentityTestActivity.Probes[$"loop-{i}"]);
                await ChangeStructure(activity, () => CompositionIdentityTestActivity.Phase.Value = allPhase ^ firstPhase);
                foreach (var probe in existing.Values)
                    Assert.AreEqual(1, probe.Disposals);
                await ChangeStructure(activity, () => CompositionIdentityTestActivity.Phase.Value = allPhase);
                foreach (var (index, probe) in existing)
                {
                    var replacement = CompositionIdentityTestActivity.Probes[$"loop-{index}"];
                    Assert.AreNotSame(probe, replacement);
                    Assert.AreEqual(0, replacement.ObservedSaved);
                    Assert.AreEqual(1, probe.Disposals);
                }
                foreach (var (index, probe) in retained)
                {
                    Assert.AreSame(probe, CompositionIdentityTestActivity.Probes[$"loop-{index}"]);
                    Assert.AreEqual(0, probe.Disposals);
                }
                await Seed(initialIndices);
            }

            var beforeRecreate = Enumerable.Range(0, count)
                .Select(i => CompositionIdentityTestActivity.Probes[$"loop-{i}"]).ToArray();
            int pass = CompositionIdentityTestActivity.ParentPasses;
            await OnUi(activity, activity.Recreate);
            await WaitFor(() => CompositionIdentityTestActivity.Current is { } current &&
                    !ReferenceEquals(activity, current) &&
                    CompositionIdentityTestActivity.ParentPasses > pass,
                "Selective child composition did not recreate.");
            activity = CompositionIdentityTestActivity.Current
                ?? throw new InvalidOperationException("Recreated selective identity activity was unavailable.");
            for (int i = 0; i < count; i++)
            {
                var current = CompositionIdentityTestActivity.Probes[$"loop-{i}"];
                Assert.AreEqual((i + 1) * 101, current.ObservedSaved);
                Assert.AreEqual(0, current.Observed);
                Assert.AreEqual(1, current.Setups);
                Assert.AreEqual(1, beforeRecreate[i].Disposals);
            }
        }
        finally
        {
            await OnUi(activity, activity.Finish);
            await WaitFor(() => global::AndroidX.Compose.ComposableCallSite.Occurrences.CompositionCount == 0,
                "Occurrence registry retained a finished composition.");
        }

        async Task Seed(IEnumerable<int> indices)
        {
            int[] items = indices.ToArray();
            int parentPass = CompositionIdentityTestActivity.ParentPasses;
            await OnUi(activity, () =>
            {
                foreach (int i in items)
                {
                    var probe = CompositionIdentityTestActivity.Probes[$"loop-{i}"];
                    Saved(probe).Value = (i + 1) * 101;
                    probe.Ordinary.Value = (i + 1) * 501;
                }
            });
            await WaitFor(() => items.All(i =>
                    CompositionIdentityTestActivity.Probes[$"loop-{i}"].ObservedSaved == (i + 1) * 101 &&
                    CompositionIdentityTestActivity.Probes[$"loop-{i}"].Observed == (i + 1) * 501),
                "Selective child did not observe its independent values.");
            Assert.AreEqual(parentPass, CompositionIdentityTestActivity.ParentPasses,
                "Leaf-only invalidation must not rerun the parent.");
        }
    }

    [TestMethod]
    [DataRow("same")]
    [DataRow("different")]
    [DataRow("branches")]
    [DataRow("nested")]
    [DataRow("loop")]
    public async Task ConditionalCalls_RetainSurvivorsAndForgetRemovedSubtrees(string scenario)
    {
        var activity = await StartActivity(scenario, checkNodeOrder: true);
        try
        {
            await WaitFor(() => CompositionIdentityTestActivity.ParentPasses > 0,
                "Initial composition did not complete.");
            var initial = CompositionIdentityTestActivity.Probes.ToDictionary();
            await AssertNodeOrder(scenario, phase: 0, count: 3);
            int seed = 10;
            await OnUi(activity, () =>
            {
                foreach (var probe in initial.Values)
                {
                    probe.Ordinary.Value = ++seed;
                    Saved(probe).Value = 100 + seed;
                }
            });
            await WaitFor(() => initial.Values.All(p =>
                    p.Observed == p.Ordinary.Value && p.ObservedSaved == Saved(p).Value),
                "Distinct remembered values did not recompose.");
            var permanent = initial["permanent"];
            int permanentValue = permanent.Observed;
            int permanentSaved = permanent.ObservedSaved;

            await ChangeStructure(activity, () => CompositionIdentityTestActivity.Phase.Value = 1);
            await AssertNodeOrder(scenario, phase: 1, count: 3);
            Assert.AreSame(permanent, CompositionIdentityTestActivity.Probes["permanent"],
                "A later sibling inherited another call site's remembered state.");
            Assert.AreEqual(permanentValue, permanent.Observed);
            Assert.AreEqual(permanentSaved, permanent.ObservedSaved);
            foreach (var (id, probe) in initial)
            {
                bool survives = id == "permanent" || id.StartsWith("loop-", StringComparison.Ordinal);
                Assert.AreEqual(survives ? 0 : 1, Volatile.Read(ref probe.Disposals), id);
                if (survives)
                    Assert.AreSame(probe, CompositionIdentityTestActivity.Probes[id], id);
            }

            // Only the leaf reads these states: its UpdateScope must not reopen the caller envelope.
            int parentPasses = CompositionIdentityTestActivity.ParentPasses;
            await OnUi(activity, () =>
            {
                permanent.Ordinary.Value = 73;
                Saved(permanent).Value = 173;
            });
            await WaitFor(() => permanent.Observed == 73 && permanent.ObservedSaved == 173,
                "The retained leaf did not restart at its anchored group.");
            Assert.AreEqual(parentPasses, CompositionIdentityTestActivity.ParentPasses,
                "A leaf-only invalidation unexpectedly executed its parent.");
            Assert.AreEqual(1, permanent.Setups);
            Assert.AreEqual(0, permanent.Disposals);

            await ChangeStructure(activity, () => CompositionIdentityTestActivity.Phase.Value = 0);
            await AssertNodeOrder(scenario, phase: 0, count: 3);
            foreach (var (id, probe) in initial)
            {
                bool survives = id == "permanent" || id.StartsWith("loop-", StringComparison.Ordinal);
                var current = CompositionIdentityTestActivity.Probes[id];
                if (survives)
                    Assert.AreSame(probe, current, id);
                else
                {
                    Assert.AreNotSame(probe, current, id);
                    Assert.AreEqual(0, current.Observed, id);
                    Assert.AreEqual(0, current.ObservedSaved, id);
                    Assert.AreEqual(1, probe.Disposals, id);
                }
            }

            if (scenario == "nested")
            {
                var inner = CompositionIdentityTestActivity.Probes["inner-permanent"];
                var optional = CompositionIdentityTestActivity.Probes["optional"];
                await ChangeStructure(activity, () => CompositionIdentityTestActivity.Phase.Value = 2);
                await AssertNodeOrder(scenario, phase: 2, count: 3);
                Assert.AreSame(inner, CompositionIdentityTestActivity.Probes["inner-permanent"]);
                Assert.AreEqual(0, inner.Disposals);
                Assert.AreEqual(1, optional.Disposals);
            }
            if (scenario == "loop")
            {
                var first = CompositionIdentityTestActivity.Probes["loop-0"];
                var removed = CompositionIdentityTestActivity.Probes["loop-2"];
                await ChangeStructure(activity, () => CompositionIdentityTestActivity.Count.Value = 1);
                await AssertNodeOrder(scenario, phase: 0, count: 1);
                Assert.AreSame(first, CompositionIdentityTestActivity.Probes["loop-0"]);
                Assert.AreSame(permanent, CompositionIdentityTestActivity.Probes["permanent"]);
                Assert.AreEqual(1, removed.Disposals);
                await ChangeStructure(activity, () => CompositionIdentityTestActivity.Count.Value = 3);
                await AssertNodeOrder(scenario, phase: 0, count: 3);
                Assert.AreNotSame(removed, CompositionIdentityTestActivity.Probes["loop-2"]);
                Assert.AreEqual(0, CompositionIdentityTestActivity.Probes["loop-2"].ObservedSaved);
            }
        }
        finally
        {
            await OnUi(activity, activity.Finish);
        }
    }

    [TestMethod]
    [DataRow("same")]
    [DataRow("loop")]
    public async Task SaveableState_RestoresAfterPrecedingCallSitesDisappear(string scenario)
    {
        var activity = await StartActivity(scenario);
        try
        {
            await WaitFor(() => CompositionIdentityTestActivity.ParentPasses > 0,
                "Initial composition did not complete.");
            var initial = CompositionIdentityTestActivity.Probes.ToDictionary();
            await OnUi(activity, () =>
            {
                Saved(initial["permanent"]).Value = 123;
                if (scenario == "loop")
                {
                    Saved(initial["loop-0"]).Value = 201;
                    Saved(initial["loop-1"]).Value = 202;
                    Saved(initial["loop-2"]).Value = 203;
                }
            });
            await WaitFor(() => initial["permanent"].ObservedSaved == 123 &&
                (scenario != "loop" || initial["loop-2"].ObservedSaved == 203),
                "Saveable state mutations did not complete.");
            await ChangeStructure(activity, () => CompositionIdentityTestActivity.Phase.Value = 1);
            int pass = CompositionIdentityTestActivity.ParentPasses;
            await OnUi(activity, activity.Recreate);
            await WaitFor(() => CompositionIdentityTestActivity.Current is { } current &&
                    !ReferenceEquals(activity, current) &&
                    CompositionIdentityTestActivity.ParentPasses > pass,
                "Recreated composition did not complete.");
            activity = CompositionIdentityTestActivity.Current
                ?? throw new InvalidOperationException("Recreated identity activity was unavailable.");
            Assert.AreEqual(123, CompositionIdentityTestActivity.Probes["permanent"].ObservedSaved);
            if (scenario == "loop")
            {
                Assert.AreEqual(201, CompositionIdentityTestActivity.Probes["loop-0"].ObservedSaved);
                Assert.AreEqual(202, CompositionIdentityTestActivity.Probes["loop-1"].ObservedSaved);
                Assert.AreEqual(203, CompositionIdentityTestActivity.Probes["loop-2"].ObservedSaved);
            }
        }
        finally
        {
            await OnUi(activity, activity.Finish);
        }
    }

    static global::AndroidX.Compose.MutableNumberState<int> Saved(CompositionIdentityProbe probe) =>
        probe.Saved ?? throw new InvalidOperationException("Probe saveable state was not composed.");

    static async Task ChangeStructure(CompositionIdentityTestActivity activity, Action update)
    {
        int pass = CompositionIdentityTestActivity.ParentPasses;
        await OnUi(activity, update);
        await WaitFor(() => CompositionIdentityTestActivity.ParentPasses > pass,
            "Structural recomposition did not complete.");
    }

    static async Task AssertNodeOrder(string scenario, int phase, int count)
    {
        List<string> ids = [];
        if (scenario == "loop")
        {
            for (int i = 0; i < count; i++)
            {
                if (phase != 1) ids.Add($"optional-{i}");
                ids.Add($"loop-{i}");
            }
        }
        else if (scenario == "branches")
            ids.Add(phase == 0 ? "optional" : "alternative");
        else if (phase != 1)
        {
            if (phase != 2) ids.Add("optional");
            if (scenario == "nested") ids.Add("inner-permanent");
        }
        ids.Add("permanent");
        if (phase != 1) ids.Add("trailing");
        int[] expected = ids.Select(CompositionIdentityTestActivity.NodeCode).ToArray();
        await WaitFor(() => Volatile.Read(ref CompositionIdentityTestActivity.NodeOrder)
                .SequenceEqual(expected),
            $"Applier node order does not match {string.Join(", ", ids)}.");
    }

    static async Task<CompositionIdentityTestActivity> StartActivity(
        string scenario, bool checkNodeOrder = false, bool directContent = false)
    {
        var context = global::Android.App.Application.Context;
        CompositionIdentityTestActivity.Reset(scenario, checkNodeOrder, directContent);
        using var intent = new global::Android.Content.Intent(context, typeof(CompositionIdentityTestActivity));
        intent.AddFlags(global::Android.Content.ActivityFlags.NewTask);
        context.StartActivity(intent);
        await WaitFor(() => CompositionIdentityTestActivity.Current is not null,
            "Identity test activity did not start.");
        return CompositionIdentityTestActivity.Current
            ?? throw new InvalidOperationException("Identity test activity was unavailable.");
    }

    static Task OnUi(CompositionIdentityTestActivity activity, Action action)
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

    static async Task WaitFor(Func<bool> predicate, string message)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(10);
        while (!predicate())
        {
            if (DateTime.UtcNow >= deadline)
                Assert.Fail(message);
            await Task.Delay(20);
        }
    }
}
