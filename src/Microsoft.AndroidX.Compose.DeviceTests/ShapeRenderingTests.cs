using Android.Content;
using Android.Graphics;
using LayoutDirection = AndroidX.Compose.UI.Unit.LayoutDirection;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Checks actual compositor pixels and retained callbacks after live recomposition and GC.</summary>
[TestClass]
[DoNotParallelize]
public class ShapeRenderingTests
{
    /// <summary>RTL mirrors relative cuts and the builder, but never absolute corners.</summary>
    [TestMethod]
    public async Task NativeClippingSurvivesRtlResizeAndGc()
    {
        var instrumentation = TestInstrumentation.Current
            ?? throw new InvalidOperationException("Shape instrumentation missing.");
        ShapeRenderingTestActivity.Prepare();
        using var intent = new Intent(Application.Context, typeof(ShapeRenderingTestActivity));
        intent.AddFlags(ActivityFlags.NewTask);
        Application.Context.StartActivity(intent);
        var activity = await ShapeRenderingTestActivity.Started.Task.WaitAsync(TimeSpan.FromSeconds(10));
        try
        {
            await WaitFor(() => activity.Resumed && activity.HasWindowFocus && activity.Tiles.Count == 4
                && Volatile.Read(ref activity.Probe.Calls) > 0);
            var original = activity.Generic ?? throw new InvalidOperationException("Remembered GenericShape missing.");
            for (int cycle = 0; cycle < 4; cycle++)
            {
                if (cycle > 0)
                {
                    int before = Volatile.Read(ref activity.Probe.Calls);
                    ShapeTests.Collect();
                    await activity.OnUi(() => activity.Cycle.Value = cycle);
                    await WaitFor(() => activity.RenderedCycle == cycle
                        && Volatile.Read(ref activity.Probe.Calls) > before);
                }
                await activity.CommitFrame();
                var automation = instrumentation.UiAutomation
                    ?? throw new InvalidOperationException("Shape native screenshot service unavailable.");
                using var bitmap = automation.TakeScreenshot()
                    ?? throw new InvalidOperationException("Shape compositor screenshot failed.");
                bool rtl = cycle % 2 == 1;
                await activity.OnUi(() =>
                {
                    Assert.IsTrue(activity.Resumed && activity.HasWindowFocus);
                    Assert.IsNull(activity.ForegroundFailure);
                    Assert.AreSame(original, activity.Generic, "Recomposition recreated the native shape/callback.");
                    Assert.AreEqual(activity.Tiles["generic"].Width, activity.Probe.LastSize.Width, 0.01f);
                    Assert.AreEqual(activity.Tiles["generic"].Height, activity.Probe.LastSize.Height, 0.01f);
                    Assert.AreEqual(rtl ? LayoutDirection.Rtl : LayoutDirection.Ltr, activity.Probe.LastDirection);
                    Check(bitmap, activity.Pixel("generic", 0.1f, 0.1f), red: !rtl);
                    Check(bitmap, activity.Pixel("generic", 0.9f, 0.1f), red: rtl);
                    Check(bitmap, activity.Pixel("generic", 0.5f, 0.5f), red: true);
                    string[] ids = ["cut", "absolute-cut", "absolute-round"];
                    foreach (string id in ids)
                    {
                        Check(bitmap, activity.Pixel(id, 0.025f, 0.04f), red: id == "cut" && rtl);
                        Check(bitmap, activity.Pixel(id, 0.975f, 0.04f), red: id != "cut" || !rtl);
                        Check(bitmap, activity.Pixel(id, 0.5f, 0.5f), red: true);
                    }
                });
                string directory = System.IO.Path.Combine(
                    Application.Context.GetExternalFilesDir(null)?.AbsolutePath
                        ?? throw new InvalidOperationException("Shape capture directory unavailable."), "shape-captures");
                Directory.CreateDirectory(directory);
                using var output = File.Create(System.IO.Path.Combine(directory, $"cycle-{cycle}.png"));
                Assert.IsTrue(bitmap.Compress(Bitmap.CompressFormat.Png
                    ?? throw new InvalidOperationException("PNG unavailable."), 100, output));
                Console.WriteLine($"SHAPE_FRAME cycle={cycle} rtl={rtl} calls={activity.Probe.Calls} "
                    + $"size={activity.Probe.LastSize} pid={(global::Android.OS.Process.MyPid())} pixels=12/12");
            }
            GC.KeepAlive(original);
        }
        finally
        {
            await activity.OnUi(() => { activity.Ending = true; activity.Finish(); });
            await activity.Destroyed.Task.WaitAsync(TimeSpan.FromSeconds(10));
        }
    }

    static void Check(Bitmap bitmap, (int X, int Y) point, bool red)
    {
        int expected = red ? global::Android.Graphics.Color.Red.ToArgb() : global::Android.Graphics.Color.White.ToArgb();
        Assert.AreEqual(expected, bitmap.GetPixel(point.X, point.Y),
            $"Native clipping at ({point.X},{point.Y}), expected {(red ? "red" : "white")}.");
    }

    static async Task WaitFor(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        while (!condition())
            await Task.Delay(20, timeout.Token);
    }
}
