using AndroidX.Compose;
using AndroidX.Compose.Runtime;
using Composable = AndroidX.Compose.ComposableAttribute;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Exercises occurrence ownership before apply, on abandonment, and without an ambient frame or save registry.</summary>
[TestClass]
[DoNotParallelize]
public class CompositionIdentityControlledTests
{
    [TestMethod]
    public void DelayedForgetAndAbandon_ReconstructTheSameOccurrenceAncestry()
    {
        using var applier = new IdentityTestApplier();
        using var recomposer = new Recomposer(Kotlin.Coroutines.EmptyCoroutineContext.Instance
            ?? throw new InvalidOperationException("Empty coroutine context is unavailable."));
        var composition = CompositionKt.ControlledComposition(applier, recomposer);
        var probes = new List<CompositionIdentityProbe>();
        var keys = new List<long>();
        try
        {
            Compose(3);
            var initial = probes.ToArray();
            var initialKeys = keys.ToArray();
            Assert.IsTrue(composition.HasPendingChanges);
            Assert.IsTrue(initial.All(p => p.Setups == 0));
            composition.ApplyChanges();
            Assert.IsTrue(initial.All(p => p.Setups == 1 && p.Disposals == 0));
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Compose(1);
            Assert.AreSame(initial[0], probes[0]);
            Assert.IsTrue(initial.All(p => p.Disposals == 0),
                "Forgetting must be deferred until apply, not the end of a managed invocation.");
            composition.ApplyChanges();
            Assert.AreEqual(0, initial[0].Disposals);
            Assert.AreEqual(1, initial[1].Disposals);
            Assert.AreEqual(1, initial[2].Disposals);

            Compose(3);
            CollectionAssert.AreEqual(initialKeys, keys.ToArray());
            var abandoned = probes.Skip(1).ToArray();
            Assert.IsTrue(abandoned.All(p => p.Setups == 0));
            composition.AbandonChanges();
            Assert.IsTrue(abandoned.All(p => p.Setups == 0 && p.Disposals == 0));
            Compose(3);
            CollectionAssert.AreEqual(initialKeys, keys.ToArray(),
                "Abandoned insertions must release their occurrence ordinals.");
            Assert.AreNotSame(abandoned[0], probes[1]);
            composition.ApplyChanges();

            Compose(0);
            composition.ApplyChanges();
            Assert.AreEqual(0, ComposableCallSite.Occurrences.CompositionCount);
            Compose(3);
            CollectionAssert.AreEqual(initialKeys, keys.ToArray(),
                "Re-entering after forgetting all occurrences must start at ordinal zero.");
            composition.ApplyChanges();
            composition.VerifyConsistent();
        }
        finally
        {
            composition.Dispose();
        }
        Assert.AreEqual(0, ComposableCallSite.Occurrences.CompositionCount);

        void Compose(int count)
        {
            probes.Clear();
            keys.Clear();
            composition.ComposeContent(new ComposableLambda2(c =>
            {
                for (int i = 0; i < count; i++)
                    Probe(c, i, probes, keys);
            }));
        }
    }

    [TestMethod]
    public void FailedInitialComposition_ReleasesOrdinalsBeforeRetry()
    {
        using var applier = new IdentityTestApplier();
        using var recomposer = new Recomposer(Kotlin.Coroutines.EmptyCoroutineContext.Instance
            ?? throw new InvalidOperationException("Empty coroutine context is unavailable."));
        var composition = CompositionKt.ControlledComposition(applier, recomposer);
        var probes = new List<CompositionIdentityProbe>();
        var keys = new List<long>();
        bool fail = true;
        var content = new ComposableLambda2(c =>
        {
            for (int i = 0; i < 2; i++)
                Probe(c, i, probes, keys);
            if (fail)
                throw new Java.Lang.IllegalStateException("Expected identity test failure.");
        });
        try
        {
            Assert.ThrowsExactly<Java.Lang.IllegalStateException>(() => composition.ComposeContent(content));
            var failedKeys = keys.ToArray();
            Assert.AreEqual(0, ComposableCallSite.Occurrences.CompositionCount);
            Assert.IsTrue(probes.All(p => p.Setups == 0 && p.Disposals == 0));
            probes.Clear();
            keys.Clear();
            fail = false;
            composition.ComposeContent(content);
            CollectionAssert.AreEqual(failedKeys, keys.ToArray());
            composition.ApplyChanges();
            composition.VerifyConsistent();
        }
        finally
        {
            composition.Dispose();
        }
        Assert.AreEqual(0, ComposableCallSite.Occurrences.CompositionCount);
    }

    /// <summary>Records native compound keys, ordinary slots, and exact committed effect lifetime.</summary>
    [Composable]
    internal static void Probe(IComposer composer, int index, List<CompositionIdentityProbe> probes, List<long> keys)
    {
        var probe = composer.Remember(() => new CompositionIdentityProbe());
        var saved = composer.RememberSaveable(() => new MutableNumberState<int>(index));
        Assert.AreEqual(index, saved.Value, "Missing saveable registry must remain supported.");
        composer.DisposableEffect(0, () =>
        {
            probe.Setups++;
            return () => probe.Disposals++;
        });
        probes.Add(probe);
        keys.Add(composer.CompositeKeyHashCode);
    }
}
