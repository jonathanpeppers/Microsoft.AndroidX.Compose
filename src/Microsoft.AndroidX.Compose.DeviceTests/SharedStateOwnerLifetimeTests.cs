using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using Android.Runtime;
using AndroidX.Compose;
using AndroidX.Compose.Runtime;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Checks GC retention and retirement of direct native observer peers.</summary>
[TestClass]
[DoNotParallelize]
public class SharedStateOwnerLifetimeTests
{
    [TestMethod]
    public void OwnerDeclaresActivationContract()
    {
        Assert.IsNotNull(typeof(SharedStateOwner).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic, null,
            [typeof(IntPtr), typeof(JniHandleOwnership)], null));
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void ForcedGc_NativeObserverRetiresOwnerAndSibling(bool abandon)
    {
        var retired = OnRetiredThread(() => ExerciseNativeObservers(abandon));
        AssertCollected(retired);
    }

    [TestMethod]
    public void EarlierCleanupFailure_DoesNotRootPublicOwner()
    {
        var retired = OnRetiredThread(RemoveThrowingSubtree);
        AssertCollected(retired);
    }

    [TestMethod]
    public void RetainedWrapper_DoesNotRootSkippedOwnerAndCanRebind()
    {
        var probes = RemoveThrowingSubtree();
        Assert.IsTrue(probes[1].TryGetTarget(out var retained));
        var state = retained as DrawerStateHolder
            ?? throw new InvalidOperationException("The wrapper was not retained.");
        var original = state.Jvm ?? throw new InvalidOperationException("The skipped binding was unavailable.");
        AssertCollected([probes[0], probes[3]]);
        using var applier = new StateOnlyApplier();
        using var recomposer = new Recomposer(Kotlin.Coroutines.EmptyCoroutineContext.Instance
            ?? throw new InvalidOperationException("Empty coroutine context was unavailable."));
        var composition = CompositionKt.ControlledComposition(applier, recomposer)
            ?? throw new InvalidOperationException("Controlled composition was unavailable.");
        using var content = new ComposableLambda2(composer => composer.RememberDrawerState(state));
        try
        {
            composition.ComposeContent(content);
            composition.ApplyChanges();
            composition.ApplyLateChanges();
            composition.ChangesApplied();
            Assert.IsNotNull(state.Jvm, "The retained wrapper did not acquire a live successor.");
            Assert.AreNotSame(original, state.Jvm, "Collected ownership did not replace the orphaned peer.");
            Assert.IsTrue(state.IsClosed);
        }
        finally
        {
            composition.Dispose();
            recomposer.Cancel();
        }
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void SkippedRetirement_RebindsRetainedOwnerBeforeGc(bool reuseComposition)
    {
        using var applier = new StateOnlyApplier();
        using var replacementApplier = new StateOnlyApplier();
        using var recomposer = new Recomposer(Kotlin.Coroutines.EmptyCoroutineContext.Instance
            ?? throw new InvalidOperationException("Empty coroutine context was unavailable."));
        var composition = CompositionKt.ControlledComposition(applier, recomposer)
            ?? throw new InvalidOperationException("Controlled composition was unavailable.");
        var replacement = CompositionKt.ControlledComposition(replacementApplier, recomposer)
            ?? throw new InvalidOperationException("Replacement composition was unavailable.");
        var state = new TimePickerState(7, 10);
        using var content = new ComposableLambda2(composer =>
        {
            composer.RememberTimePickerState(state);
            ThrowingChildCleanup(composer, () => { });
        });
        using var empty = new ComposableLambda2(_ => { });
        using var successor = new ComposableLambda2(composer =>
        {
            composer.RememberTimePickerState(state);
            var peer = state.Jvm;
            composer.RememberTimePickerState(state);
            Assert.AreSame(peer, state.Jvm, "Live siblings acquired separate native states.");
        });
        try
        {
            composition.ComposeContent(content);
            Apply(composition);
            var original = state.Jvm ?? throw new InvalidOperationException("Initial peer was unavailable.");
            var probe = ProbeOwner(state);
            Assert.IsTrue(probe.TryGetTarget(out var retainedToken));
            state.Hour = 19;
            state.Minute = 42;

            composition.ComposeContent(empty);
            var error = Assert.ThrowsExactly<Java.Lang.IllegalStateException>(composition.ApplyChanges);
            StringAssert.Contains(error.Message, "Expected earlier cleanup failure.");
            Assert.AreSame(original, state.Jvm, "Guard: retirement must have been skipped.");
            Assert.IsFalse(composition.IsDisposed, "Recovery must not depend on disposal.");
            var target = reuseComposition ? composition : replacement;
            target.ComposeContent(successor);
            Apply(target);
            Assert.IsNotNull(state.Jvm, "The successor must be bound, not merely different from the original.");
            Assert.AreNotSame(original, state.Jvm, "A retained obsolete token blocked successor ownership.");
            Assert.AreEqual(19, state.Hour, "Handoff lost the native settled hour.");
            Assert.AreEqual(42, state.Minute, "Handoff lost the native settled minute.");
            GC.KeepAlive(retainedToken);
            GC.KeepAlive(original);
            GC.KeepAlive(composition);
        }
        finally
        {
            replacement.Dispose();
            composition.Dispose();
            recomposer.Cancel();
        }

        static void Apply(IControlledComposition target)
        {
            target.ApplyChanges();
            target.ApplyLateChanges();
            target.ChangesApplied();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static WeakReference<object>[] RemoveThrowingSubtree()
    {
        using var applier = new StateOnlyApplier();
        using var recomposer = new Recomposer(Kotlin.Coroutines.EmptyCoroutineContext.Instance
            ?? throw new InvalidOperationException("Empty coroutine context was unavailable."));
        var composition = CompositionKt.ControlledComposition(applier, recomposer)
            ?? throw new InvalidOperationException("Controlled composition was unavailable.");
        var state = new DrawerStateHolder();
        object payload = new();
        List<string> events = [];
        WeakReference<object>? ownerProbe = null;
        using var content = new ComposableLambda2(composer =>
        {
            composer.RememberDrawerState(state, _ =>
            {
                GC.KeepAlive(payload);
                return true;
            });
            ownerProbe = ProbeOwner(state);
            ThrowingChildCleanup(composer, () => events.Add("child-throws"));
            composer.SideEffect(() => events.Add("initial-applied"));
        });
        using var empty = new ComposableLambda2(_ => { });
        try
        {
            composition.ComposeContent(content);
            composition.ApplyChanges();
            composition.ApplyLateChanges();
            composition.ChangesApplied();
            Assert.IsNotNull(state.Jvm, "Initial successful application must retain the native state.");
            string[] expectedEvents = ["initial-applied"];
            CollectionAssert.AreEqual(expectedEvents, events);
            Console.WriteLine("Initial state bound after successful ApplyChanges.");
            composition.ComposeContent(empty);
            var error = Assert.ThrowsExactly<Java.Lang.IllegalStateException>(composition.ApplyChanges);
            StringAssert.Contains(error.Message, "Expected earlier cleanup failure.");
            Console.WriteLine("Retirement sequence: " + string.Join(", ", events));
            Console.WriteLine("After throwing removal: state bound=" + (state.Jvm is not null));
        }
        finally
        {
            composition.Dispose();
            recomposer.Cancel();
        }
        Console.WriteLine("After disposal: state bound=" + (state.Jvm is not null));
        Assert.IsNotNull(state.Jvm, "The regression must exercise a skipped public-owner retirement callback.");
        return
        [
            new(composition, trackResurrection: true),
            new(state, trackResurrection: true),
            new(payload, trackResurrection: true),
            ownerProbe ?? throw new InvalidOperationException("Owner was not published.")
        ];
    }

    [TestMethod]
    [DataRow(false, false, false)]
    [DataRow(true, false, false)]
    [DataRow(true, true, false)]
    [DataRow(false, false, true)]
    public void EarlierAbandonFailure_RebindsUncommittedOwnerBeforeGc(
        bool reuseComposition, bool nativeControl, bool unrelatedPending)
    {
        using var applier = new StateOnlyApplier();
        using var replacementApplier = new StateOnlyApplier();
        using var recomposer = new Recomposer(Kotlin.Coroutines.EmptyCoroutineContext.Instance
            ?? throw new InvalidOperationException("Empty coroutine context was unavailable."));
        var composition = CompositionKt.ControlledComposition(applier, recomposer)
            ?? throw new InvalidOperationException("Controlled composition was unavailable.");
        var replacement = CompositionKt.ControlledComposition(replacementApplier, recomposer)
            ?? throw new InvalidOperationException("Replacement composition was unavailable.");
        using var blocker = new ThrowingAbandonObserver();
        var state = new TimePickerState(7, 10);
        using var content = new ComposableLambda2(composer =>
        {
            composer.StartReplaceableGroup(354711);
            composer.UpdateRememberedValue(blocker);
            composer.EndReplaceableGroup();
            RememberState(composer);
        });
        using var successor = new ComposableLambda2(RememberState);
        using var empty = new ComposableLambda2(_ => { });
        try
        {
            var hash = JNIEnv.GetMethodID(blocker.Class.Handle, "hashCode", "()I");
            Assert.AreEqual(0, JNIEnv.CallIntMethod(blocker.Handle, hash), "Native abandon ordering needs the zero hash.");
            composition.ComposeContent(content);
            var original = state.Jvm ?? throw new InvalidOperationException("Initial peer was unavailable.");
            object? retainedToken = null;
            if (!nativeControl)
                Assert.IsTrue(ProbeOwner(state).TryGetTarget(out retainedToken));
            state.Hour = 19;
            state.Minute = 42;
            var error = Assert.ThrowsExactly<Java.Lang.IllegalStateException>(composition.AbandonChanges);
            StringAssert.Contains(error.Message, "Expected earlier abandon failure.");
            Assert.AreEqual(1, blocker.AbandonedCalls);
            Assert.AreSame(original, state.Jvm, "Guard: native dispatch must skip the owner's abandonment callback.");
            Assert.IsFalse(composition.IsDisposed);
            Assert.IsFalse(composition.HasPendingChanges);
            if (unrelatedPending)
            {
                composition.ComposeContent(empty);
                Assert.IsTrue(composition.HasPendingChanges, "The intervening attempt must remain unapplied.");
                Assert.AreSame(original, state.Jvm, "Guard: the old provisional binding must survive the intervening attempt.");
            }
            var target = reuseComposition ? composition : replacement;
            target.ComposeContent(successor);
            target.ApplyChanges();
            target.ApplyLateChanges();
            target.ChangesApplied();
            Assert.IsNotNull(state.Jvm, "The successor must be bound, not merely different from the original.");
            Assert.AreNotSame(original, state.Jvm, "Skipped abandonment retained an uncommitted native owner.");
            Assert.AreEqual(nativeControl ? 7 : 19, state.Hour);
            Assert.AreEqual(nativeControl ? 10 : 42, state.Minute);
            GC.KeepAlive(retainedToken);
            GC.KeepAlive(blocker);
        }
        finally
        {
            composition.Dispose();
            replacement.Dispose();
            recomposer.Cancel();
        }

        void RememberState(IComposer composer)
        {
            if (!nativeControl)
            {
                composer.RememberTimePickerState(state);
                return;
            }
            var handle = ComposeBridges.RememberTimePickerStateJvm(7, 10, true, composer);
            try
            {
                state.BindJvm(Java.Lang.Object.GetObject<global::AndroidX.Compose.Material3.ITimePickerState>(
                    handle, JniHandleOwnership.DoNotTransfer)
                    ?? throw new InvalidOperationException("Native control did not return a state peer."));
            }
            finally
            {
                JNIEnv.DeleteLocalRef(handle);
            }
        }
    }

    [TestMethod]
    public void ProvisionalSiblings_DisjointInsertionsShareOneOwner()
    {
        using var applier = new StateOnlyApplier();
        using var recomposer = new Recomposer(Kotlin.Coroutines.EmptyCoroutineContext.Instance
            ?? throw new InvalidOperationException("Empty coroutine context was unavailable."));
        var composition = CompositionKt.ControlledComposition(applier, recomposer)
            ?? throw new InvalidOperationException("Controlled composition was unavailable.");
        var state = new TimePickerState(7, 10);
        bool insert = false;
        object? firstPeer = null;
        using var content = new ComposableLambda2(composer =>
        {
            for (int i = 0; i < 2; i++)
            {
                composer.StartReplaceableGroup(354721 + i);
                if (insert)
                {
                    composer.RememberTimePickerState(state);
                    Assert.IsNotNull(state.Jvm);
                    if (i == 0)
                        firstPeer = state.Jvm;
                    else
                        Assert.AreSame(firstPeer, state.Jvm, "Separate insertion regions created competing provisional owners.");
                }
                composer.EndReplaceableGroup();
            }
        });
        try
        {
            composition.ComposeContent(content);
            Apply();
            insert = true;
            composition.ComposeContent(content);
            Assert.AreSame(firstPeer, state.Jvm, "Completion lost provisional sibling sharing.");
            Apply();
            Assert.IsNotNull(state.Jvm);
            Assert.AreSame(firstPeer, state.Jvm, "Application replaced the shared provisional state.");
        }
        finally
        {
            composition.Dispose();
            recomposer.Cancel();
        }

        void Apply()
        {
            composition.ApplyChanges();
            composition.ApplyLateChanges();
            composition.ChangesApplied();
        }
    }

    [TestMethod]
    public void CompletedPendingOwner_RemainsSharedUntilItsOwnBatchRetires()
    {
        using var applier = new StateOnlyApplier();
        using var siblingApplier = new StateOnlyApplier();
        using var recomposer = new Recomposer(Kotlin.Coroutines.EmptyCoroutineContext.Instance
            ?? throw new InvalidOperationException("Empty coroutine context was unavailable."));
        var composition = CompositionKt.ControlledComposition(applier, recomposer)
            ?? throw new InvalidOperationException("Controlled composition was unavailable.");
        var sibling = CompositionKt.ControlledComposition(siblingApplier, recomposer)
            ?? throw new InvalidOperationException("Sibling composition was unavailable.");
        var state = new TimePickerState(7, 10);
        using var content = new ComposableLambda2(composer =>
        {
            Assert.AreEqual(ComposeRuntimeFlags.IsLinkBufferComposerEnabled
                ? "androidx.compose.runtime.LinkComposer" : "androidx.compose.runtime.GapComposer",
                ((Java.Lang.Object)composer).Class.Name);
            composer.RememberTimePickerState(state);
        });
        try
        {
            composition.ComposeContent(content);
            Assert.IsTrue(composition.HasPendingChanges);
            var original = state.Jvm ?? throw new InvalidOperationException("Pending owner has no native peer.");
            sibling.ComposeContent(content);
            Assert.AreSame(original, state.Jvm, "A legitimate completed-pending owner must be shared.");
            Apply(sibling);
            Assert.AreSame(original, state.Jvm);
            Apply(composition);
            Assert.AreSame(original, state.Jvm, "Applying the actual owner must preserve its peer.");
            composition.Dispose();
            Assert.IsNull(state.Jvm, "Delivered retirement must release the owning peer.");
            sibling.ComposeContent(content);
            Apply(sibling);
            Assert.IsNotNull(state.Jvm);
            Assert.AreNotSame(original, state.Jvm, "The surviving sibling must acquire new ownership.");
        }
        finally
        {
            sibling.Dispose();
            composition.Dispose();
            recomposer.Cancel();
        }

        static void Apply(IControlledComposition target)
        {
            target.ApplyChanges();
            target.ApplyLateChanges();
            target.ChangesApplied();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static WeakReference<object> ProbeOwner(object state)
    {
        var field = typeof(SharedStateOwner).GetField("Ownerships", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Shared ownership table was not available.");
        var table = field.GetValue(null) as ConditionalWeakTable<object, SharedStateOwnership>
            ?? throw new InvalidOperationException("Shared ownership table had an unexpected type.");
        Assert.IsTrue(table.TryGetValue(state, out var ownership));
        var owner = ownership?.Owner ?? throw new InvalidOperationException("Typed public helper did not publish an owner.");
        return new(owner, trackResurrection: true);
    }

    /// <summary>Places the throwing effect in a real generated child restart group.</summary>
    [global::AndroidX.Compose.Composable]
    public static void ThrowingChildCleanup(IComposer composer, Action record)
    {
        composer.DisposableEffect(0, () => () =>
        {
            record();
            throw new Java.Lang.IllegalStateException("Expected earlier cleanup failure.");
        });
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static WeakReference<object>[] ExerciseNativeObservers(bool abandon)
    {
        using var applier = new StateOnlyApplier();
        using var recomposer = new Recomposer(Kotlin.Coroutines.EmptyCoroutineContext.Instance
            ?? throw new InvalidOperationException("Empty coroutine context was unavailable."));
        var composition = CompositionKt.ControlledComposition(applier, recomposer)
            ?? throw new InvalidOperationException("Controlled composition was unavailable.");
        object wrapper = new();
        bool visible = true;
        int released = 0;
        WeakReference<object>?[] current = new WeakReference<object>?[2];
        List<WeakReference<object>> retired = [];
        using var content = new ComposableLambda2(composer =>
        {
            for (int i = 0; i < 2; i++)
            {
                composer.StartReplaceableGroup(354701 + i);
                if (visible)
                {
                    var owner = SharedStateOwner.Remember(composer, wrapper, () => released++);
                    Assert.AreEqual(i == 0, owner.IsOwner, "Sibling must not acquire independent ownership.");
                    if (current[i] is { } previous)
                    {
                        Assert.IsTrue(previous.TryGetTarget(out var original), "Active observer lost its managed peer.");
                        Assert.AreSame(original, owner, "RememberedValue returned a different managed peer.");
                    }
                    else
                    {
                        var probe = new WeakReference<object>(owner, trackResurrection: true);
                        current[i] = probe;
                        retired.Add(probe);
                    }
                }
                composer.EndReplaceableGroup();
            }
        });
        try
        {
            for (int cycle = 0; cycle < 3; cycle++)
            {
                visible = true;
                current = new WeakReference<object>?[2];
                composition.ComposeContent(content);
                CollectBothRuntimes();
                if (abandon)
                    composition.AbandonChanges();
                else
                {
                    Apply();
                    CollectBothRuntimes();
                    composition.ComposeContent(content);
                    Apply();
                    Assert.AreEqual(cycle, released, "GC or recomposition prematurely retired the owner.");
                    visible = false;
                    composition.ComposeContent(content);
                    Apply();
                }
                Assert.AreEqual(cycle + 1, released, "Native retirement did not invoke the original release callback once.");
            }
        }
        finally
        {
            composition.Dispose();
            recomposer.Cancel();
        }
        return [.. retired];

        void Apply()
        {
            composition.ApplyChanges();
            composition.ApplyLateChanges();
            composition.ChangesApplied();
        }
    }

    [TestMethod]
    public void FailedPublication_ReleasesPeerAndCapturedPayload()
    {
        var retired = OnRetiredThread(FailPublication);
        AssertCollected(retired);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static WeakReference<object>[] FailPublication()
    {
        object payload = new();
        var payloadProbe = new WeakReference<object>(payload, trackResurrection: true);
        WeakReference<object>? ownerProbe = null;
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            SharedStateOwner.Publish(payload, () => GC.KeepAlive(payload), owner =>
            {
                ownerProbe = new(owner, trackResurrection: true);
                throw new InvalidOperationException("Expected publication failure.");
            }));
        return [payloadProbe, ownerProbe ?? throw new InvalidOperationException("Publication was not attempted.")];
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void NativeRelease_ClearsPayloadEvenWhenCallbackThrows(bool throws)
    {
        var retired = VerifyNativeRelease(throws);
        AssertCollected(retired);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static WeakReference<object>[] VerifyNativeRelease(bool throws)
    {
        var (owner, payload) = RetireNativeOwner(throws);
        var ownerProbe = new WeakReference<object>(owner, trackResurrection: true);
        try
        {
            AssertCollected([payload]);
        }
        finally
        {
            owner.Dispose();
        }
        // Keeping the retired Java peer alive must not keep its captured payload alive.
        GC.KeepAlive(owner);
        return [ownerProbe, payload];
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static (SharedStateOwner Owner, WeakReference<object> Payload) RetireNativeOwner(bool throws)
    {
        object payload = new();
        var probe = new WeakReference<object>(payload, trackResurrection: true);
        var owner = SharedStateOwner.Publish(payload, () =>
        {
            GC.KeepAlive(payload);
            if (throws)
                throw new InvalidOperationException("Expected release failure.");
        }, _ => { });
        Assert.IsTrue(owner.IsOwner);
        CollectBothRuntimes();
        var method = JNIEnv.GetMethodID(owner.Class.Handle, "onForgotten", "()V");
        if (throws)
        {
            var error = Assert.Throws<Exception>(() => JNIEnv.CallVoidMethod(owner.Handle, method));
            StringAssert.Contains(error.ToString(), "Expected release failure.");
        }
        else
            JNIEnv.CallVoidMethod(owner.Handle, method);

        // A failed release must still vacate arbitration and retire its own state.
        Assert.ThrowsExactly<InvalidOperationException>(() => _ = owner.IsOwner);
        var successor = SharedStateOwner.Publish(payload, () => { }, _ => { });
        try
        {
            Assert.IsTrue(successor.IsOwner);
        }
        finally
        {
            successor.OnAbandoned();
        }
        return (owner, probe);
    }

    internal static void CollectBothRuntimes()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        Java.Lang.JavaSystem.Gc();
        Java.Lang.JavaSystem.RunFinalization();
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    internal static void AssertCollected(WeakReference<object>[] probes)
    {
        for (int attempt = 0; attempt < 20; attempt++)
        {
            CollectBothRuntimes();
            if (probes.All(static probe => !IsAlive(probe)))
                return;
            Thread.Sleep(50);
        }
        Assert.Fail("Retired observer or captured payload remained rooted after managed and Java GC: "
            + string.Join(", ", probes.Select((probe, index) => $"{index}={DescribeProbe(probe)}")));
    }

    internal static WeakReference<object>[] OnRetiredThread(Func<WeakReference<object>[]> body)
    {
        // Let the allocating stack disappear before collection; conservative
        // stack roots must not be mistaken for native ownership in Release.
        WeakReference<object>[]? result = null;
        ExceptionDispatchInfo? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                result = body();
            }
            catch (Exception error)
            {
                failure = ExceptionDispatchInfo.Capture(error);
            }
        });
        thread.Start();
        thread.Join();
        failure?.Throw();
        return result ?? throw new InvalidOperationException("Lifetime probe thread returned no result.");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static string DescribeProbe(WeakReference<object> probe) =>
        probe.TryGetTarget(out var target) ? target.GetType().FullName ?? "unknown type" : "collected";

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool IsAlive(WeakReference<object> probe) => probe.TryGetTarget(out _);
}
