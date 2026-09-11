using System.Runtime.ExceptionServices;
using AndroidX.Compose;
using AndroidX.Compose.Runtime;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Checks crossed borrowing while two native composition monitors are held.</summary>
[TestClass]
[DoNotParallelize]
public class SharedStateConcurrentLifetimeTests
{
    [TestMethod]
    public void ConcurrentCrossedBorrowers_DoNotDeadlock()
    {
        var applierA = new StateOnlyApplier();
        var applierB = new StateOnlyApplier();
        var recomposer = new Recomposer(Kotlin.Coroutines.EmptyCoroutineContext.Instance
            ?? throw new InvalidOperationException("Empty coroutine context was unavailable."));
        var compositionA = CompositionKt.ControlledComposition(applierA, recomposer)
            ?? throw new InvalidOperationException("Composition A was unavailable.");
        var compositionB = CompositionKt.ControlledComposition(applierB, recomposer)
            ?? throw new InvalidOperationException("Composition B was unavailable.");
        var stateA = new TimePickerState(7, 10);
        var stateB = new TimePickerState(8, 20);
        var barrier = new Barrier(2);
        bool crossed = false;
        int reachedBorrow = 0;
        ExceptionDispatchInfo? failureA = null;
        ExceptionDispatchInfo? failureB = null;
        var contentA = Content(stateA, stateB);
        var contentB = Content(stateB, stateA);
        bool finished = true;
        try
        {
            compositionA.ComposeContent(contentA);
            Apply(compositionA);
            compositionB.ComposeContent(contentB);
            Apply(compositionB);
            var peerA = stateA.Jvm;
            var peerB = stateB.Jvm;
            crossed = true;
            var first = new Thread(() => Run(compositionA, contentA, ref failureA)) { IsBackground = true };
            var second = new Thread(() => Run(compositionB, contentB, ref failureB)) { IsBackground = true };
            first.Start();
            second.Start();
            bool joinedA = first.Join(TimeSpan.FromSeconds(10));
            bool joinedB = second.Join(TimeSpan.FromSeconds(10));
            finished = joinedA && joinedB;
            Assert.AreEqual(2, Volatile.Read(ref reachedBorrow),
                "Guard: both native compositions must reach crossed borrowing concurrently.");
            Assert.IsTrue(finished, "Crossed borrowers deadlocked while holding their native composition monitors.");
            failureA?.Throw();
            failureB?.Throw();
            Apply(compositionA);
            Apply(compositionB);
            Assert.AreSame(peerA, stateA.Jvm);
            Assert.AreSame(peerB, stateB.Jvm);
        }
        finally
        {
            // The isolated instrumentation process must be stopped after a timeout:
            // disposing a composition would wait on the same deadlocked monitor.
            if (finished)
            {
                compositionA.Dispose();
                compositionB.Dispose();
                recomposer.Cancel();
                recomposer.Dispose();
                contentA.Dispose();
                contentB.Dispose();
                applierA.Dispose();
                applierB.Dispose();
                barrier.Dispose();
            }
        }

        ComposableLambda2 Content(TimePickerState own, TimePickerState other) => new(composer =>
        {
            composer.RememberTimePickerState(own);
            if (crossed)
            {
                Assert.IsTrue(barrier.SignalAndWait(TimeSpan.FromSeconds(5)),
                    "Both native composition bodies did not execute concurrently.");
                Interlocked.Increment(ref reachedBorrow);
                composer.RememberTimePickerState(other);
            }
        });
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
