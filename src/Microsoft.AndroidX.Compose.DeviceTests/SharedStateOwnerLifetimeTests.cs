using System.Reflection;
using System.Runtime.CompilerServices;
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
        var retired = ExerciseNativeObservers(abandon);
        AssertCollected(retired);
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
                        var probe = new WeakReference<object>(owner);
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
        var retired = FailPublication();
        AssertCollected(retired);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static WeakReference<object>[] FailPublication()
    {
        object payload = new();
        var payloadProbe = new WeakReference<object>(payload);
        WeakReference<object>? ownerProbe = null;
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            SharedStateOwner.Publish(payload, () => GC.KeepAlive(payload), owner =>
            {
                ownerProbe = new(owner);
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
        var ownerProbe = new WeakReference<object>(owner);
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
        var probe = new WeakReference<object>(payload);
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

    static void AssertCollected(WeakReference<object>[] probes)
    {
        for (int attempt = 0; attempt < 20; attempt++)
        {
            CollectBothRuntimes();
            if (probes.All(static probe => !IsAlive(probe)))
                return;
            Thread.Sleep(50);
        }
        Assert.Fail("Retired observer or captured payload remained rooted after managed and Java GC.");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool IsAlive(WeakReference<object> probe) => probe.TryGetTarget(out _);
}
