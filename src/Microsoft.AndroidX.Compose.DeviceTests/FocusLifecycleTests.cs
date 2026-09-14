using Android.Content;
using Android.Views;
using Android.Views.Accessibility;
using AndroidX.Compose;
using AndroidX.Compose.UI.Focus;
using FocusState = AndroidX.Compose.FocusState;
using Modifier = AndroidX.Compose.Modifier;
using Action = System.Action;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Native focus, IME, semantics, root attachment, and owner-lifetime regressions.</summary>
[TestClass]
[DoNotParallelize]
public class FocusLifecycleTests
{
    static TestInstrumentation Runner => TestInstrumentation.Current
        ?? throw new InvalidOperationException("Focus tests require the active native instrumentation.");

    [TestMethod]
    public void ApiContracts_RejectMissingCompositionAndPreserveStructuralKeys()
    {
#pragma warning disable CN5009, CS8625 // Deliberately exercise the runtime misuse diagnostics.
        Func<IFocusManager> read = LocalFocusManager.Current;
        var error = Assert.ThrowsExactly<InvalidOperationException>(() => read());
        StringAssert.Contains(error.Message, "No composer is active");
        Assert.ThrowsExactly<ArgumentNullException>(() => LocalFocusManager.Current(null));
        Assert.ThrowsExactly<ArgumentNullException>(() => LocalFocusManager.Provides(null));
        Assert.ThrowsExactly<ArgumentNullException>(() => FocusManagerExtensions.ClearFocus(null));
#pragma warning restore CN5009, CS8625
        Assert.AreEqual(Modifier.FocusTarget().StructuralKey,
            Modifier.Companion.FocusTarget().StructuralKey);
        Assert.AreNotEqual(Modifier.FocusTarget().StructuralKey,
            Modifier.Focusable().StructuralKey);
    }

    [TestMethod]
    public async Task EditorSelector_HandoffPreservesImeSemanticsAndDoesNotOscillate()
    {
        var activity = await Start();
        try
        {
            var editor = Require(activity.Editor);
            var panel = Require(activity.Panel);
            var manager = Require(activity.Manager);
            Assert.IsTrue(SamePeer(manager, Require(activity.ImplicitManager)));
            Assert.IsTrue(SamePeer(manager, Require(activity.ProvidedManager)));

            await Act(activity, editor.RequestFocus);
            await Ime(activity, true);
            Assert.IsTrue(activity.EditorState.IsFocused);
            using (var label = Find("Native focus editor"))
            // Compose's merged editor exposes its content description as a synthetic leaf.
            using (var node = label.Editable ? Copy(label) : label.Parent
                ?? throw new InvalidOperationException(
                    "The editor description had no native parent: " + Describe(label)))
            {
                Assert.AreEqual(label.WindowId, node.WindowId);
                Assert.AreEqual("net.compose.devicetests", node.PackageName);
                Assert.IsTrue(node.Editable, Describe(node));
                Assert.IsTrue(node.Focusable, Describe(node));
                Assert.IsTrue(node.Focused, Describe(node));
            }

            int requests = activity.Requests;
            await Act(activity, () => activity.Selector.Value = 1);
            await Ime(activity, false);
            Assert.IsFalse(activity.EditorState.IsFocused);
            Assert.IsTrue(activity.PanelState.IsFocused);
            Assert.AreEqual(requests + 1, activity.Requests);
            using (var node = Find("Native focus panel"))
            {
                Assert.IsFalse(node.Editable);
                Assert.IsFalse(node.Focusable, "FocusTarget must not add Focusable semantics.");
                Assert.IsFalse(node.Focused, "Input focus must not masquerade as accessibility focus semantics.");
                Assert.IsFalse(node.AccessibilityFocused);
            }

            await Act(activity, () => Require(activity.Child).RequestFocus());
            Assert.IsTrue(activity.ChildState.IsFocused);
            Assert.IsTrue(activity.PanelState.HasFocus);
            Assert.IsFalse(activity.PanelState.IsFocused);
            int events = activity.PanelEvents.Count;
            requests = activity.Requests;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Java.Lang.JavaSystem.Gc();
            bool captured = false;
            await Act(activity, () => captured = Require(activity.Child).CaptureFocus());
            Assert.IsTrue(captured);
            Assert.IsTrue(activity.ChildState.IsCaptured, "The native-owned callback did not survive GC.");
            await Act(activity, () => Require(activity.Child).FreeFocus());
            Assert.IsFalse(activity.ChildState.IsCaptured);
            for (int i = 0; i < 3; i++)
            {
                int passes = activity.Passes;
                await Act(activity, () => activity.Tick.Value++);
                Assert.IsTrue(activity.Passes > passes, "The unrelated input did not actually recompose.");
                Assert.IsTrue(activity.ChildState.IsFocused);
                Assert.AreEqual(requests, activity.Requests, "Unrelated recomposition stole focus.");
                Assert.AreEqual(events, activity.PanelEvents.Count, "Focus oscillated during recomposition.");
                Assert.AreSame(editor, activity.Editor);
                Assert.AreSame(panel, activity.Panel);
            }

            await Act(activity, editor.RequestFocus);
            await Ime(activity, true);
            Assert.AreEqual(0, activity.Selector.Value);
            Assert.IsTrue(activity.EditorState.IsFocused);
            Assert.IsTrue(activity.EditorEvents.Any(s => !s.IsFocused));
            Assert.IsTrue(activity.EditorEvents.Count(s => s.IsFocused) >= 2);
            await Act(activity, () => manager.ClearFocus());
            await Ime(activity, false);
            Assert.IsFalse(activity.EditorState.IsFocused);

            await Act(activity, () => activity.Selector.Value = 2);
            Assert.AreEqual(requests, activity.Requests, "Requested an unattached emoji target.");
            await Act(activity, () => activity.Selector.Value = 1);
            Assert.IsTrue(activity.PanelState.IsFocused);
            Runner.SendKeyDownUpSync(Keycode.Back);
            await Settle(activity);
            Assert.AreEqual(0, activity.Selector.Value, "Native Back did not dismiss the selector.");
            await Act(activity, () => activity.Selector.Value = 1);
            Assert.IsTrue(activity.PanelState.IsFocused, "Re-added target was not attached.");
        }
        finally
        {
            await Finish(activity);
        }
    }

    [TestMethod]
    public async Task ClearFocus_DefaultHonorsCapture_ForceReleasesAndNewOwnerWorks()
    {
        var activity = await Start();
        IFocusManager? previousManager = null;
        try
        {
            await Act(activity, () => activity.Selector.Value = 1);
            previousManager = Require(activity.Manager);
            bool captured = false;
            await Act(activity, () => captured = Require(activity.Panel).CaptureFocus());
            Assert.IsTrue(captured);
            Assert.AreEqual(new FocusState(true, true, true), activity.PanelState);
            await Act(activity, () => previousManager.ClearFocus());
            Assert.IsTrue(activity.PanelState.IsCaptured);
            await Act(activity, () => previousManager.ClearFocus(force: false));
            Assert.IsTrue(activity.PanelState.IsCaptured);
            await Act(activity, () => previousManager.ClearFocus(force: true));
            Assert.AreEqual(new FocusState(false, false, false), activity.PanelState);
            Assert.IsTrue(Require(activity.Owner).IsAttachedToWindow);
            Assert.IsTrue(activity.HasWindowFocus);
            await Act(activity, () => Require(activity.Editor).RequestFocus());
            await Ime(activity, true);
            Assert.IsTrue(activity.EditorState.IsFocused, "Clear lost the root's ability to focus.");
        }
        finally
        {
            await Finish(activity);
        }

        var replacement = await Start();
        try
        {
            Assert.IsFalse(SamePeer(Require(previousManager), Require(replacement.Manager)));
            await Act(replacement, () => Require(replacement.Editor).RequestFocus());
            await Ime(replacement, true);
            await Act(replacement, () => replacement.Selector.Value = 1);
            await Ime(replacement, false);
            Assert.IsTrue(replacement.PanelState.IsFocused);
        }
        finally
        {
            await Finish(replacement);
        }
    }

    static async Task<FocusTestActivity> Start()
    {
        using var intent = new Intent(global::Android.App.Application.Context, typeof(FocusTestActivity));
        intent.AddFlags(ActivityFlags.NewTask);
        Runner.RunOnMainSync(() => global::Android.App.Application.Context.StartActivity(intent));
        try
        {
            await Until(() => Volatile.Read(ref FocusTestActivity.Current) is { Resumed: true, HasWindowFocus: true },
                "Activity did not resume with native window focus.");
            var activity = Require(Volatile.Read(ref FocusTestActivity.Current));
            await Settle(activity);
            Assert.IsTrue(Require(activity.Owner).IsAttachedToWindow);
            Assert.IsTrue(Require(activity.Owner).IsLaidOut);
            Assert.IsTrue(activity.Passes > 0);
            return activity;
        }
        catch
        {
            if (Volatile.Read(ref FocusTestActivity.Current) is { } activity)
                await Finish(activity);
            throw;
        }
    }

    static async Task Finish(FocusTestActivity activity)
    {
        Runner.RunOnMainSync(activity.Finish);
        await activity.Destroyed.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Runner.WaitForIdleSync();
    }

    static async Task Act(FocusTestActivity activity, Action action)
    {
        Runner.RunOnMainSync(action);
        await Settle(activity);
    }

    static async Task Settle(FocusTestActivity activity)
    {
        Runner.WaitForIdleSync();
        var frame = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var secondFrame = new Java.Lang.Runnable(() => frame.TrySetResult());
        using var firstFrame = new Java.Lang.Runnable(() =>
            Require(activity.Window?.DecorView).PostOnAnimation(secondFrame));
        Runner.RunOnMainSync(() =>
        {
            var decor = activity.Window?.DecorView
                ?? throw new InvalidOperationException("Focus activity has no decor view.");
            decor.PostOnAnimation(firstFrame);
        });
        await frame.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Runner.WaitForIdleSync();
        Require(Runner.UiAutomation).WaitForIdle(100, 5000);
    }

    static async Task Ime(FocusTestActivity activity, bool expected)
    {
        await Until(() =>
        {
            bool visible = false;
            Runner.RunOnMainSync(() =>
            {
                if (!OperatingSystem.IsAndroidVersionAtLeast(30))
                    throw new PlatformNotSupportedException("Native IME focus regression requires Android 11 or later.");
                var insets = Require(activity.Owner).RootWindowInsets
                    ?? throw new InvalidOperationException("Focus owner has no attached root insets.");
                visible = insets.IsVisible(global::Android.Views.WindowInsets.Type.Ime());
            });
            return visible == expected;
        }, $"Native IME visibility did not become {expected}.");
        await Settle(activity);
    }

    static AccessibilityNodeInfo Find(string description)
    {
        using var root = Require(Runner.UiAutomation).RootInActiveWindow
            ?? throw new InvalidOperationException("No active native accessibility root.");
        Assert.AreEqual("net.compose.devicetests", root.PackageName,
            "The active accessibility root belongs to another app.");
        return FindIn(root, node => node.ContentDescription == description)
            ?? throw new InvalidOperationException(
                $"Accessibility node '{description}' was missing: {Describe(root)}");
    }

    static AccessibilityNodeInfo? FindIn(
        AccessibilityNodeInfo node, Func<AccessibilityNodeInfo, bool> predicate)
    {
        var matches = new List<AccessibilityNodeInfo>();
        try
        {
            FindAllIn(node, predicate, matches);
            if (matches.Count > 1)
                throw new InvalidOperationException(
                    $"Ambiguous accessibility subtree ({matches.Count} matches): {Describe(node)}");
            if (matches.Count == 0)
                return null;
            var result = matches[0];
            matches.Clear();
            return result;
        }
        finally
        {
            foreach (var match in matches)
                match.Dispose();
        }
    }

    static void FindAllIn(
        AccessibilityNodeInfo node,
        Func<AccessibilityNodeInfo, bool> predicate,
        List<AccessibilityNodeInfo> matches)
    {
        if (predicate(node))
            matches.Add(Copy(node));
        for (int i = 0; i < node.ChildCount; i++)
        {
            using var child = node.GetChild(i);
            if (child is not null)
                FindAllIn(child, predicate, matches);
        }
    }

    static AccessibilityNodeInfo Copy(AccessibilityNodeInfo node) =>
        Require(OperatingSystem.IsAndroidVersionAtLeast(33)
            ? new AccessibilityNodeInfo(node)
            : AccessibilityNodeInfo.Obtain(node));

    static string Describe(AccessibilityNodeInfo node)
    {
        var text = new System.Text.StringBuilder()
            .Append('[').Append(node.ClassName).Append(" window=").Append(node.WindowId)
            .Append(" id=").Append(node.ViewIdResourceName)
            .Append(" uniqueId=").Append(OperatingSystem.IsAndroidVersionAtLeast(33) ? node.UniqueId : null)
            .Append(" description=").Append(node.ContentDescription)
            .Append(" text=").Append(node.Text).Append(" editable=").Append(node.Editable)
            .Append(" focusable=").Append(node.Focusable).Append(" focused=").Append(node.Focused).Append(']');
        for (int i = 0; i < node.ChildCount; i++)
        {
            using var child = node.GetChild(i);
            if (child is not null)
                text.Append(Describe(child));
        }
        return text.ToString();
    }

    static async Task Until(Func<bool> predicate, string message)
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (!predicate())
        {
            if (deadline.IsCancellationRequested)
                Assert.Fail(message);
            await Task.Delay(20);
        }
    }

    static T Require<T>(T? value) where T : class =>
        value ?? throw new InvalidOperationException($"Focus test {typeof(T).Name} was unavailable.");

    static bool SamePeer(IFocusManager first, IFocusManager second)
    {
        try
        {
            return global::Android.Runtime.JNIEnv.IsSameObject(
                ((Java.Lang.Object)first).Handle, ((Java.Lang.Object)second).Handle);
        }
        finally
        {
            GC.KeepAlive(first);
            GC.KeepAlive(second);
        }
    }
}
