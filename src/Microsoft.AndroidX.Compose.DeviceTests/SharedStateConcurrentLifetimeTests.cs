using System.Runtime.ExceptionServices;
using AndroidX.Compose;
using AndroidX.Compose.Runtime;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Checks transient native monitor dependencies for concurrent shared-state consumers.</summary>
[TestClass]
[DoNotParallelize]
public class SharedStateConcurrentLifetimeTests
{
    [TestMethod]
    [DataRow(2)]
    [DataRow(3)]
    public void ConcurrentCrossedBorrowers_FailFastAndRetrySequentially(int count)
    {
        var appliers = Enumerable.Range(0, count).Select(_ => new StateOnlyApplier()).ToArray();
        var recomposer = new Recomposer(Kotlin.Coroutines.EmptyCoroutineContext.Instance
            ?? throw new InvalidOperationException("Empty coroutine context was unavailable."));
        var compositions = appliers.Select(applier => CompositionKt.ControlledComposition(applier, recomposer)
            ?? throw new InvalidOperationException("Controlled composition was unavailable.")).ToArray();
        var states = Enumerable.Range(0, count).Select(i => new TimePickerState(7 + i, 10)).ToArray();
        var blockers = Enumerable.Range(0, count)
            .Select(_ => new ThrowingAbandonObserver { ThrowOnAbandoned = false }).ToArray();
        var barrier = new Barrier(count);
        bool borrow = false;
        bool concurrent = false;
        int reachedBorrow = 0;
        var failures = new ExceptionDispatchInfo?[count];
        var contents = Enumerable.Range(0, count).Select(Content).ToArray();
        bool finished = true;
        try
        {
            Assert.AreEqual(0, ComposeBridges.SharedStateDependencyCount());
            for (int i = 0; i < count; i++)
            {
                compositions[i].ComposeContent(contents[i]);
                Apply(compositions[i]);
            }
            var peers = states.Select(state => state.Jvm).ToArray();
            borrow = concurrent = true;
            var threads = Enumerable.Range(0, count).Select(i =>
                new Thread(() => Run(compositions[i], contents[i], ref failures[i])) { IsBackground = true }).ToArray();
            foreach (var thread in threads)
                thread.Start();
            finished = JoinAll(threads);
            Assert.AreEqual(count, Volatile.Read(ref reachedBorrow),
                "Guard: all native composition bodies must reach crossed borrowing concurrently.");
            Assert.IsTrue(finished, "Crossed borrowers deadlocked while holding their native composition monitors.");
            Assert.AreEqual(0, ComposeBridges.SharedStateDependencyCount(), "Cycle exit left a graph edge rooted.");
            Assert.IsTrue(failures.Any(failure => failure is not null), "A real cycle must be reported, not ignored.");
            for (int i = 0; i < count; i++)
            {
                if (failures[i] is { } failure)
                {
                    Assert.IsInstanceOfType<Java.Lang.IllegalStateException>(failure.SourceException);
                    StringAssert.Contains(failure.SourceException.Message, "Shared state ownership cycle detected");
                    Assert.AreEqual(1, blockers[i].AbandonedCalls, "Native failure must abandon speculative observers.");
                    Assert.IsFalse(compositions[i].HasPendingChanges);
                }
                else
                    Apply(compositions[i]);
            }
            concurrent = false;
            for (int i = 0; i < count; i++)
            {
                compositions[i].ComposeContent(contents[i]);
                Apply(compositions[i]);
                Assert.AreSame(peers[i], states[i].Jvm, "Sequential retry replaced an existing owning peer.");
            }
            Assert.AreEqual(0, ComposeBridges.SharedStateDependencyCount());
        }
        finally
        {
            // Only a failing regression leaves workers here; the bounded device
            // runner stops this test process rather than waiting on deadlocked disposal.
            if (finished)
            {
                foreach (var composition in compositions)
                    composition.Dispose();
                recomposer.Cancel();
                recomposer.Dispose();
                foreach (var content in contents)
                    content.Dispose();
                foreach (var blocker in blockers)
                    blocker.Dispose();
                foreach (var applier in appliers)
                    applier.Dispose();
                barrier.Dispose();
            }
        }

        ComposableLambda2 Content(int index) => new(composer =>
        {
            composer.RememberTimePickerState(states[index]);
            if (borrow)
            {
                composer.StartReplaceableGroup(354821);
                if (concurrent)
                    composer.UpdateRememberedValue(blockers[index]);
                composer.EndReplaceableGroup();
                if (concurrent)
                {
                    Assert.IsTrue(barrier.SignalAndWait(TimeSpan.FromSeconds(5)),
                        "Native composition bodies did not execute concurrently.");
                    Interlocked.Increment(ref reachedBorrow);
                }
                composer.RememberTimePickerState(states[(index + 1) % count]);
            }
        });
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void AcyclicContention_WaitsAndPreservesOriginalErrors(bool throwAfterBorrow)
    {
        var ownerApplier = new StateOnlyApplier();
        var consumerApplier = new StateOnlyApplier();
        var recomposer = new Recomposer(Kotlin.Coroutines.EmptyCoroutineContext.Instance
            ?? throw new InvalidOperationException("Empty coroutine context was unavailable."));
        var owner = CompositionKt.ControlledComposition(ownerApplier, recomposer)
            ?? throw new InvalidOperationException("Owner composition was unavailable.");
        var consumer = CompositionKt.ControlledComposition(consumerApplier, recomposer)
            ?? throw new InvalidOperationException("Consumer composition was unavailable.");
        var state = new TimePickerState(7, 10);
        var entered = new ManualResetEventSlim();
        var release = new ManualResetEventSlim();
        bool contended = false;
        var expected = new InvalidOperationException("Expected content failure after shared-state borrowing.");
        var blocker = new ThrowingAbandonObserver { ThrowOnAbandoned = false };
        var ownerContent = new ComposableLambda2(composer =>
        {
            composer.RememberTimePickerState(state);
            if (contended)
            {
                entered.Set();
                Assert.IsTrue(release.Wait(TimeSpan.FromSeconds(10)), "The test did not release the owning monitor.");
            }
        });
        var consumerContent = new ComposableLambda2(composer =>
        {
            Assert.IsTrue(entered.Wait(TimeSpan.FromSeconds(5)), "Owner did not enter its native monitor.");
            composer.StartReplaceableGroup(354822);
            composer.UpdateRememberedValue(blocker);
            composer.EndReplaceableGroup();
            composer.RememberTimePickerState(state);
            if (throwAfterBorrow)
                throw expected;
        });
        ExceptionDispatchInfo? ownerFailure = null;
        ExceptionDispatchInfo? consumerFailure = null;
        bool finished = true;
        try
        {
            owner.ComposeContent(ownerContent);
            Apply(owner);
            var peer = state.Jvm;
            contended = true;
            var first = new Thread(() => Run(owner, ownerContent, ref ownerFailure)) { IsBackground = true };
            var second = new Thread(() => Run(consumer, consumerContent, ref consumerFailure)) { IsBackground = true };
            first.Start();
            second.Start();
            bool observedEdge;
            try
            {
                Assert.IsTrue(entered.Wait(TimeSpan.FromSeconds(5)));
                observedEdge = SpinWait.SpinUntil(() => ComposeBridges.SharedStateDependencyCount() == 1,
                    TimeSpan.FromSeconds(5));
            }
            finally
            {
                release.Set();
                finished = JoinAll([first, second]);
            }
            Assert.IsTrue(finished, "Acyclic contention did not complete.");
            Assert.IsTrue(observedEdge, "Guard: the consumer must actually wait on the held foreign monitor.");
            ownerFailure?.Throw();
            Assert.AreEqual(0, ComposeBridges.SharedStateDependencyCount(), "Return/throw left a graph edge rooted.");
            if (throwAfterBorrow)
            {
                Assert.AreSame(expected, consumerFailure?.SourceException, "The original content exception was replaced.");
                Assert.AreEqual(1, blocker.AbandonedCalls);
                Assert.IsFalse(consumer.HasPendingChanges);
            }
            else
            {
                consumerFailure?.Throw();
                Apply(consumer);
            }
            Apply(owner);
            Assert.AreSame(peer, state.Jvm, "Contention changed the owning peer.");
        }
        finally
        {
            if (finished)
            {
                consumer.Dispose();
                owner.Dispose();
                recomposer.Cancel();
                recomposer.Dispose();
                ownerContent.Dispose();
                consumerContent.Dispose();
                blocker.Dispose();
                ownerApplier.Dispose();
                consumerApplier.Dispose();
                entered.Dispose();
                release.Dispose();
            }
        }
    }

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public void NestedConcurrentBorrowing_DetectsOuterMonitorInEitherOrder(bool firstOwnerInsideNested, bool nestedWaitsFirst)
    {
        var appliers = Enumerable.Range(0, 3).Select(_ => new StateOnlyApplier()).ToArray();
        var recomposer = new Recomposer(Kotlin.Coroutines.EmptyCoroutineContext.Instance
            ?? throw new InvalidOperationException("Empty coroutine context was unavailable."));
        var compositions = appliers.Select(applier => CompositionKt.ControlledComposition(applier, recomposer)
            ?? throw new InvalidOperationException("Controlled composition was unavailable.")).ToArray();
        var stateA = new TimePickerState(7, 10);
        var stateC = new TimePickerState(8, 20);
        var barrier = new Barrier(2);
        bool nested = false;
        bool concurrent = false;
        IComposer? outerComposer = null;
        using var contentB = new ComposableLambda2(composer =>
        {
            if (firstOwnerInsideNested)
            {
                // A had no shared token when B was entered. Publishing A's first
                // borrowable owner must observe its monitor before either wait.
                (outerComposer ?? throw new InvalidOperationException("Outer composer unavailable."))
                    .RememberTimePickerState(stateA);
            }
            if (concurrent)
            {
                Assert.IsTrue(barrier.SignalAndWait(TimeSpan.FromSeconds(5)));
                if (!nestedWaitsFirst)
                    Assert.IsTrue(SpinWait.SpinUntil(() => ComposeBridges.SharedStateDependencyCount() == 1,
                        TimeSpan.FromSeconds(5)), "C must register its wait before B queries C.");
            }
            composer.RememberTimePickerState(stateC);
        });
        using var contentA = new ComposableLambda2(composer =>
        {
            if (!firstOwnerInsideNested)
                composer.RememberTimePickerState(stateA);
            if (nested)
            {
                outerComposer = composer;
                try
                {
                    compositions[1].ComposeContent(contentB);
                }
                finally
                {
                    outerComposer = null;
                }
            }
        });
        using var contentC = new ComposableLambda2(composer =>
        {
            composer.RememberTimePickerState(stateC);
            if (nested)
            {
                if (concurrent)
                {
                    Assert.IsTrue(barrier.SignalAndWait(TimeSpan.FromSeconds(5)));
                    if (nestedWaitsFirst)
                        Assert.IsTrue(SpinWait.SpinUntil(() => ComposeBridges.SharedStateDependencyCount() == 2,
                            TimeSpan.FromSeconds(5)), "Both held A and B must be recorded before C targets A.");
                }
                composer.RememberTimePickerState(stateA);
            }
        });
        ExceptionDispatchInfo? failureA = null;
        ExceptionDispatchInfo? failureC = null;
        bool finished = true;
        try
        {
            compositions[0].ComposeContent(contentA);
            Apply(compositions[0]);
            compositions[2].ComposeContent(contentC);
            Apply(compositions[2]);
            var peerA = stateA.Jvm;
            var peerC = stateC.Jvm;
            Assert.AreEqual(!firstOwnerInsideNested, peerA is not null);
            nested = concurrent = true;
            var first = new Thread(() => Run(compositions[0], contentA, ref failureA)) { IsBackground = true };
            var second = new Thread(() => Run(compositions[2], contentC, ref failureC)) { IsBackground = true };
            first.Start();
            second.Start();
            finished = JoinAll([first, second]);
            Assert.IsTrue(finished, "An enclosing native monitor was omitted from cycle detection.");
            Assert.AreEqual(0, ComposeBridges.SharedStateDependencyCount());
            var failure = nestedWaitsFirst ? failureC : failureA;
            Assert.IsInstanceOfType<Java.Lang.IllegalStateException>(failure?.SourceException);
            StringAssert.Contains(failure?.SourceException.Message
                ?? throw new InvalidOperationException("Cycle failure was unavailable."), "Shared state ownership cycle detected");
            if (nestedWaitsFirst)
            {
                failureA?.Throw();
                Apply(compositions[1]);
                Apply(compositions[0]);
            }
            else
            {
                failureC?.Throw();
                Apply(compositions[2]);
            }
            concurrent = false;
            compositions[0].ComposeContent(contentA);
            Apply(compositions[1]);
            Apply(compositions[0]);
            compositions[2].ComposeContent(contentC);
            Apply(compositions[2]);
            Assert.IsNotNull(stateA.Jvm);
            if (!firstOwnerInsideNested)
                Assert.AreSame(peerA, stateA.Jvm);
            Assert.AreSame(peerC, stateC.Jvm);
            Assert.AreEqual(0, ComposeBridges.SharedStateDependencyCount());
        }
        finally
        {
            if (finished)
            {
                foreach (var composition in compositions)
                    composition.Dispose();
                recomposer.Cancel();
                recomposer.Dispose();
                foreach (var applier in appliers)
                    applier.Dispose();
                barrier.Dispose();
            }
        }
    }

    [TestMethod]
    public void MonitorCatalogue_DoesNotRootRetiredNativeMonitors()
    {
        Java.Lang.Ref.WeakReference? nativeProbe = null;
        var managed = SharedStateOwnerLifetimeTests.OnRetiredThread(() =>
        {
            using var applier = new StateOnlyApplier();
            using var recomposer = new Recomposer(Kotlin.Coroutines.EmptyCoroutineContext.Instance
                ?? throw new InvalidOperationException("Empty coroutine context was unavailable."));
            var composition = CompositionKt.ControlledComposition(applier, recomposer)
                ?? throw new InvalidOperationException("Controlled composition was unavailable.");
            var state = new TimePickerState(7, 10);
            using var content = new ComposableLambda2(composer => composer.RememberTimePickerState(state));
            try
            {
                composition.ComposeContent(content);
                Apply(composition);
                var peer = (Java.Lang.Object)composition;
                using var field = peer.Class.GetDeclaredField("lock")
                    ?? throw new InvalidOperationException("Native monitor field unavailable.");
                field.Accessible = true;
                using var monitor = field.Get(peer)
                    ?? throw new InvalidOperationException("Native monitor unavailable.");
                nativeProbe = new Java.Lang.Ref.WeakReference(monitor);
                Assert.IsNotNull(nativeProbe.Get());
            }
            finally
            {
                composition.Dispose();
                recomposer.Cancel();
            }
            return [new WeakReference<object>(composition, trackResurrection: true)];
        });
        using var probe = nativeProbe ?? throw new InvalidOperationException("Native monitor probe unavailable.");
        SharedStateOwnerLifetimeTests.AssertCollected(managed);
        for (int attempt = 0; attempt < 20; attempt++)
        {
            SharedStateOwnerLifetimeTests.CollectBothRuntimes();
            if (probe.Get() is null)
            {
                Assert.AreEqual(0, ComposeBridges.SharedStateDependencyCount());
                return;
            }
            Thread.Sleep(50);
        }
        Assert.Fail("The weak catalogue retained a retired native composition monitor.");
    }

    [TestMethod]
    public void UnpublishedInternalTarget_ThrowsWithoutWaiting()
    {
        using var applier = new StateOnlyApplier();
        using var recomposer = new Recomposer(Kotlin.Coroutines.EmptyCoroutineContext.Instance
            ?? throw new InvalidOperationException("Empty coroutine context was unavailable."));
        var composition = CompositionKt.ControlledComposition(applier, recomposer)
            ?? throw new InvalidOperationException("Controlled composition was unavailable.");
        using var token = SharedStateOwner.Publish(new object(), () => { }, _ => { });
        try
        {
            var error = Assert.ThrowsExactly<Java.Lang.IllegalStateException>(() =>
                ComposeBridges.SharedStateIsLive(composition, token, null));
            StringAssert.Contains(error.Message, "owner monitor was not registered before publication");
            Assert.AreEqual(0, ComposeBridges.SharedStateDependencyCount());
        }
        finally
        {
            token.OnAbandoned();
            composition.Dispose();
            recomposer.Cancel();
        }
    }

    [TestMethod]
    public void SameThreadNestedBorrowing_IsReentrant()
    {
        using var outerApplier = new StateOnlyApplier();
        using var innerApplier = new StateOnlyApplier();
        using var recomposer = new Recomposer(Kotlin.Coroutines.EmptyCoroutineContext.Instance
            ?? throw new InvalidOperationException("Empty coroutine context was unavailable."));
        var outer = CompositionKt.ControlledComposition(outerApplier, recomposer)
            ?? throw new InvalidOperationException("Outer composition was unavailable.");
        var inner = CompositionKt.ControlledComposition(innerApplier, recomposer)
            ?? throw new InvalidOperationException("Inner composition was unavailable.");
        var state = new TimePickerState(7, 10);
        bool nested = false;
        using var innerContent = new ComposableLambda2(composer =>
        {
            composer.RememberTimePickerState(state);
            Assert.AreEqual(0, ComposeBridges.SharedStateDependencyCount(), "Reentrant ownership must not add a wait edge.");
        });
        using var outerContent = new ComposableLambda2(composer =>
        {
            composer.RememberTimePickerState(state);
            composer.RememberTimePickerState(state);
            if (nested)
                inner.ComposeContent(innerContent);
        });
        try
        {
            outer.ComposeContent(outerContent);
            Apply(outer);
            var peer = state.Jvm;
            nested = true;
            outer.ComposeContent(outerContent);
            Apply(outer);
            Apply(inner);
            Assert.AreSame(peer, state.Jvm);
            Assert.AreEqual(0, ComposeBridges.SharedStateDependencyCount());
        }
        finally
        {
            inner.Dispose();
            outer.Dispose();
            recomposer.Cancel();
        }
    }

    static bool JoinAll(Thread[] threads)
    {
        bool finished = true;
        foreach (var thread in threads)
            finished &= thread.Join(TimeSpan.FromSeconds(10));
        return finished;
    }

    static void Run(IControlledComposition composition, ComposableLambda2 content, ref ExceptionDispatchInfo? failure)
    {
        try
        {
            composition.ComposeContent(content);
        }
        catch (Exception error)
        {
            failure = ExceptionDispatchInfo.Capture(error);
        }
    }

    static void Apply(IControlledComposition composition)
    {
        composition.ApplyChanges();
        composition.ApplyLateChanges();
        composition.ChangesApplied();
    }
}
