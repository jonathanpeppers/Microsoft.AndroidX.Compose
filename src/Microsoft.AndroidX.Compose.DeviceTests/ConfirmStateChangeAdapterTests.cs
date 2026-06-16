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
        var result = adapter.Invoke(null);
        Assert.AreSame(Java.Lang.Boolean.True, result);
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
    public void Sheet_Invoke_NullCallback_ReturnsTrue()
    {
        var adapter = new SheetValueConfirmStateChange();
        Assert.AreSame(Java.Lang.Boolean.True, adapter.Invoke(null));
    }
}
