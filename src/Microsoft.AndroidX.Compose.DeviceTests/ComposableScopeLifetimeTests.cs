using System.Runtime.CompilerServices;
using AndroidX.Compose;
using AndroidX.Compose.Runtime;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Verifies that raw callback scope handles retain their managed owner during collection.</summary>
[TestClass]
[DoNotParallelize]
public class ComposableScopeLifetimeTests
{
    /// <summary>Collects during an active callback before resolving the borrowed RowScope handle.</summary>
    [TestMethod]
    [DataRow(3, false)]
    [DataRow(3, true)]
    [DataRow(4, false)]
    [DataRow(4, true)]
    public void RawScope_RemainsAliveUntilCallbackUnwinds(int arity, bool throws)
    {
        using var applier = new IdentityTestApplier();
        using var recomposer = new Recomposer(Kotlin.Coroutines.EmptyCoroutineContext.Instance
            ?? throw new InvalidOperationException("Empty coroutine context unavailable."));
        var composition = CompositionKt.ControlledComposition(applier, recomposer);
        var weak = new WeakReference<Java.Lang.Object?>(null);
        try
        {
            IComposer? captured = null;
            composition.ComposeContent(new ComposableLambda2(c => captured = c));
            composition.ApplyChanges();
            var composer = captured ?? throw new InvalidOperationException("Native composer was not captured.");
            int calls = 0;
            void Body(IntPtr handle, IComposer current)
            {
                Assert.AreSame(composer, current);
                Collect();
                var observedOwner = AssertBorrowedHandle(weak, handle);
                try
                {
                    using var scope = RenderContext.PushScope(handle, ScopeKind.Row);
                    Assert.IsNotNull(Modifier.AlignByBaseline().Build(),
                        "The live scope must still support the bound RowScope alignment operation.");
                    calls++;
                    if (throws)
                        throw new InvalidOperationException("deliberate scope callback failure");
                }
                finally
                {
                    // Root only after the collection under test; never use an obsolete reference.
                    GC.KeepAlive(observedOwner);
                }
            }

            using Java.Lang.Object callback = arity == 3
                ? new ComposableLambda3(Body)
                : new ComposableLambda4((handle, _, current) => Body(handle, current));
            if (throws)
            {
                var error = Assert.ThrowsExactly<InvalidOperationException>(() => Invoke(callback, composer, weak));
                Assert.AreEqual("deliberate scope callback failure", error.Message);
            }
            else
            {
                Invoke(callback, composer, weak);
            }
            Assert.AreEqual(1, calls);
            Assert.AreEqual(ScopeKind.None, RenderContext.CurrentScopeKind);
            Assert.ThrowsExactly<InvalidOperationException>(() => _ = ComposableContext.Current);
        }
        finally
        {
            composition.Dispose();
        }
    }

    /// <summary>Checks that a strongly retained native scope keeps its particular JNI reference stable.</summary>
    [TestMethod]
    public void NativeSingleton_StrongOwnerPreservesBorrowedHandle()
    {
        var weak = new WeakReference<Java.Lang.Object?>(null);
        var owner = CreateScope(weak);
        var handle = owner.Handle;
        try
        {
            Collect();
            AssertBorrowedHandle(weak, handle);
        }
        finally
        {
            GC.KeepAlive(owner);
        }
    }

    /// <summary>Records native-rooted peer survival separately from its JNI reference stability.</summary>
    [TestMethod]
    public void NativeSingleton_UnrootedOwnerReportsReferenceState()
    {
        var weak = new WeakReference<Java.Lang.Object?>(null);
        var original = CreateUnretainedScope(weak);
        Collect();
        ReportReference(weak, original);
    }

    /// <summary>Proves collection eligibility using a fresh object with no independent native root.</summary>
    [TestMethod]
    public void FreshObject_UnrootedOwnerIsCollectible()
    {
        var weak = CreateUnretainedObject();
        Collect();
        Assert.IsFalse(HasTarget(weak), "An unrooted fresh Java object must not be retained by the fixture.");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Invoke(Java.Lang.Object callback, IComposer composer, WeakReference<Java.Lang.Object?> weak)
    {
        using var changed = Java.Lang.Integer.ValueOf(0)
            ?? throw new InvalidOperationException("Boxed changed flag unavailable.");
        switch (callback)
        {
            case ComposableLambda3 fn:
                fn.Invoke(CreateScope(weak), (Java.Lang.Object)composer, changed);
                break;
            case ComposableLambda4 fn:
                fn.Invoke(CreateScope(weak), null, (Java.Lang.Object)composer, changed);
                break;
            default:
                throw new InvalidOperationException("Unexpected scope callback type.");
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static IntPtr CreateUnretainedScope(WeakReference<Java.Lang.Object?> weak) => CreateScope(weak).Handle;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static WeakReference<Java.Lang.Object?> CreateUnretainedObject() => new(new Java.Util.ArrayList());

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Java.Lang.Object CreateScope(WeakReference<Java.Lang.Object?> weak)
    {
        using var type = Java.Lang.Class.ForName(
            "androidx.compose.foundation.layout.RowScopeInstance", true,
            global::Android.App.Application.Context.ClassLoader);
        using var field = type.GetField("INSTANCE")
            ?? throw new InvalidOperationException("Native RowScopeInstance.INSTANCE field unavailable.");
        var scope = field.Get(null)
            ?? throw new InvalidOperationException("Native RowScopeInstance.INSTANCE unavailable.");
        weak.SetTarget(scope);
        return scope;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Java.Lang.Object AssertBorrowedHandle(WeakReference<Java.Lang.Object?> weak, IntPtr original)
    {
        bool survived = weak.TryGetTarget(out var peer);
        Console.WriteLine($"Scope owner survived={survived}; borrowed=0x{original:x}; current=0x{peer?.Handle ?? IntPtr.Zero:x}");
        Assert.IsTrue(survived, "The scope owner weak reference was cleared while its callback was active.");
        Assert.IsNotNull(peer, "The live scope weak reference must have a target.");
        Assert.AreNotEqual(IntPtr.Zero, peer.Handle, "The retained scope must have a live JNI reference.");
        Assert.AreEqual(original, peer.Handle,
            "The owner survived, but its JNI reference changed while the callback still held the old handle.");
        return peer;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void ReportReference(WeakReference<Java.Lang.Object?> weak, IntPtr original)
    {
        bool survived = weak.TryGetTarget(out var peer);
        Console.WriteLine($"Unrooted native singleton: owner survived={survived}; borrowed=0x{original:x}; current=0x{peer?.Handle ?? IntPtr.Zero:x}");
        // Native reachability can preserve the managed peer while the GC bridge replaces its JNI reference.
        if (peer is not null)
            Assert.AreNotEqual(IntPtr.Zero, peer.Handle, "A surviving native singleton peer must have a live reference.");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool HasTarget(WeakReference<Java.Lang.Object?> weak) => weak.TryGetTarget(out var peer) && peer is not null;

    static void Collect()
    {
        for (int i = 0; i < 3; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Java.Lang.JavaSystem.Gc();
            Java.Lang.JavaSystem.RunFinalization();
        }
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
}
