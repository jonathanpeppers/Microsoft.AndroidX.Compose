using Android.Graphics;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Checks real Compose text drawing in both palettes across recomposition and collection.</summary>
[TestClass]
[DoNotParallelize]
public class ResourceFontRenderTests
{
    [TestMethod]
    public async Task Fonts_RenderInLightAndDarkAndRemainStableAfterGc()
    {
        ResourceFontTests.RequireFontRasterSupport();
        var context = global::Android.App.Application.Context;
        using var intent = new global::Android.Content.Intent(context, typeof(ResourceFontTestActivity));
        intent.AddFlags(global::Android.Content.ActivityFlags.NewTask);
        context.StartActivity(intent);
        await WaitFor(() => ResourceFontTestActivity.Current is { CompletedPasses: > 0 }, "Font activity did not render.");
        var activity = ResourceFontTestActivity.Current
            ?? throw new InvalidOperationException("ResourceFontTestActivity not available.");
        try
        {
            bool[] palettes = [false, true];
            foreach (bool dark in palettes)
            {
                int previous = activity.CompletedPasses;
                activity.RunOnUiThread(() =>
                {
                    activity.Dark.Value = dark;
                    activity.Cycle.Value++;
                });
                await WaitFor(() => activity.CompletedPasses > previous, "Font palette did not recompose.");
                await Task.Delay(300);
                var baseline = await Capture(activity);
                int ink = dark ? -1 : unchecked((int)0xff000000);
                Assert.IsTrue(baseline.Count(pixel => pixel == ink) > 500, "Expected visible contrasting font pixels.");

                for (int cycle = 0; cycle < 5; cycle++)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    Java.Lang.JavaSystem.Gc();
                    previous = activity.CompletedPasses;
                    activity.RunOnUiThread(() => activity.Cycle.Value++);
                    await WaitFor(() => activity.CompletedPasses > previous, "Font text did not re-render after collection.");
                    await Task.Delay(150);
                    CollectionAssert.AreEqual(baseline, await Capture(activity), "Rendered glyphs changed after collection.");
                }
            }
        }
        finally
        {
            activity.RunOnUiThread(activity.Finish);
            await WaitFor(() => ResourceFontTestActivity.Current is null, "Font activity did not finish.");
        }
    }

    static Task<int[]> Capture(ResourceFontTestActivity activity)
    {
        var completion = new TaskCompletionSource<int[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        activity.RunOnUiThread(() =>
        {
            try
            {
                var decor = activity.Window?.DecorView
                    ?? throw new InvalidOperationException("Font activity DecorView unavailable.");
                using var bitmap = Bitmap.CreateBitmap(decor.Width, decor.Height, Bitmap.Config.Argb8888
                    ?? throw new InvalidOperationException("ARGB bitmap configuration unavailable."));
                using var canvas = new Canvas(bitmap);
                decor.Draw(canvas);
                // Crop system bars and the bottom of the screen, which can change independently.
                int top = decor.Height / 10;
                int height = decor.Height / 3;
                var pixels = new int[decor.Width * height];
                bitmap.GetPixels(pixels, 0, decor.Width, 0, top, decor.Width, height);
                completion.SetResult(pixels);
            }
            catch (Exception exception)
            {
                completion.SetException(exception);
            }
        });
        return completion.Task;
    }

    static async Task WaitFor(Func<bool> condition, string message)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (!condition() && DateTime.UtcNow < deadline)
            await Task.Delay(50);
        Assert.IsTrue(condition(), message);
    }
}
