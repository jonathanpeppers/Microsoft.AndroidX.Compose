using System.Runtime.ExceptionServices;
using AccessibilityNodeInfo = Android.Views.Accessibility.AccessibilityNodeInfo;

namespace Microsoft.AndroidX.Compose.DeviceTests;

internal sealed class FlowTestAdmission
{
    internal Guid InstanceId { get; } = Guid.NewGuid();
    internal bool Resumed;
    internal bool Ending;
    internal bool Admitted;
    internal int NativePid;
    internal int WindowId;
    string? _failure;

    static TestInstrumentation Runner => TestInstrumentation.Current
        ?? throw new InvalidOperationException("Flow admission requires native instrumentation.");

    internal void Paused()
    {
        Resumed = false;
        if (Admitted && !Ending)
            _failure ??= "Admitted flow activity paused.";
    }

    internal void FocusChanged(bool hasFocus, bool ownerHasFocus)
    {
        if (!hasFocus && !ownerHasFocus && Admitted && !Ending)
            _failure ??= "Admitted flow activity lost native window focus.";
    }

    internal async Task Admit(FlowOverflowTestActivity activity)
    {
        for (int attempt = 0; attempt < 150; attempt++)
        {
            bool ready = false;
            OnUi(() => ready = IsReady(activity));
            if (ready)
            {
                var automation = Runner.UiAutomation
                    ?? throw new InvalidOperationException("Flow admission has no UI automation.");
                if (OperatingSystem.IsAndroidVersionAtLeast(34))
                    Assert.IsTrue(automation.ClearCache(), "Could not refresh the initial native window.");
                using var root = automation.RootInActiveWindow;
                if (root is not null)
                {
                    Assert.AreEqual("net.compose.devicetests", root.PackageName,
                        "Initial accessibility window belongs to another application.");
                    Assert.IsTrue(root.WindowId >= 0, "Initial accessibility window has no native identity.");
                    OnUi(() =>
                    {
                        if (!IsReady(activity))
                            throw new InvalidOperationException("Flow activity lost readiness during admission.");
                        NativePid = global::Android.OS.Process.MyPid();
                        WindowId = root.WindowId;
                        Admitted = true;
                    });
                    Console.WriteLine($"FLOW_ADMISSION instance={InstanceId} pid={NativePid} window={WindowId} " +
                        "resumed=true attached=true laidOut=true focused=true finishing=false destroyed=false");
                    return;
                }
            }
            await Task.Delay(100);
        }
        throw new TimeoutException("Flow activity did not become resumed, attached, laid out and focused.");
    }

    bool IsReady(FlowOverflowTestActivity activity) =>
        Resumed && !Ending && !activity.IsFinishing && !activity.IsDestroyed && activity.HasWindowFocus &&
        activity.Owner is { IsAttachedToWindow: true, IsLaidOut: true, HasWindowFocus: true };

    internal void RequireLive(FlowOverflowTestActivity activity)
    {
        if (_failure is { } failure)
            throw new InvalidOperationException($"{failure} Instance={InstanceId}.");
        if (!Admitted || !IsReady(activity) || NativePid != global::Android.OS.Process.MyPid())
            throw new InvalidOperationException($"Flow fixture is no longer admitted. Instance={InstanceId}.");
    }

    internal AccessibilityNodeInfo AcquireRoot(FlowOverflowTestActivity activity)
    {
        OnUi(() => RequireLive(activity));
        var automation = Runner.UiAutomation
            ?? throw new InvalidOperationException("Flow input has no UI automation.");
        if (OperatingSystem.IsAndroidVersionAtLeast(34))
            Assert.IsTrue(automation.ClearCache(), "Could not refresh the native input target.");
        var root = automation.RootInActiveWindow
            ?? throw new InvalidOperationException("Flow input has no active accessibility window.");
        if (root.PackageName != "net.compose.devicetests" || root.WindowId != WindowId)
        {
            root.Dispose();
            throw new InvalidOperationException("Flow input moved to a foreign package or native window.");
        }
        return root;
    }

    internal static void OnUi(Action action)
    {
        Exception? failure = null;
        Runner.RunOnMainSync(() =>
        {
            try { action(); }
            catch (Exception error) { failure = error; }
        });
        if (failure is not null)
            ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
