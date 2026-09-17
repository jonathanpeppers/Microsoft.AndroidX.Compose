using AndroidX.Compose;
using AndroidX.Compose.Runtime;
using Composable = AndroidX.Compose.ComposableAttribute;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Exercises occurrence ownership before apply, on abandonment, and without an ambient frame or save registry.</summary>
[TestClass]
[DoNotParallelize]
public class CompositionIdentityControlledTests
{
    static WeakReference? s_disposalPayload;

    [TestMethod]
    public void ThrowingChildCleanup_DoesNotPermanentlyRootRemovedComposition()
    {
        var references = RemoveThrowingSubtree();
        for (int i = 0; i < 20; i++)
        {
            CollectPeers();
            Console.WriteLine($"GC round {i + 1}: " +
                string.Join(", ", references.Select(r => $"{r.Name}={r.Reference.IsAlive}")) +
                $", compositions={ComposableCallSite.Occurrences.CompositionCount}");
            if (references.All(r => !r.Reference.IsAlive) &&
                ComposableCallSite.Occurrences.CompositionCount == 0)
                break;
        }
        Assert.IsTrue(references.All(r => !r.Reference.IsAlive),
            "Skipped observer callbacks must not permanently retain the removed composition, pools, occurrence, or payload.");
        Assert.AreEqual(0, ComposableCallSite.Occurrences.CompositionCount);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    static (string Name, WeakReference Reference)[] RemoveThrowingSubtree()
    {
        using var applier = new IdentityTestApplier();
        using var recomposer = new Recomposer(Kotlin.Coroutines.EmptyCoroutineContext.Instance
            ?? throw new InvalidOperationException("Empty coroutine context is unavailable."));
        var composition = CompositionKt.ControlledComposition(applier, recomposer);
        try
        {
            composition.ComposeContent(new ComposableLambda2(c => ThrowingCleanup(c)));
            composition.ApplyChanges();
            var owners = ObserveOwners();
            Assert.AreEqual(1, owners.Length);
            var roots = ObserveRegistryRoots(composition);
            composition.ComposeContent(new ComposableLambda2(_ => { }));
            var error = Assert.ThrowsExactly<Java.Lang.IllegalStateException>(composition.ApplyChanges);
            StringAssert.Contains(error.Message, "Expected disposal failure.");
            Assert.AreEqual(1, ComposableCallSite.Occurrences.GetOwners().Length,
                "Native dispatch should have stopped before the enclosing occurrence's cleanup.");
            composition.Dispose();
            Assert.IsTrue(composition.IsDisposed);
            Assert.AreEqual(1, ComposableCallSite.Occurrences.GetOwners().Length,
                "Disposing cannot recover a committed occurrence whose slot was already removed.");
            return [("composition", new WeakReference(composition, trackResurrection: true)),
                .. roots, ("occurrence", owners[0]),
                ("payload", s_disposalPayload ?? throw new InvalidOperationException("Disposal payload was not created."))];
        }
        finally
        {
            if (!composition.IsDisposed)
                composition.Dispose();
        }
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    static (string Name, WeakReference Reference)[] ObserveRegistryRoots(IControlledComposition composition)
    {
        var field = typeof(CompositionOccurrenceRegistry<IControlledComposition>).GetField(
            "_compositions", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Occurrence composition table is unavailable.");
        var table = field.GetValue(ComposableCallSite.Occurrences) as
            IEnumerable<KeyValuePair<IControlledComposition, Dictionary<(long Parent, string Site), CompositionOccurrencePool>>>
            ?? throw new InvalidOperationException("Occurrence composition table cannot be observed.");
        List<(string Name, WeakReference Reference)> roots = [];
        foreach (var entry in table)
        {
            Assert.AreSame(composition, entry.Key,
                "The native composer's registry key must be the original managed composition peer.");
            roots.Add(("registry-key", new WeakReference(entry.Key, trackResurrection: true)));
            roots.Add(("site-map", new WeakReference(entry.Value, trackResurrection: true)));
            foreach (var pool in entry.Value.Values)
                roots.Add(("pool", new WeakReference(pool, trackResurrection: true)));
        }
        return roots.ToArray();
    }

    [Composable]
    internal static void ThrowingCleanup(IComposer composer)
    {
        var payload = composer.Remember(static () => new object());
        s_disposalPayload = new WeakReference(payload, trackResurrection: true);
        composer.DisposableEffect(0, () => () =>
        {
            GC.KeepAlive(payload);
            throw new Java.Lang.IllegalStateException("Expected disposal failure.");
        });
    }

    [TestMethod]
    public void FailedSlotPublication_ReleasesTheUninstalledOwner()
    {
        using var applier = new IdentityTestApplier();
        using var recomposer = new Recomposer(Kotlin.Coroutines.EmptyCoroutineContext.Instance
            ?? throw new InvalidOperationException("Empty coroutine context is unavailable."));
        var composition = CompositionKt.ControlledComposition(applier, recomposer);
        using var failure = new FailingIdentityComposer { Composition = composition };
        try
        {
            using var identity = new Java.Lang.String("failed-publication");
            var error = Assert.ThrowsExactly<InvalidOperationException>(
                () => ComposableCallSite.Start(failure, 350, identity));
            Assert.AreEqual("Injected occurrence publication failure.", error.Message);
            Assert.AreEqual(1, failure.OwnersAtFailure);
            Assert.AreEqual(0, ComposableCallSite.Occurrences.CompositionCount);
            var owner = failure.AttemptedOwner
                ?? throw new InvalidOperationException("Publication was not attempted.");
            for (int i = 0; i < 10 && owner.IsAlive; i++)
                CollectPeers();
            Assert.IsFalse(owner.IsAlive);
        }
        finally
        {
            composition.Dispose();
        }
    }

    [TestMethod]
    public void SeparateCompositions_UseSeparateOrdinalPools()
    {
        using var applier = new IdentityTestApplier();
        using var secondApplier = new IdentityTestApplier();
        using var recomposer = new Recomposer(Kotlin.Coroutines.EmptyCoroutineContext.Instance
            ?? throw new InvalidOperationException("Empty coroutine context is unavailable."));
        var first = CompositionKt.ControlledComposition(applier, recomposer);
        var second = CompositionKt.ControlledComposition(secondApplier, recomposer);
        var probes = new List<CompositionIdentityProbe>();
        var keys = new List<long>();
        var content = new ComposableLambda2(c =>
        {
            for (int i = 0; i < 2; i++)
                Probe(c, i, probes, keys);
        });
        try
        {
            first.ComposeContent(content);
            first.ApplyChanges();
            var firstKeys = keys.ToArray();
            probes.Clear();
            keys.Clear();
            second.ComposeContent(content);
            second.ApplyChanges();
            CollectionAssert.AreEqual(firstKeys, keys.ToArray());
            Assert.AreEqual(2, ComposableCallSite.Occurrences.CompositionCount);
            first.Dispose();
            Assert.AreEqual(1, ComposableCallSite.Occurrences.CompositionCount);
            Assert.IsTrue(probes.All(p => p.Disposals == 0));
        }
        finally
        {
            if (!first.IsDisposed)
                first.Dispose();
            second.Dispose();
        }
        Assert.AreEqual(0, ComposableCallSite.Occurrences.CompositionCount);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void OccurrencePeers_SurviveGcUntilForgottenOrAbandoned(bool abandon)
    {
        using var applier = new IdentityTestApplier();
        using var recomposer = new Recomposer(Kotlin.Coroutines.EmptyCoroutineContext.Instance
            ?? throw new InvalidOperationException("Empty coroutine context is unavailable."));
        var composition = CompositionKt.ControlledComposition(applier, recomposer);
        var probes = new List<CompositionIdentityProbe>();
        var keys = new List<long>();
        WeakReference[] owners = [];
        try
        {
            composition.ComposeContent(new ComposableLambda2(c =>
            {
                for (int i = 0; i < 10; i++)
                    Probe(c, i, probes, keys);
            }));
            owners = ObserveOwners();
            Assert.AreEqual(10, owners.Length);
            CollectPeers();
            Assert.IsTrue(owners.All(o => o.IsAlive));
            if (abandon)
            {
                composition.AbandonChanges();
            }
            else
            {
                composition.ApplyChanges();
                CollectPeers();
                Assert.IsTrue(owners.All(o => o.IsAlive));
                composition.ComposeContent(new ComposableLambda2(_ => { }));
                Assert.IsTrue(owners.All(o => o.IsAlive));
                composition.ApplyChanges();
            }
            Assert.AreEqual(0, ComposableCallSite.Occurrences.CompositionCount);
        }
        finally
        {
            composition.Dispose();
        }
        for (int i = 0; i < 10 && owners.Any(o => o.IsAlive); i++)
            CollectPeers();
        Assert.IsTrue(owners.All(o => !o.IsAlive),
            "Released ordinal ownership must not root a forgotten or abandoned peer.");
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    static WeakReference[] ObserveOwners() =>
        ComposableCallSite.Occurrences.GetOwners().Select(owner => new WeakReference(owner, trackResurrection: true)).ToArray();

    static void CollectPeers()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        Java.Lang.JavaSystem.Gc();
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

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
        var composition = composer.Composition;
        Assert.AreSame(composition, composer.Composition,
            "Repeated projections must preserve the managed key while its pool is live.");
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
