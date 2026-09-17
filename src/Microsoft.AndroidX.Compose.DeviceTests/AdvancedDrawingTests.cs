using Android.Content;
using AndroidX.Compose;
using Bitmap = Android.Graphics.Bitmap;
using NativeColor = Android.Graphics.Color;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Validates advanced drawing state restoration and rendered output.</summary>
[TestClass]
[DoNotParallelize]
public class AdvancedDrawingTests
{
    /// <summary>Nested scopes restore after exceptions and advanced operations reach the canvas.</summary>
    [TestMethod]
    public async Task AdvancedOperationsRestoreStateAndRender()
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(29))
            Assert.Inconclusive("Advanced drawing pixel checks require Android 10 or newer.");

        AdvancedDrawingTestActivity.Prepare();
        using var intent = new Intent(Application.Context, typeof(AdvancedDrawingTestActivity));
        intent.AddFlags(ActivityFlags.NewTask);
        Application.Context.StartActivity(intent);
        var activity = await AdvancedDrawingTestActivity.Started.Task.WaitAsync(TimeSpan.FromSeconds(10));
        try
        {
            await WaitFor(() => activity.Resumed
                && activity.ExceptionRestored
                && activity.TransformExpired
                && activity.DrawIntoCanvasCalls > 0
                && activity.TextDrawCalls > 0
                && activity.CanvasOrigin.Y > 0f);
            using var bitmap = await activity.Capture();
            await activity.OnUi(() =>
            {
                Assert.IsTrue(activity.ExceptionRestored);
                Assert.IsTrue(activity.TransformExpired);
                Assert.IsTrue(activity.InsetSize.Width > 0f);
                Assert.IsTrue(activity.InsetSize.Height > 0f);
                AssertColor(bitmap, activity.Pixel(10, 10), NativeColor.Blue);
                AssertColor(bitmap, activity.Pixel(40, 20), NativeColor.Red);
                AssertColor(bitmap, activity.Pixel(75, 20), NativeColor.Rgb(0x12, 0x24, 0x44));
                AssertColor(bitmap, activity.Pixel(90, 10), NativeColor.Green);
                AssertColor(bitmap, activity.Pixel(120, 10), NativeColor.Magenta);
                AssertColor(bitmap, activity.Pixel(20, 60), NativeColor.Yellow);
                AssertColor(bitmap, activity.Pixel(170, 20), NativeColor.Yellow);
                AssertRegionHasColor(
                    bitmap,
                    activity.Pixel(105, 120),
                    activity.Pixel(235, 179),
                    NativeColor.Green);
                AssertCombinedAlphaBlend(
                    bitmap,
                    activity.Pixel(215, 105),
                    NativeColor.Rgb(0x12, 0x24, 0x44),
                    NativeColor.White,
                    effectiveAlpha: 64);
            });
            Console.WriteLine(
                $"ADVANCED_DRAWING restored={activity.ExceptionRestored} expired={activity.TransformExpired} "
                + $"inset={activity.InsetSize} canvasCalls={activity.DrawIntoCanvasCalls} "
                + $"textCalls={activity.TextDrawCalls} pixels=7/7");
        }
        finally
        {
            await activity.OnUi(activity.Finish);
            await activity.Destroyed.Task.WaitAsync(TimeSpan.FromSeconds(10));
        }
    }

    /// <summary>Supporting integer geometry and owned drawing peers preserve their inputs.</summary>
    [TestMethod]
    public void SupportingInputsRoundTrip()
    {
        var offset = new IntOffset(-7, 11);
        var size = new IntSize(17, 23);
        Assert.AreEqual(-7, offset.X);
        Assert.AreEqual(11, offset.Y);
        Assert.AreEqual(17, size.Width);
        Assert.AreEqual(23, size.Height);
        Assert.AreEqual(
            64,
            global::AndroidX.Compose.Color.White
                .WithOpacity(0.5f)
                .ModulateOpacity(0.5f)
                .A);

        using var path = new global::AndroidX.Compose.Path()
            .MoveTo(0f, 0f)
            .LineTo(10f, 10f);
        using var dash = PathEffect.Dash([4f, 2f]);
        using var corner = PathEffect.Corner(2f);
        using var chain = PathEffect.Chain(corner, dash);
        using var stamped = PathEffect.Stamped(path, 4f);
    }

    static void AssertColor(Bitmap bitmap, (int X, int Y) point, NativeColor expected) =>
        Assert.AreEqual(expected.ToArgb(), bitmap.GetPixel(point.X, point.Y),
            $"Unexpected pixel at ({point.X}, {point.Y}).");

    static void AssertRegionHasColor(
        Bitmap bitmap,
        (int X, int Y) topLeft,
        (int X, int Y) bottomRight,
        NativeColor expected)
    {
        int expectedArgb = expected.ToArgb();
        for (int y = topLeft.Y; y <= bottomRight.Y; y++)
        {
            for (int x = topLeft.X; x <= bottomRight.X; x++)
            {
                if (bitmap.GetPixel(x, y) == expectedArgb)
                    return;
            }
        }
        Assert.Fail(
            $"Expected color {expectedArgb:X8} in region "
            + $"({topLeft.X},{topLeft.Y})-({bottomRight.X},{bottomRight.Y}).");
    }

    static void AssertCombinedAlphaBlend(
        Bitmap bitmap,
        (int X, int Y) point,
        NativeColor background,
        NativeColor foreground,
        int effectiveAlpha)
    {
        int actual = bitmap.GetPixel(point.X, point.Y);
        Assert.AreEqual(0xFF, global::Android.Graphics.Color.GetAlphaComponent(actual));
        AssertChannel(
            global::Android.Graphics.Color.GetRedComponent(actual),
            Blend(
                global::Android.Graphics.Color.GetRedComponent(background.ToArgb()),
                global::Android.Graphics.Color.GetRedComponent(foreground.ToArgb()),
                effectiveAlpha));
        AssertChannel(
            global::Android.Graphics.Color.GetGreenComponent(actual),
            Blend(
                global::Android.Graphics.Color.GetGreenComponent(background.ToArgb()),
                global::Android.Graphics.Color.GetGreenComponent(foreground.ToArgb()),
                effectiveAlpha));
        AssertChannel(
            global::Android.Graphics.Color.GetBlueComponent(actual),
            Blend(
                global::Android.Graphics.Color.GetBlueComponent(background.ToArgb()),
                global::Android.Graphics.Color.GetBlueComponent(foreground.ToArgb()),
                effectiveAlpha));
    }

    static int Blend(int background, int foreground, int alpha) =>
        (foreground * alpha + background * (255 - alpha) + 127) / 255;

    static void AssertChannel(int actual, int expected) =>
        Assert.IsTrue(
            Math.Abs(actual - expected) <= 2,
            $"Expected channel {expected} +/- 2, got {actual}.");

    static async Task WaitFor(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        while (!condition())
            await Task.Delay(20, timeout.Token);
    }
}
