using AndroidX.Compose;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>
/// Verifies <see cref="MutableComposableLambda0"/> and
/// <see cref="MutableComposableLambda1"/> identity stability — the JCW
/// peer is allocated once per call site by
/// <c>ComposeExtensions.RememberAction</c> and its mutable
/// <c>Target</c> is rebound on every render, so the JNI handle the
/// bridge passes stays reference-equal across recompositions and
/// Kotlin reads <see cref="ChangedBits.Static"/> for the slot.
/// </summary>
[TestClass]
public class MutableComposableLambdaTests
{
    [TestMethod]
    public void Lambda0_InvokeCallsTarget()
    {
        var counter = 0;
        var wrapper = new MutableComposableLambda0(() => counter++);
        wrapper.Invoke();
        Assert.AreEqual(1, counter);
    }

    [TestMethod]
    public void Lambda0_RebindTarget_NextInvokeUsesNewBody()
    {
        var oldCount = 0;
        var newCount = 0;
        var wrapper = new MutableComposableLambda0(() => oldCount++);
        wrapper.Invoke();
        wrapper.Target = () => newCount++;
        wrapper.Invoke();
        wrapper.Invoke();
        Assert.AreEqual(1, oldCount);
        Assert.AreEqual(2, newCount);
    }

    [TestMethod]
    public void Lambda0_RebindTargetPreservesPeerHandle()
    {
        var wrapper = new MutableComposableLambda0(() => { });
        var handleBefore = wrapper.Handle;
        wrapper.Target = () => { };
        var handleAfter = wrapper.Handle;
        Assert.AreEqual(handleBefore, handleAfter);
    }

    [TestMethod]
    public void Lambda1_InvokeCallsTargetWithArg()
    {
        Java.Lang.Object? captured = null;
        var wrapper = new MutableComposableLambda1(o => captured = o);
        var arg = new Java.Lang.String("hi");
        wrapper.Invoke(arg);
        Assert.AreSame(arg, captured);
    }

    [TestMethod]
    public void Lambda1_RebindTarget_NextInvokeUsesNewBody()
    {
        Java.Lang.Object? oldCaptured = null;
        Java.Lang.Object? newCaptured = null;
        var wrapper = new MutableComposableLambda1(o => oldCaptured = o);
        var first = new Java.Lang.String("a");
        wrapper.Invoke(first);
        wrapper.Target = o => newCaptured = o;
        var second = new Java.Lang.String("b");
        wrapper.Invoke(second);
        Assert.AreSame(first, oldCaptured);
        Assert.AreSame(second, newCaptured);
    }
}
