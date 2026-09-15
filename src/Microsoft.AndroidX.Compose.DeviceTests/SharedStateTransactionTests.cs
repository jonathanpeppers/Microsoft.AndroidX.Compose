using Android.Runtime;
using AndroidX.Compose;
using AndroidX.Compose.Material3;
using AndroidX.Compose.Runtime;
using Kotlin.Jvm.Functions;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Checks native shared state against uncommitted composition changes.</summary>
[TestClass]
[DoNotParallelize]
public class SharedStateTransactionTests
{
    [TestMethod]
    public void AbandonedOwnerReplacement_DoesNotReleaseCommittedOwner()
    {
        using var applier = new StateOnlyApplier();
        using var recomposer = new Recomposer(Kotlin.Coroutines.EmptyCoroutineContext.Instance
            ?? throw new InvalidOperationException("Empty coroutine context was unavailable."));
        var composition = CompositionKt.ControlledComposition(applier, recomposer)
            ?? throw new InvalidOperationException("Controlled composition was unavailable.");
        var first = new DrawerStateHolder();
        var second = new DrawerStateHolder(DrawerValue.Open);
        var selected = first;
        using var content = new ComposableLambda2(composer =>
        {
            bool allow = ReferenceEquals(selected, second);
            composer.RememberDrawerState(selected, _ => allow);
        });
        try
        {
            composition.ComposeContent(content);
            Assert.IsNotNull(first.Jvm);
            composition.AbandonChanges();
            Assert.IsNull(first.Jvm, "Initially abandoned owner retained a published peer.");

            composition.ComposeContent(content);
            Apply();
            var firstPeer = first.Jvm;
            Assert.IsNotNull(firstPeer);
            selected = second;
            composition.ComposeContent(content);
            Assert.AreSame(firstPeer, first.Jvm, "Speculation released the committed owner.");
            Assert.AreNotSame(firstPeer, second.Jvm);
            Assert.IsTrue(second.IsOpen);
            Assert.IsFalse(InvokeNativeConfirm(first), "Replacement altered the previous native callback.");
            Assert.IsTrue(InvokeNativeConfirm(second), "Replacement factory did not receive its initial policy.");
            composition.AbandonChanges();
            Assert.IsNull(second.Jvm, "Abandoned replacement retained its native peer.");
            Assert.AreSame(firstPeer, first.Jvm);
            Assert.IsTrue(first.IsClosed);
            Assert.IsTrue(second.IsOpen);

            selected = first;
            composition.ComposeContent(content);
            Apply();
            Assert.AreSame(firstPeer, first.Jvm, "Returning to committed content replaced its peer.");
            selected = second;
            composition.ComposeContent(content);
            Apply();
            Assert.IsNull(first.Jvm);
            Assert.IsTrue(second.IsOpen);
            Assert.IsTrue(InvokeNativeConfirm(second));
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
    public void AbandonedRecomposition_DoesNotPublishConfirmCallback()
    {
        using var applier = new StateOnlyApplier();
        using var recomposer = new Recomposer(Kotlin.Coroutines.EmptyCoroutineContext.Instance
            ?? throw new InvalidOperationException("Empty coroutine context was unavailable."));
        var composition = CompositionKt.ControlledComposition(applier, recomposer)
            ?? throw new InvalidOperationException("Controlled composition was unavailable.");
        var state = new DrawerStateHolder();
        bool allow = false;
        int applied = 0;
        using var content = new ComposableLambda2(composer =>
        {
            bool renderPolicy = allow;
            composer.RememberDrawerState(state, _ => renderPolicy);
            composer.SideEffect(() => applied++);
        });
        try
        {
            composition.ComposeContent(content);
            Assert.IsFalse(InvokeNativeConfirm(state), "Initial native creation must receive the veto immediately.");
            Apply();
            var peer = state.Jvm;
            Assert.AreEqual(1, applied);

            allow = true;
            composition.ComposeContent(content);
            Assert.AreSame(peer, state.Jvm);
            Assert.IsFalse(InvokeNativeConfirm(state), "Uncommitted render changed the committed veto.");
            composition.AbandonChanges();
            Assert.AreEqual(1, applied, "Abandoned render published side effects.");
            Assert.IsFalse(InvokeNativeConfirm(state), "Abandonment lost the last committed veto.");

            composition.ComposeContent(content);
            Apply();
            Assert.AreSame(peer, state.Jvm);
            Assert.IsTrue(InvokeNativeConfirm(state), "Committed recomposition did not publish the new policy.");
            Assert.AreEqual(2, applied);
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

    static bool InvokeNativeConfirm(DrawerStateHolder state)
    {
        var peer = state.Jvm ?? throw new InvalidOperationException("Drawer peer is not bound.");
        IntPtr local = IntPtr.Zero;
        try
        {
            var type = JNIEnv.FindClass("androidx/compose/material3/DrawerState");
            var method = JNIEnv.GetMethodID(type, "getConfirmStateChange$material3", "()Lkotlin/jvm/functions/Function1;");
            local = JNIEnv.CallObjectMethod(peer.Handle, method);
            var callback = Java.Lang.Object.GetObject<IFunction1>(local, JniHandleOwnership.DoNotTransfer)
                ?? throw new InvalidOperationException("Drawer native confirm callback is unavailable.");
            return callback.Invoke(DrawerValue.Open) is Java.Lang.Boolean value
                ? value.BooleanValue()
                : throw new InvalidOperationException("Drawer native confirm callback did not return Boolean.");
        }
        finally
        {
            JNIEnv.DeleteLocalRef(local);
            GC.KeepAlive(peer);
        }
    }
}
