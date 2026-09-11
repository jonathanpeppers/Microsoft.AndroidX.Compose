using System.Reflection;
using Android.Runtime;
using AndroidX.Compose;
using AndroidX.Compose.Runtime;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Checks ownership created by paused work whose cancellation cleanup fails.</summary>
[TestClass]
[DoNotParallelize]
public class SharedStatePausedLifetimeTests
{
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void PausedOwner_PreservesCommittedSiblingAndRejectsCancelledInsertion(bool apply) =>
        ExercisePausedOwner(apply);

    [TestMethod]
    public void FailedPausedCancellation_DoesNotRootOwnerOrTransaction() =>
        SharedStateOwnerLifetimeTests.AssertCollected(
            SharedStateOwnerLifetimeTests.OnRetiredThread(() => ExercisePausedOwner(false)));

    static WeakReference<object>[] ExercisePausedOwner(bool apply)
    {
        using var applier = new StateOnlyApplier();
        using var siblingApplier = new StateOnlyApplier();
        using var recomposer = new Recomposer(Kotlin.Coroutines.EmptyCoroutineContext.Instance
            ?? throw new InvalidOperationException("Empty coroutine context was unavailable."));
        var composition = CompositionKt.ControlledComposition(applier, recomposer)
            ?? throw new InvalidOperationException("Controlled composition was unavailable.");
        var sibling = CompositionKt.ControlledComposition(siblingApplier, recomposer)
            ?? throw new InvalidOperationException("Sibling composition was unavailable.");
        using var pausable = composition.JavaCast<IPausableComposition>()
            ?? throw new InvalidOperationException("Composition was not pausable.");
        using var callback = new NeverPauseCallback();
        using var blocker = new ThrowingAbandonObserver { ThrowOnAbandoned = false };
        var committed = new TimePickerState(7, 10);
        var inserted = new TimePickerState(8, 20);
        bool insert = false;
        using var content = new ComposableLambda2(composer =>
        {
            composer.StartReplaceableGroup(354801);
            composer.RememberTimePickerState(committed);
            composer.EndReplaceableGroup();
            composer.StartReplaceableGroup(354802);
            if (insert)
            {
                composer.StartReplaceableGroup(354803);
                composer.UpdateRememberedValue(blocker);
                composer.EndReplaceableGroup();
                composer.RememberTimePickerState(inserted);
            }
            composer.EndReplaceableGroup();
        });
        using var consumers = new ComposableLambda2(composer =>
        {
            composer.RememberTimePickerState(committed);
            composer.RememberTimePickerState(inserted);
        });
        try
        {
            composition.ComposeContent(content);
            Apply(composition);
            var original = committed.Jvm ?? throw new InvalidOperationException("Committed peer was unavailable.");
            insert = true;
            using var paused = pausable.SetPausableContent(content);
            Assert.IsTrue(paused.Resume(callback));
            Assert.IsTrue(paused.IsComplete);
            Assert.IsFalse(paused.IsApplied);
            Assert.AreEqual(0, blocker.AbandonedCalls, "Resume unexpectedly abandoned the cancellation blocker.");
            blocker.ThrowOnAbandoned = true;
            var speculative = inserted.Jvm ?? throw new InvalidOperationException("Paused peer was unavailable.");
            var token = Owner(inserted);
            var scope = typeof(SharedStateOwner).GetField("_scope", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(token) as IRecomposeScope
                ?? throw new InvalidOperationException("Paused owner marker was unavailable.");
            Assert.IsTrue(ComposeBridges.SharedStateIsLive(composition, token, scope),
                "Guard: paused resume must install the owner marker before final application.");
            inserted.Hour = 19;
            inserted.Minute = 42;
            if (apply)
            {
                paused.Apply();
                Assert.IsTrue(paused.IsApplied);
            }
            else
            {
                var error = Assert.ThrowsExactly<Java.Lang.IllegalStateException>(paused.Cancel);
                StringAssert.Contains(error.Message, "Expected earlier abandon failure.");
                Assert.IsTrue(paused.IsCancelled);
                Assert.AreEqual(1, blocker.AbandonedCalls);
                Assert.AreSame(speculative, inserted.Jvm, "Guard: the owner's abandonment callback must be skipped.");
                Assert.IsTrue(ComposeBridges.SharedStateIsLive(composition, token, scope),
                    "Guard: installed membership alone must still accept the cancelled insertion.");
            }
            sibling.ComposeContent(consumers);
            Apply(sibling);
            Assert.AreSame(original, committed.Jvm, "Cancelling later work retired a previously committed owner.");
            Assert.IsNotNull(inserted.Jvm);
            if (apply)
                Assert.AreSame(speculative, inserted.Jvm, "Applied paused work lost its live owner.");
            else
                Assert.AreNotSame(speculative, inserted.Jvm, "Cancelled paused work was borrowed as a live owner.");
            Assert.AreEqual(19, inserted.Hour);
            Assert.AreEqual(42, inserted.Minute);
            GC.KeepAlive(token);
            GC.KeepAlive(paused);
            return
            [
                new(composition, trackResurrection: true),
                new(paused, trackResurrection: true),
                new(token, trackResurrection: true),
                new(committed, trackResurrection: true),
                new(inserted, trackResurrection: true)
            ];
        }
        finally
        {
            sibling.Dispose();
            composition.Dispose();
            recomposer.Cancel();
        }
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void PausedBorrower_TracksRegistrationAndLaterAcquisition(bool acquire)
    {
        using var applier = new StateOnlyApplier();
        using var externalApplier = new StateOnlyApplier();
        using var recomposer = new Recomposer(Kotlin.Coroutines.EmptyCoroutineContext.Instance
            ?? throw new InvalidOperationException("Empty coroutine context was unavailable."));
        var composition = CompositionKt.ControlledComposition(applier, recomposer)
            ?? throw new InvalidOperationException("Controlled composition was unavailable.");
        var external = CompositionKt.ControlledComposition(externalApplier, recomposer)
            ?? throw new InvalidOperationException("External composition was unavailable.");
        using var pausable = composition.JavaCast<IPausableComposition>()
            ?? throw new InvalidOperationException("Composition was not pausable.");
        using var callback = new NeverPauseCallback();
        using var blocker = new ThrowingAbandonObserver { ThrowOnAbandoned = false };
        object wrapper = new();
        int released = 0;
        bool pause = false;
        SharedStateOwner? candidate = null;
        SharedStateOwner? externalOwner = null;
        using var externalContent = new ComposableLambda2(composer =>
        {
            externalOwner = SharedStateOwner.Remember(composer, wrapper, () => released++);
            Assert.IsTrue(externalOwner.IsOwner);
        });
        using var content = new ComposableLambda2(composer =>
        {
            composer.StartReplaceableGroup(354811);
            if (pause)
            {
                composer.StartReplaceableGroup(354812);
                composer.UpdateRememberedValue(blocker);
                composer.EndReplaceableGroup();
            }
            composer.EndReplaceableGroup();
            composer.StartReplaceableGroup(354813);
            if (acquire || pause)
            {
                candidate = SharedStateOwner.Remember(composer, wrapper, () => released++);
                Assert.AreEqual(acquire && pause, candidate.IsOwner);
                if (candidate.IsOwner)
                    candidate.TrackScope(composer);
            }
            composer.EndReplaceableGroup();
        });
        try
        {
            external.ComposeContent(externalContent);
            Apply(external);
            composition.ComposeContent(content);
            Apply(composition);
            var previous = candidate;
            if (acquire)
            {
                Assert.IsNotNull(previous);
                external.Dispose();
                Assert.AreEqual(1, released);
            }
            pause = true;
            using var paused = pausable.SetPausableContent(content);
            Assert.IsTrue(paused.Resume(callback));
            Assert.IsTrue(paused.IsComplete);
            Assert.IsFalse(paused.IsApplied);
            Assert.AreEqual(0, blocker.AbandonedCalls);
            var token = candidate ?? throw new InvalidOperationException("Paused borrower was unavailable.");
            Assert.IsTrue(token.IsLive);
            if (acquire)
                Assert.AreSame(previous, token, "The test must acquire ownership using the previously committed borrower.");
            Assert.IsTrue(ComposeBridges.SharedStateIsLive(composition, token, null),
                "Guard: the candidate must be installed, including the null-marker borrower path.");
            blocker.ThrowOnAbandoned = true;
            var error = Assert.ThrowsExactly<Java.Lang.IllegalStateException>(paused.Cancel);
            StringAssert.Contains(error.Message, "Expected earlier abandon failure.");
            Assert.IsTrue(paused.IsCancelled);
            Assert.AreEqual(1, blocker.AbandonedCalls);
            Assert.AreEqual(acquire ? 1 : 0, released, "Guard: cancellation must not deliver candidate cleanup.");
            Assert.IsTrue(ComposeBridges.SharedStateIsLive(composition, token, null),
                "Guard: installed token membership must remain after the failed cancellation.");
            Assert.IsFalse(token.IsLive, "Cancelled registration or acquisition was accepted as live.");
            if (!acquire)
                Assert.IsTrue(externalOwner?.IsLive, "Cancelling a borrower retired the independent active owner.");
            GC.KeepAlive(token);
        }
        finally
        {
            composition.Dispose();
            external.Dispose();
            recomposer.Cancel();
        }
    }

    static SharedStateOwner Owner(object state)
    {
        Assert.IsTrue(SharedStateOwnerLifetimeTests.ProbeOwner(state).TryGetTarget(out var target));
        return target as SharedStateOwner
            ?? throw new InvalidOperationException("Shared owner was unavailable.");
    }

    static void Apply(IControlledComposition composition)
    {
        composition.ApplyChanges();
        composition.ApplyLateChanges();
        composition.ChangesApplied();
    }
}
