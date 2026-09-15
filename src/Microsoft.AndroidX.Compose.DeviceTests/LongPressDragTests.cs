using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using AndroidX.Compose;
using Modifier = AndroidX.Compose.Modifier;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Real MotionEvent long-press/drag/release, cancellation, and native identity regressions.</summary>
[TestClass]
[DoNotParallelize]
public class LongPressDragTests
{
    static TestInstrumentation Runner => TestInstrumentation.Current
        ?? throw new InvalidOperationException("Pointer tests require native instrumentation.");

    [TestMethod]
    public void NullContractsAndKeys_PreserveManagedEqualityWithoutStringCoercion()
    {
        Assert.IsTrue(new Tooltip { Tip = new Text("tip"), Anchor = new Text("anchor") }.EnableUserInput);
#pragma warning disable CS8625
        Assert.ThrowsExactly<ArgumentNullException>(() => Modifier.PointerInput(null));
        Assert.ThrowsExactly<ArgumentNullException>(() => Modifier.DetectDragGesturesAfterLongPress(null));
        Assert.ThrowsExactly<ArgumentNullException>(() => ModifierExtensions.DetectDragGesturesAfterLongPress(null, _ => { }));
#pragma warning restore CS8625
        using var a = ModifierExtensions.BoxPointerInputKey((1, 2));
        using var b = ModifierExtensions.BoxPointerInputKey((1, 2));
        using var c = ModifierExtensions.BoxPointerInputKey("(1, 2)");
        Assert.IsNotNull(a);
        Assert.IsTrue(a.Equals(b));
        Assert.IsFalse(a.Equals(c));
        using var handler = new LongPressDragGestureBlock(new(_ => { }, null, null, null));
        Assert.AreEqual(Modifier.PointerInput(handler, 1).StructuralKey,
            Modifier.Companion.PointerInput(handler, 1).StructuralKey);
        Assert.AreNotEqual(Modifier.PointerInput(handler, 1).StructuralKey,
            Modifier.PointerInput(handler, 2).StructuralKey);
    }

    [TestMethod]
    public async Task NativeLongPress_MovesRecomposesReleasesAndKeepsEveryPeer()
    {
        var activity = await Start();
        long down = 0;
        try
        {
            down = Touch(activity, MotionEventActions.Down);
            Touch(activity, MotionEventActions.Up, down);
            down = 0;
            await Frames(activity);
            Assert.AreEqual(0, activity.StartCount, "A short tap must not start recording.");
            Assert.AreEqual(0, activity.EndCount);

            var original = Require(activity.Handler);
            var start = original.Start;
            var drag = original.Drag;
            var end = original.End;
            var cancel = original.Cancel;
            down = Touch(activity, MotionEventActions.Down);
            await activity.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Touch(activity, MotionEventActions.Move, down, 25, -12);
            await Frames(activity);
            int passes = activity.Passes;
            Runner.RunOnMainSync(() => activity.Version.Value = 7);
            await Frames(activity);
            Assert.IsTrue(activity.Passes > passes);
            Assert.AreEqual(1, activity.StartCount);
            Assert.AreEqual(0, activity.CancelCount);
            var current = Require(activity.Handler);
            Assert.IsTrue(JNIEnv.IsSameObject(original.Handle, current.Handle));
            Assert.IsTrue(JNIEnv.IsSameObject(start.Handle, current.Start.Handle));
            Assert.IsTrue(JNIEnv.IsSameObject(drag.Handle, current.Drag.Handle));
            Assert.IsTrue(JNIEnv.IsSameObject(end.Handle, current.End.Handle));
            Assert.IsTrue(JNIEnv.IsSameObject(cancel.Handle, current.Cancel.Handle));
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Java.Lang.JavaSystem.Gc();
            Touch(activity, MotionEventActions.Move, down, -15, 7);
            await Frames(activity);
            Assert.AreEqual(7, activity.LastVersion, "The active native coroutine must call the newly committed delegate.");
            Assert.AreEqual(-15f, activity.X.Value, 0.01f);
            Assert.AreEqual(7f, activity.Y.Value, 0.01f);
            Assert.IsTrue(activity.MoveCount >= 2);
            Touch(activity, MotionEventActions.Up, down, -15, 7);
            down = 0;
            await activity.Ended.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.AreEqual(1, activity.EndCount);
            Assert.AreEqual(0, activity.CancelCount);
            Assert.AreEqual(7, activity.LastVersion);
            global::Android.Util.Log.Info("Pointer337",
                $"identity pid={(global::Android.OS.Process.MyPid())} handler={Java.Lang.JavaSystem.IdentityHashCode(current)} " +
                $"start={Java.Lang.JavaSystem.IdentityHashCode(start)} drag={Java.Lang.JavaSystem.IdentityHashCode(drag)} " +
                $"end={Java.Lang.JavaSystem.IdentityHashCode(end)} cancel={Java.Lang.JavaSystem.IdentityHashCode(cancel)} passes={activity.Passes}");
        }
        finally
        {
            if (down != 0) Touch(activity, MotionEventActions.Cancel, down);
            await Finish(activity);
        }
    }

    [TestMethod]
    public async Task NativeCancellation_KeyRemovalMotionCancelAndDisposal_DoNotRelease()
    {
        var activity = await Start();
        long down = 0;
        bool finished = false;
        try
        {
            for (int mode = 0; mode < 4; mode++)
            {
                activity.Arm();
                down = Touch(activity, MotionEventActions.Down);
                await activity.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
                var old = Require(activity.Handler);
                if (mode == 0)
                {
                    Runner.RunOnMainSync(() => activity.Key.Value++);
                    await Frames(activity);
                    Assert.IsFalse(JNIEnv.IsSameObject(old.Handle, Require(activity.Handler).Handle));
                }
                else if (mode == 1)
                {
                    Runner.RunOnMainSync(() => activity.Visible.Value = false);
                    await Frames(activity);
                }
                else if (mode == 2)
                {
                    Touch(activity, MotionEventActions.Cancel, down);
                    down = 0;
                }
                else
                {
                    await Finish(activity);
                    finished = true;
                    down = 0;
                }
                await activity.Cancelled.Task.WaitAsync(TimeSpan.FromSeconds(5));
                Assert.AreEqual(mode + 1, activity.CancelCount);
                Assert.AreEqual(0, activity.EndCount, "Cancellation must not masquerade as release.");
                if (down != 0)
                {
                    Touch(activity, MotionEventActions.Up, down);
                    down = 0;
                }
                if (mode == 1)
                    Runner.RunOnMainSync(() => activity.Visible.Value = true);
                if (!finished) await Frames(activity);
            }
            Assert.AreEqual(4, activity.StartCount);
        }
        finally
        {
            if (!finished)
            {
                if (down != 0) Touch(activity, MotionEventActions.Cancel, down);
                await Finish(activity);
            }
        }
    }

    [TestMethod]
    public async Task JetchatRecordButton_PinnedThresholdsAndReleaseUseRealNativeInput()
    {
        var activity = await Start(recording: true);
        long down = 0;
        try
        {
            float density = (global::Android.Content.Res.Resources.System?.DisplayMetrics
                ?? throw new InvalidOperationException("No density for recording acceptance.")).Density;
            down = Touch(activity, MotionEventActions.Down);
            Touch(activity, MotionEventActions.Up, down);
            down = 0;
            await Frames(activity);
            Assert.IsFalse(activity.Recording.Value);
            Assert.AreEqual(0, activity.StartCount);
            down = Touch(activity, MotionEventActions.Down);
            await activity.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.IsTrue(activity.Recording.Value);
            Touch(activity, MotionEventActions.Move, down, -199 * density, 0);
            await Frames(activity);
            Assert.IsTrue(activity.Recording.Value);
            Assert.AreEqual(-199 * density, activity.Swipe.Value, 0.1f);
            Touch(activity, MotionEventActions.Up, down, -199 * density, 0);
            down = 0;
            await activity.Ended.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await Frames(activity);
            Assert.AreEqual(1, activity.EndCount);
            Assert.IsFalse(activity.Recording.Value);

            activity.Arm();
            down = Touch(activity, MotionEventActions.Down);
            await activity.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Touch(activity, MotionEventActions.Move, down, -201 * density, 81 * density);
            await Frames(activity);
            Assert.IsTrue(activity.Recording.Value, "Vertical displacement outside the corridor must prevent swipe cancellation.");
            Assert.AreEqual(0, activity.CancelCount);
            Touch(activity, MotionEventActions.Move, down, -201 * density, 79 * density);
            await activity.Cancelled.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Touch(activity, MotionEventActions.Up, down, -201 * density, 79 * density);
            down = 0;
            await Frames(activity);
            Assert.IsFalse(activity.Recording.Value);
            Assert.AreEqual(1, activity.CancelCount);
            Assert.AreEqual(1, activity.EndCount, "Release after threshold cancellation must not commit.");
        }
        finally
        {
            if (down != 0) Touch(activity, MotionEventActions.Cancel, down);
            await Finish(activity);
        }
    }

    static async Task<LongPressDragTestActivity> Start(bool recording = false)
    {
        LongPressDragTestActivity.Created = LongPressDragTestActivity.NewSource<LongPressDragTestActivity>();
        using var intent = new Intent(global::Android.App.Application.Context, typeof(LongPressDragTestActivity));
        intent.PutExtra("recording", recording);
        intent.AddFlags(ActivityFlags.NewTask);
        Runner.RunOnMainSync(() => global::Android.App.Application.Context.StartActivity(intent));
        var activity = await LongPressDragTestActivity.Created.Task.WaitAsync(TimeSpan.FromSeconds(10));
        try
        {
            await activity.Focused.Task.WaitAsync(TimeSpan.FromSeconds(10));
            await Frames(activity);
            Assert.IsTrue(Require(activity.Owner).IsAttachedToWindow);
            Assert.IsTrue(activity.CenterX > 0 && activity.CenterY > 0);
            return activity;
        }
        catch
        {
            await Finish(activity);
            throw;
        }
    }

    static async Task Finish(LongPressDragTestActivity activity)
    {
        Runner.RunOnMainSync(activity.Finish);
        await activity.Destroyed.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Runner.WaitForIdleSync();
    }

    static long Touch(LongPressDragTestActivity activity, MotionEventActions action, long down = 0,
        float x = 0, float y = 0)
    {
        Assert.IsTrue(activity.HasWindowFocus, "Refusing to inject into a foreign input owner.");
        long now = SystemClock.UptimeMillis();
        if (down == 0) down = now;
        using var motion = MotionEvent.Obtain(down, now, action, activity.CenterX + x, activity.CenterY + y, 0)
            ?? throw new InvalidOperationException("Could not create native pointer event.");
        motion.SetSource(InputSourceType.Touchscreen);
        Runner.SendPointerSync(motion);
        return down;
    }

    static async Task Frames(LongPressDragTestActivity activity)
    {
        Runner.WaitForIdleSync();
        var complete = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var decor = Require(activity.Window?.DecorView);
        using var last = new Java.Lang.Runnable(() => complete.TrySetResult());
        using var first = new Java.Lang.Runnable(() => decor.PostOnAnimation(last));
        Runner.RunOnMainSync(() => decor.PostOnAnimation(first));
        await complete.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Runner.WaitForIdleSync();
    }

    static T Require<T>(T? value) where T : class =>
        value ?? throw new InvalidOperationException($"Missing {typeof(T).Name} in pointer-input test.");
}
