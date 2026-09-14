using Android.OS;
using Android.Views;
using Color = AndroidX.Compose.Color;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Native pixels, content locals, interactions, and remembered children for every FAB family.</summary>
[TestClass]
[DoNotParallelize]
public class FabStylingTests
{
    /// <summary>Exercises live optional styling and remembered content through all four facade routes.</summary>
    [TestMethod]
    [DataRow(0, false)]
    [DataRow(1, false)]
    [DataRow(2, false)]
    [DataRow(3, false)]
    [DataRow(0, true)]
    [DataRow(1, true)]
    [DataRow(2, true)]
    [DataRow(3, true)]
    public Task StylingTransitionsKeepNativeDefaultsAndRememberedContent(int variant, bool direct) =>
        RunCase(variant, direct);

    /// <summary>Runs the same transitions through the official bound API with constant native masks.</summary>
    [TestMethod]
    public Task NativeControlKeepsRememberedContent() => RunCase(4, false);

    static async Task RunCase(int variant, bool direct)
    {
        var instrumentation = TestInstrumentation.Current
            ?? throw new InvalidOperationException("FAB tests require the native instrumentation runner.");
        var automation = instrumentation.UiAutomation
            ?? throw new InvalidOperationException("Native UI automation unavailable.");
        var context = instrumentation.TargetContext
            ?? throw new InvalidOperationException("Instrumentation target context unavailable.");
        FabStylingTestActivity.Ready = FabStylingTestActivity.NewReady();
        using var intent = new global::Android.Content.Intent(context, typeof(FabStylingTestActivity));
        intent.AddFlags(global::Android.Content.ActivityFlags.NewTask | global::Android.Content.ActivityFlags.NoAnimation);
        intent.PutExtra("variant", variant);
        intent.PutExtra("direct", direct);
        instrumentation.RunOnMainSync(() => context.StartActivity(intent));
        var activity = await FabStylingTestActivity.Ready.Task.WaitAsync(TimeSpan.FromSeconds(15));
        try
        {
            object? identity = null;
            int expandedWidth = 0;
            for (int phase = 0; phase <= 15; phase++)
            {
                if (phase != 0)
                {
                    int next = phase;
                    instrumentation.RunOnMainSync(() =>
                    {
                        activity.Committed = FabStylingTestActivity.NewCompletion();
                        activity.Phase.Value = next;
                    });
                }
                await activity.Committed.Task.WaitAsync(TimeSpan.FromSeconds(15));
                automation.WaitForIdle(200, 5000);
                var measured = await activity.WaitForNativeIdleAsync();
                using var bounds = new global::Android.Graphics.Rect(
                    measured.Left, measured.Top, measured.Left + measured.Width, measured.Top + measured.Height);
                Assert.IsTrue(bounds.Width() > 0 && bounds.Height() > 0, "FAB layout must be admitted before pixels are read.");
                using var windowBounds = new global::Android.Graphics.Rect(
                    measured.WindowLeft, measured.WindowTop, measured.WindowLeft + measured.Width, measured.WindowTop + measured.Height);
                using var pixels = await activity.CaptureFabAsync(windowBounds);
                using var screenshot = automation.TakeScreenshot()
                    ?? throw new InvalidOperationException("Native FAB screenshot unavailable.");
                var directory = activity.GetExternalFilesDir(null)?.AbsolutePath
                    ?? throw new InvalidOperationException("FAB screenshot directory unavailable.");
                using (var file = File.Create(Path.Combine(directory, $"fab-{variant}-{direct}-{phase}.png")))
                {
                    var png = global::Android.Graphics.Bitmap.CompressFormat.Png
                        ?? throw new InvalidOperationException("Native PNG encoder unavailable.");
                    Assert.IsTrue(screenshot.Compress(png, 100, file));
                }
                int sampleX = pixels.Width / 2;
                int sampleY = pixels.Height / 8;
                foreach (var content in measured.Content)
                    Assert.IsFalse(
                        measured.WindowLeft + sampleX >= content.Left && measured.WindowLeft + sampleX < content.Right
                        && measured.WindowTop + sampleY >= content.Top && measured.WindowTop + sampleY < content.Bottom,
                        "The fixed container sample must not intersect native icon or label layout bounds.");
                int actual = pixels.GetPixel(sampleX, sampleY);
                var expectedContainer = Container(phase);
                Console.WriteLine($"FAB {variant}, direct={direct}, phase={phase}, bounds={bounds}, " +
                    $"window={windowBounds}, pixel={actual:X8}, content={activity.ContentColor:X16}, elevation={activity.TonalElevation}.");
                Assert.AreEqual(unchecked((int)(expectedContainer.ToPacked() >> 32)), actual,
                    $"Container pixel for variant {variant}, direct={direct}, phase={phase}.");
                Assert.AreEqual(Foreground(phase).ToPacked(), activity.ContentColor, $"Native inherited content, phase={phase}.");
                Assert.AreEqual(phase is 1 or 7 ? 0f : 6f, activity.TonalElevation, $"Native elevation, phase={phase}.");
                Assert.IsNotNull(activity.ContentIdentity);
                identity ??= activity.ContentIdentity;
                Assert.AreSame(identity, activity.ContentIdentity, $"FAB icon lost remembered content in phase {phase}.");
                if (variant == 3 && phase == 7) expandedWidth = bounds.Width();
                if (variant == 3 && phase == 8) Assert.IsTrue(bounds.Width() < expandedWidth, "Extended label must actually collapse.");
                if (variant == 3 && phase == 9) Assert.AreEqual(expandedWidth, bounds.Width(), "Extended label must expand again.");

                if (phase is 1 or 2)
                {
                    // Real native input must reach the currently supplied source, including its replacement.
                    instrumentation.RunOnMainSync(() => activity.PressChanged = FabStylingTestActivity.NewCompletion());
                    long downTime = SystemClock.UptimeMillis();
                    using var down = MotionEvent.Obtain(downTime, downTime, MotionEventActions.Down,
                        bounds.CenterX(), bounds.CenterY(), 0)
                        ?? throw new InvalidOperationException("Could not create native pointer down.");
                    down.SetSource(InputSourceType.Touchscreen);
                    instrumentation.SendPointerSync(down);
                    try
                    {
                        await activity.PressChanged.Task.WaitAsync(TimeSpan.FromSeconds(10));
                        Assert.IsTrue(activity.Pressed, "The hoisted native source did not observe the press.");
                        instrumentation.RunOnMainSync(() => activity.PressChanged = FabStylingTestActivity.NewCompletion());
                    }
                    finally
                    {
                        using var up = MotionEvent.Obtain(downTime, SystemClock.UptimeMillis(), MotionEventActions.Up,
                            bounds.CenterX(), bounds.CenterY(), 0)
                            ?? throw new InvalidOperationException("Could not create native pointer up.");
                        up.SetSource(InputSourceType.Touchscreen);
                        instrumentation.SendPointerSync(up);
                    }
                    await activity.PressChanged.Task.WaitAsync(TimeSpan.FromSeconds(10));
                    Assert.IsFalse(activity.Pressed);
                    automation.WaitForIdle(200, 5000);
                    await activity.WaitForNativeIdleAsync();
                    Assert.AreEqual(phase, activity.Clicks, "FAB click callback must keep working after source replacement.");
                }
                if (phase == 2)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    Java.Lang.JavaSystem.Gc();
                }
            }
        }
        finally
        {
            instrumentation.RunOnMainSync(activity.Finish);
            await activity.Destroyed.Task.WaitAsync(TimeSpan.FromSeconds(15));
        }
    }

    static Color Container(int phase) => phase switch
    {
        1 or 10 => Color.FromHex("#FFE082"),
        2 or 8 or 9 => Color.Blue,
        4 => Color.FromHex("#402050"),
        5 => Color.FromHex("#D0F8E0"),
        6 => Color.FromHex("#104030"),
        7 => Color.White,
        _ => Color.FromHex("#E8D0FF"),
    };

    static Color Foreground(int phase) => phase switch
    {
        1 or 7 or 10 => Color.Black,
        2 or 8 or 9 => Color.White,
        4 => Color.FromHex("#F0D0FF"),
        5 => Color.FromHex("#103020"),
        6 => Color.FromHex("#C0FFE0"),
        _ => Color.FromHex("#201030"),
    };
}
