using System.Runtime.ExceptionServices;
using AndroidX.Compose;
using AndroidX.Compose.Runtime;
using AndroidX.Compose.Runtime.Internal;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Exercises concurrent first acquisition of one previously unowned wrapper.</summary>
[TestClass]
[DoNotParallelize]
public class SharedStateFirstOwnerTests
{
    [TestMethod]
    public void SimultaneousFirstClaims_ShareOneNativePeer()
    {
        for (int attempt = 0; attempt < 64; attempt++)
            FirstClaims(attempt);
    }

    [TestMethod]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public void PublishedWinner_SerializesInitializationAndFailure(bool failWinner, bool bindBeforeFailure)
    {
        StateOnlyApplier[] appliers = [new(), new()];
        var recomposer = new Recomposer(Kotlin.Coroutines.EmptyCoroutineContext.Instance
            ?? throw new InvalidOperationException("Empty coroutine context unavailable."));
        var compositions = appliers.Select(applier => CompositionKt.ControlledComposition(applier, recomposer)
            ?? throw new InvalidOperationException("Controlled composition unavailable.")).ToArray();
        var state = new TimePickerState(7, 10);
        var published = new ManualResetEventSlim();
        var proceed = new ManualResetEventSlim();
        var blocker = new ThrowingAbandonObserver { ThrowOnAbandoned = false };
        var expected = new InvalidOperationException("Expected first owner failure.");
        var failures = new ExceptionDispatchInfo?[2];
        var threads = new Thread[2];
        var peers = new global::AndroidX.Compose.Material3.ITimePickerState?[2];
        SharedStateOwner? originalOwner = null;
        object? arbitrationGate = null;
        bool firstAttempt = true;
        int nativeFactories = 0;
        int originalReleases = 0;
        int releasingThread = 0;
        var contents = Enumerable.Range(0, 2).Select(index =>
            ComposableLambdaKt.ComposableLambdaInstance(354911 + index, false, new ComposableLambda2(composer =>
            {
                if (index == 1 && firstAttempt)
                    Assert.IsTrue(published.Wait(TimeSpan.FromSeconds(5)), "The first claim was not published.");
                if (index == 0)
                {
                    composer.StartReplaceableGroup(354913);
                    if (firstAttempt)
                        composer.UpdateRememberedValue(blocker);
                    composer.EndReplaceableGroup();
                }
                var owner = SharedStateOwner.Remember(composer, state, () =>
                {
                    Assert.IsFalse(Monitor.IsEntered(arbitrationGate
                        ?? throw new InvalidOperationException("Arbitration gate unavailable.")),
                        "Release callbacks must not hold the managed arbitration gate.");
                    if (index == 0)
                    {
                        Interlocked.Increment(ref originalReleases);
                        releasingThread = Environment.CurrentManagedThreadId;
                    }
                    state.UnbindJvm();
                });
                if (index == 0 && firstAttempt)
                {
                    originalOwner = owner;
                    var ownership = typeof(SharedStateOwner).GetField("_ownership",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                        ?.GetValue(owner) as SharedStateOwnership
                        ?? throw new InvalidOperationException("First owner arbitration unavailable.");
                    arbitrationGate = ownership.Gate;
                }
                using var acquisition = owner.Acquire();
                composer.StartReusableGroup(354102, owner);
                try
                {
                    if (acquisition.IsOwner)
                    {
                        owner.TrackScope(composer);
                        if (index == 0 && firstAttempt)
                        {
                            published.Set();
                            Assert.IsTrue(proceed.Wait(TimeSpan.FromSeconds(5)), "The pending owner was not released.");
                            if (failWinner && !bindBeforeFailure)
                            {
                                throw expected;
                            }
                        }
                        Interlocked.Increment(ref nativeFactories);
                        peers[index] = BindNative(composer, state);
                        if (index == 0 && firstAttempt && failWinner)
                        {
                            state.Hour = 19;
                            throw expected;
                        }
                        acquisition.Publish((Java.Lang.Object)(peers[index]
                            ?? throw new InvalidOperationException("Native initialization returned no peer.")));
                    }
                    else
                        peers[index] = acquisition.Peer as global::AndroidX.Compose.Material3.ITimePickerState
                            ?? throw new InvalidOperationException("Acquisition has no published TimePickerState.");
                }
                catch (Exception error)
                {
                    acquisition.Abort(error);
                    throw;
                }
                finally
                {
                    composer.EndReusableGroup();
                }
            }))).ToArray();
        bool finished = true;
        try
        {
            for (int i = 0; i < 2; i++)
            {
                int index = i;
                threads[i] = new Thread(() =>
                {
                    try
                    {
                        compositions[index].ComposeContent(contents[index]);
                    }
                    catch (Exception error)
                    {
                        failures[index] = ExceptionDispatchInfo.Capture(error);
                    }
                }) { IsBackground = true };
                threads[i].Start();
            }
            bool waiting;
            try
            {
                Assert.IsTrue(published.Wait(TimeSpan.FromSeconds(5)));
                waiting = SpinWait.SpinUntil(() => ComposeBridges.SharedStateDependencyCount() == 1,
                    TimeSpan.FromSeconds(5));
                Assert.IsNull(state.Jvm, "Guard: the winning claim must not yet have a bound peer.");
            }
            finally
            {
                proceed.Set();
                finished = threads[0].Join(TimeSpan.FromSeconds(10));
                finished &= threads[1].Join(TimeSpan.FromSeconds(10));
            }
            Assert.IsTrue(finished, "A borrower held the managed gate while waiting on the winner.");
            Assert.IsTrue(waiting, "The losing claimant did not wait on the published native owner.");
            Assert.AreEqual(0, ComposeBridges.SharedStateDependencyCount());
            failures[1]?.Throw();
            if (failWinner)
            {
                Assert.AreSame(expected, failures[0]?.SourceException, "The original initialization failure was replaced.");
                Assert.AreEqual(threads[0].ManagedThreadId, releasingThread);
                Assert.AreEqual(1, blocker.AbandonedCalls);
                Assert.AreEqual(1, originalReleases);
                Assert.AreEqual(bindBeforeFailure ? 2 : 1, nativeFactories);
                Assert.AreNotSame(peers[0], peers[1], "An abandoned first owner must not retain its native peer.");
                Assert.AreEqual(bindBeforeFailure ? 19 : 7, state.Hour);
                Apply(compositions[1]);
                originalOwner?.OnAbandoned();
                Assert.AreSame(peers[1], state.Jvm, "A stale callback cleared the successor's binding.");
            }
            else
            {
                failures[0]?.Throw();
                Assert.AreEqual(1, nativeFactories);
                Assert.AreSame(peers[0], peers[1]);
                Apply(compositions[0]);
                Apply(compositions[1]);
            }
            var peer = state.Jvm;
            firstAttempt = false;
            blocker.ThrowOnAbandoned = false;
            for (int i = 0; i < 2; i++)
            {
                compositions[i].ComposeContent(contents[i]);
                Apply(compositions[i]);
                Assert.AreSame(peer, state.Jvm, "Sequential retry replaced the surviving peer.");
            }
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
                foreach (var content in contents)
                    content.Dispose();
                foreach (var applier in appliers)
                    applier.Dispose();
                blocker.Dispose();
                published.Dispose();
                proceed.Dispose();
            }
        }
    }

    [TestMethod]
    public void TypedHelper_PostReturnFailureRecoversAfterOwnerAbandonment()
    {
        StateOnlyApplier[] appliers = [new(), new()];
        var recomposer = new Recomposer(Kotlin.Coroutines.EmptyCoroutineContext.Instance
            ?? throw new InvalidOperationException("Empty coroutine context unavailable."));
        var compositions = appliers.Select(applier => CompositionKt.ControlledComposition(applier, recomposer)
            ?? throw new InvalidOperationException("Controlled composition unavailable.")).ToArray();
        var state = new TimePickerState(7, 10);
        using var initialized = new ManualResetEventSlim();
        using var proceed = new ManualResetEventSlim();
        var expected = new InvalidOperationException("Expected failure after the typed owner helper returned.");
        var failures = new ExceptionDispatchInfo?[2];
        global::AndroidX.Compose.Material3.ITimePickerState? initialPeer = null;
        bool firstAttempt = true;
        var contents = Enumerable.Range(0, 2).Select(index =>
            ComposableLambdaKt.ComposableLambdaInstance(354921 + index, false, new ComposableLambda2(composer =>
            {
                if (index == 1 && firstAttempt)
                    Assert.IsTrue(initialized.Wait(TimeSpan.FromSeconds(5)));
                composer.RememberTimePickerState(state);
                if (index == 0 && firstAttempt)
                {
                    initialPeer = state.Jvm
                        ?? throw new InvalidOperationException("The typed helper returned before binding.");
                    state.Hour = 19;
                    initialized.Set();
                    Assert.IsTrue(proceed.Wait(TimeSpan.FromSeconds(5)));
                    throw expected;
                }
            }))).ToArray();
        var threads = Enumerable.Range(0, 2).Select(index => new Thread(() =>
        {
            try
            {
                compositions[index].ComposeContent(contents[index]);
            }
            catch (Exception error)
            {
                failures[index] = ExceptionDispatchInfo.Capture(error);
            }
        }) { IsBackground = true }).ToArray();
        bool finished = true;
        try
        {
            foreach (var thread in threads)
                thread.Start();
            try
            {
                Assert.IsTrue(initialized.Wait(TimeSpan.FromSeconds(5)));
                Assert.IsTrue(SpinWait.SpinUntil(() => ComposeBridges.SharedStateDependencyCount() == 1,
                    TimeSpan.FromSeconds(5)), "The borrower never queried the initialized owner.");
            }
            finally
            {
                proceed.Set();
                finished = threads[0].Join(TimeSpan.FromSeconds(10));
                finished &= threads[1].Join(TimeSpan.FromSeconds(10));
            }
            Assert.IsTrue(finished, "Post-publication abandonment did not finish.");
            Assert.AreSame(expected, failures[0]?.SourceException);
            failures[1]?.Throw();
            Assert.AreEqual(0, ComposeBridges.SharedStateDependencyCount());
            Apply(compositions[1]);

            // Native cleanup can follow a successful borrow; the next execution reacquires.
            firstAttempt = false;
            compositions[1].ComposeContent(contents[1]);
            Apply(compositions[1]);
            var successor = state.Jvm
                ?? throw new InvalidOperationException("The surviving consumer did not reacquire after abandonment.");
            Assert.AreNotSame(initialPeer, successor);
            Assert.AreEqual(19, state.Hour);
            compositions[0].ComposeContent(contents[0]);
            Apply(compositions[0]);
            Assert.AreSame(successor, state.Jvm, "Sequential retry replaced the surviving peer.");
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
                foreach (var content in contents)
                    content.Dispose();
                foreach (var applier in appliers)
                    applier.Dispose();
            }
        }
    }

    static global::AndroidX.Compose.Material3.ITimePickerState BindNative(IComposer composer, TimePickerState state)
    {
        var handle = ComposeBridges.RememberTimePickerState(state.RememberHour, state.RememberMinute, state.Is24Hour, composer);
        var peer = Java.Lang.Object.GetObject<global::AndroidX.Compose.Material3.ITimePickerState>(
            handle, global::Android.Runtime.JniHandleOwnership.DoNotTransfer)
            ?? throw new InvalidOperationException("Native remember returned no peer.");
        state.BindJvm(peer);
        return peer;
    }

    static void Apply(IControlledComposition composition)
    {
        composition.ApplyChanges();
        composition.ApplyLateChanges();
        composition.ChangesApplied();
    }

    static void FirstClaims(int attempt)
    {
        StateOnlyApplier[] appliers = [new(), new()];
        var recomposer = new Recomposer(Kotlin.Coroutines.EmptyCoroutineContext.Instance
            ?? throw new InvalidOperationException("Empty coroutine context unavailable."));
        var compositions = appliers.Select(applier => CompositionKt.ControlledComposition(applier, recomposer)
            ?? throw new InvalidOperationException("Controlled composition unavailable.")).ToArray();
        var state = new TimePickerState(7, 10);
        var barrier = new Java.Util.Concurrent.CyclicBarrier(2);
        var failures = new ExceptionDispatchInfo?[2];
        var peers = new global::AndroidX.Compose.Material3.ITimePickerState?[2];
        int nativeFactories = 0;
        var contents = Enumerable.Range(0, 2).Select(index =>
            ComposableLambdaKt.ComposableLambdaInstance(354901 + index, false, new ComposableLambda2(composer =>
            {
                var owner = SharedStateOwner.Remember(composer, state, state.UnbindJvm);
                barrier.Await(5, Java.Util.Concurrent.TimeUnit.Seconds
                    ?? throw new InvalidOperationException("Seconds time unit unavailable."));
                using var acquisition = owner.Acquire();
                composer.StartReusableGroup(354102, owner);
                try
                {
                    if (acquisition.IsOwner)
                    {
                        Interlocked.Increment(ref nativeFactories);
                        owner.TrackScope(composer);
                        var handle = ComposeBridges.RememberTimePickerState(7, 10, true, composer);
                        var peer = Java.Lang.Object.GetObject<global::AndroidX.Compose.Material3.ITimePickerState>(
                            handle, global::Android.Runtime.JniHandleOwnership.DoNotTransfer)
                            ?? throw new InvalidOperationException("Native remember returned no peer.");
                        state.BindJvm(peer);
                        acquisition.Publish((Java.Lang.Object)peer);
                        peers[index] = peer;
                    }
                    else
                        peers[index] = acquisition.Peer as global::AndroidX.Compose.Material3.ITimePickerState
                            ?? throw new InvalidOperationException("Acquisition has no published TimePickerState.");
                }
                catch (Exception error)
                {
                    acquisition.Abort(error);
                    throw;
                }
                finally
                {
                    composer.EndReusableGroup();
                }
            }))).ToArray();
        bool finished = true;
        try
        {
            var threads = Enumerable.Range(0, 2).Select(index => new Thread(() =>
            {
                try
                {
                    compositions[index].ComposeContent(contents[index]);
                }
                catch (Exception error)
                {
                    failures[index] = ExceptionDispatchInfo.Capture(error);
                }
            }) { IsBackground = true }).ToArray();
            foreach (var thread in threads)
                thread.Start();
            finished = threads[0].Join(TimeSpan.FromSeconds(10));
            finished &= threads[1].Join(TimeSpan.FromSeconds(10));
            Assert.IsTrue(finished, "Concurrent first claims did not return within bounded joins.");
            foreach (var failure in failures)
                failure?.Throw();
            Assert.AreEqual(1, nativeFactories, $"Attempt {attempt}: one wrapper must have exactly one native factory.");
            Assert.AreSame(peers[0], peers[1], $"Attempt {attempt}: simultaneous first consumers did not share one peer.");
            Assert.AreSame(peers[0], state.Jvm);
            foreach (var composition in compositions)
            {
                composition.ApplyChanges();
                composition.ApplyLateChanges();
                composition.ChangesApplied();
            }
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
                foreach (var content in contents)
                    content.Dispose();
                foreach (var applier in appliers)
                    applier.Dispose();
                barrier.Dispose();
            }
        }
    }
}
