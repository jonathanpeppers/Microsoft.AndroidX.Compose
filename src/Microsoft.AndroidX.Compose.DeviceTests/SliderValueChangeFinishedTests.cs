using Android.Content;
using Android.OS;
using Android.Views;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>
/// Real MotionEvent drags and taps on Slider and RangeSlider: OnValueChangeFinished
/// runs once, on release, after the last onValueChange.
/// </summary>
[TestClass]
[DoNotParallelize]
public class SliderValueChangeFinishedTests
{
    static TestInstrumentation Runner => TestInstrumentation.Current
        ?? throw new InvalidOperationException("Slider pointer tests require native instrumentation.");

    [TestMethod]
    public async Task SliderDrag_FinishesOnceOnReleaseWithLatestCallback()
    {
        var activity = await Start();
        var bounds = Require(activity.SliderBounds);
        long down = 0;
        try
        {
            down = Touch(activity, MotionEventActions.Down, bounds.X(0.25f), bounds.CenterY);
            await Frames(activity);
            Touch(activity, MotionEventActions.Move, bounds.X(0.5f), bounds.CenterY, down);
            await Frames(activity);
            Touch(activity, MotionEventActions.Move, bounds.X(0.75f), bounds.CenterY, down);
            await Frames(activity);
            Assert.IsGreaterThan(0, activity.ChangeCount, "Dragging must report onValueChange.");
            Assert.AreEqual(0, activity.FinishedCount, "OnValueChangeFinished must wait for the release.");

            Runner.RunOnMainSync(() => activity.Version.Value = 2);
            await Frames(activity);
            Touch(activity, MotionEventActions.Up, bounds.X(0.75f), bounds.CenterY, down);
            down = 0;
            await Frames(activity);

            Assert.AreEqual(1, activity.FinishedCount, "A drag must finish exactly once.");
            Assert.AreEqual(activity.Value.Value, activity.FinishedValue,
                "OnValueChangeFinished must run after the last onValueChange.");
            Assert.IsGreaterThan(0.5f, activity.FinishedValue);
            Assert.AreEqual(2, activity.FinishedVersion, "The release must call the latest committed callback.");
        }
        finally
        {
            if (down != 0) Touch(activity, MotionEventActions.Cancel, bounds.X(0.75f), bounds.CenterY, down);
            await Finish(activity);
        }
    }

    [TestMethod]
    public async Task SliderTap_FinishesOnce()
    {
        var activity = await Start();
        try
        {
            var bounds = Require(activity.SliderBounds);
            long down = Touch(activity, MotionEventActions.Down, bounds.X(0.25f), bounds.CenterY);
            Touch(activity, MotionEventActions.Up, bounds.X(0.25f), bounds.CenterY, down);
            await Frames(activity);

            Assert.IsGreaterThan(0, activity.ChangeCount, "A tap must move the slider.");
            Assert.AreEqual(1, activity.FinishedCount, "A tap must finish exactly once.");
            Assert.AreEqual(activity.Value.Value, activity.FinishedValue);
        }
        finally
        {
            await Finish(activity);
        }
    }

    [TestMethod]
    public async Task RangeSliderDrag_FinishesOnceOnRelease()
    {
        var activity = await Start();
        var bounds = Require(activity.RangeBounds);
        long down = 0;
        try
        {
            // Press nearest the end thumb (0.8) and drag it right.
            down = Touch(activity, MotionEventActions.Down, bounds.X(0.8f), bounds.CenterY);
            await Frames(activity);
            Touch(activity, MotionEventActions.Move, bounds.X(0.9f), bounds.CenterY, down);
            await Frames(activity);
            Touch(activity, MotionEventActions.Move, bounds.X(0.95f), bounds.CenterY, down);
            await Frames(activity);
            Assert.IsGreaterThan(0, activity.RangeChangeCount, "Dragging must report onValueChange.");
            Assert.AreEqual(0, activity.RangeFinishedCount, "OnValueChangeFinished must wait for the release.");

            Touch(activity, MotionEventActions.Up, bounds.X(0.95f), bounds.CenterY, down);
            down = 0;
            await Frames(activity);

            Assert.AreEqual(1, activity.RangeFinishedCount, "A drag must finish exactly once.");
            Assert.AreEqual(activity.Range.Value, activity.RangeFinishedValue,
                "OnValueChangeFinished must run after the last onValueChange.");
            Assert.IsGreaterThan(0.8f, activity.Range.Value.End);
        }
        finally
        {
            if (down != 0) Touch(activity, MotionEventActions.Cancel, bounds.X(0.95f), bounds.CenterY, down);
            await Finish(activity);
        }
    }

    [TestMethod]
    public async Task SliderWithoutCallback_StillDrags()
    {
        var activity = await Start();
        var bounds = Require(activity.PlainBounds);
        long down = 0;
        try
        {
            down = Touch(activity, MotionEventActions.Down, bounds.X(0.25f), bounds.CenterY);
            await Frames(activity);
            Touch(activity, MotionEventActions.Move, bounds.X(0.75f), bounds.CenterY, down);
            await Frames(activity);
            Touch(activity, MotionEventActions.Up, bounds.X(0.75f), bounds.CenterY, down);
            down = 0;
            await Frames(activity);

            Assert.IsGreaterThan(0, activity.PlainChangeCount);
            Assert.IsGreaterThan(0.5f, activity.PlainValue.Value);
            Assert.AreEqual(0, activity.ChangeCount, "Only the dragged slider may report a change.");
            Assert.AreEqual(0, activity.RangeChangeCount, "Only the dragged slider may report a change.");
            Assert.AreEqual(0, activity.FinishedCount, "Only the dragged slider may report a finished change.");
            Assert.AreEqual(0, activity.RangeFinishedCount, "Only the dragged slider may report a finished change.");
        }
        finally
        {
            if (down != 0) Touch(activity, MotionEventActions.Cancel, bounds.X(0.75f), bounds.CenterY, down);
            await Finish(activity);
        }
    }

    static async Task<SliderValueChangeFinishedTestActivity> Start()
    {
        SliderValueChangeFinishedTestActivity.Created = SliderValueChangeFinishedTestActivity.NewSource();
        using var intent = new Intent(global::Android.App.Application.Context,
            typeof(SliderValueChangeFinishedTestActivity));
        intent.AddFlags(ActivityFlags.NewTask);
        Runner.RunOnMainSync(() => global::Android.App.Application.Context.StartActivity(intent));
        var activity = await SliderValueChangeFinishedTestActivity.Created.Task.WaitAsync(TimeSpan.FromSeconds(10));
        try
        {
            await activity.Focused.Task.WaitAsync(TimeSpan.FromSeconds(10));
            await Frames(activity);
            Assert.IsGreaterThan(0, Require(activity.SliderBounds).Width);
            Assert.IsGreaterThan(0, Require(activity.RangeBounds).Width);
            Assert.IsGreaterThan(0, Require(activity.PlainBounds).Width);
            return activity;
        }
        catch
        {
            await Finish(activity);
            throw;
        }
    }

    static async Task Finish(SliderValueChangeFinishedTestActivity activity)
    {
        Runner.RunOnMainSync(activity.Finish);
        await activity.Destroyed.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Runner.WaitForIdleSync();
    }

    static long Touch(SliderValueChangeFinishedTestActivity activity, MotionEventActions action,
        float x, float y, long down = 0)
    {
        bool focused = false;
        Runner.RunOnMainSync(() => focused = activity.HasWindowFocus);
        Assert.IsTrue(focused, "Refusing to inject into a foreign input owner.");
        long now = SystemClock.UptimeMillis();
        if (down == 0) down = now;
        using var motion = MotionEvent.Obtain(down, now, action, x, y, 0)
            ?? throw new InvalidOperationException("Could not create native pointer event.");
        motion.SetSource(InputSourceType.Touchscreen);
        Runner.SendPointerSync(motion);
        return down;
    }

    static async Task Frames(SliderValueChangeFinishedTestActivity activity)
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
        value ?? throw new InvalidOperationException($"Missing {typeof(T).Name} in slider test.");
}
