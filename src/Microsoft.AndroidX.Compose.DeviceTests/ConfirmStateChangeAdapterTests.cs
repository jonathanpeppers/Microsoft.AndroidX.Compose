using Android.Runtime;
using AndroidX.Compose;
using AndroidX.Compose.Material3;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>
/// Verifies <see cref="ConfirmStateChangeAdapter{T}"/> behaviour at
/// the JCW boundary — Kotlin's <c>(T) -&gt; Boolean</c> veto callback is
/// invoked from the runtime as part of a <c>remember</c> cache key, so
/// reference identity must stay stable AND a <c>null</c> developer
/// callback must round-trip as "always allow" (<c>true</c>).
/// </summary>
[TestClass]
public class ConfirmStateChangeAdapterTests
{
    [TestMethod]
    public void Invoke_NullCallback_ReturnsTrue()
    {
        var adapter = new DrawerValueConfirmStateChange();
        Assert.IsNull(adapter.Callback);
        var result = adapter.Invoke(null) as Java.Lang.Boolean
            ?? throw new InvalidOperationException("Adapter did not return a Java Boolean.");
        Assert.IsTrue(result.BooleanValue());
    }

    [TestMethod]
    public void Callback_AssignAndReadRoundTrips()
    {
        var adapter = new DrawerValueConfirmStateChange();
        Func<DrawerValue, bool> cb = _ => false;
        adapter.Callback = cb;
        Assert.AreSame(cb, adapter.Callback);
    }

    [TestMethod]
    public void Callback_RebindPreservesPeerHandle()
    {
        var adapter = new DrawerValueConfirmStateChange();
        var handleBefore = adapter.Handle;
        adapter.Callback = _ => true;
        var handleAfter = adapter.Handle;
        Assert.AreEqual(handleBefore, handleAfter);
    }

    [TestMethod]
    public unsafe void Invoke_FromJavaDispatchesToInternalJcw()
    {
        var adapter = new DrawerValueConfirmStateChange();
        var value = DrawerValue.Open
            ?? throw new InvalidOperationException("DrawerValue.Open was unavailable.");
        var falseValue = Java.Lang.Boolean.False
            ?? throw new InvalidOperationException("Java Boolean.FALSE was unavailable.");
        DrawerValue? received = null;
        adapter.Callback = candidate =>
        {
            received = candidate;
            return false;
        };

        var cls = JNIEnv.FindClass("net/compose/DrawerValueConfirmStateChange");
        var invoke = JNIEnv.GetMethodID(
            cls,
            "invoke",
            "(Ljava/lang/Object;)Ljava/lang/Object;");
        var args = stackalloc JValue[1];
        args[0] = new JValue(value.Handle);
        IntPtr result = IntPtr.Zero;
        try
        {
            result = JNIEnv.CallObjectMethod(adapter.Handle, invoke, args);
            Assert.AreEqual(value, received);
            Assert.IsTrue(JNIEnv.IsSameObject(result, falseValue.Handle));
        }
        finally
        {
            if (result != IntPtr.Zero)
                JNIEnv.DeleteLocalRef(result);
            GC.KeepAlive(adapter);
            GC.KeepAlive(value);
            GC.KeepAlive(falseValue);
        }
    }

    [TestMethod]
    public void Sheet_Invoke_NullCallback_ReturnsTrue()
    {
        var adapter = new SheetValueConfirmStateChange();
        var result = adapter.Invoke(null) as Java.Lang.Boolean
            ?? throw new InvalidOperationException("Adapter did not return a Java Boolean.");
        Assert.IsTrue(result.BooleanValue());
    }
}
