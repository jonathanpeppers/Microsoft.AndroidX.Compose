using AndroidX.Compose;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Verifies progress adapters and real Material 3 indicator rendering.</summary>
[TestClass]
[DoNotParallelize]
public class ProgressIndicatorTests
{
    [TestMethod]
    public void FloatFunction0_BoxesLatestValueAndPreservesPeerIdentity()
    {
        float value = 0.25f;
        var function = new FloatFunction0(() => value);
        var handle = function.Handle;

        Assert.AreEqual(0.25f, ((Java.Lang.Float)function.Invoke()).FloatValue());
        value = 0.75f;
        Assert.AreEqual(0.75f, ((Java.Lang.Float)function.Invoke()).FloatValue());
        Assert.AreEqual(handle, function.Handle);
    }

    [TestMethod]
    public async Task DeterminateAndIndeterminateIndicators_RenderAndUpdateLiveProgress()
    {
        ProgressIndicatorTestActivity.Reset();
        var activity = await StartActivity();
        try
        {
            await WaitFor(
                static () => ProgressIndicatorTestActivity.CompletedRenderPasses,
                static value => value > 0,
                TimeSpan.FromSeconds(10),
                "Progress indicators did not complete their initial render.");
            int initialRedPixels = await WaitForRedPixelCount(
                activity,
                static count => count > 0,
                "Determinate progress indicators did not draw their initial red progress.");

            int completedPasses = ProgressIndicatorTestActivity.CompletedRenderPasses;
            activity.RunOnUiThread(() =>
            {
                var progress = ProgressIndicatorTestActivity.Progress
                    ?? throw new InvalidOperationException(
                        "Progress state not set on ProgressIndicatorTestActivity.");
                progress.Value = 0.75f;
            });

            await WaitFor(
                static () => ProgressIndicatorTestActivity.CompletedRenderPasses,
                value => value > completedPasses,
                TimeSpan.FromSeconds(10),
                "Progress indicators did not re-render after a live update.");

            await WaitForRedPixelCount(
                activity,
                count => count > initialRedPixels * 3 / 2,
                "Determinate progress indicators did not visibly grow after the live update.");
        }
        finally
        {
            activity.RunOnUiThread(activity.Finish);
            await WaitFor(
                static () => ProgressIndicatorTestActivity.Current,
                static value => value is null,
                TimeSpan.FromSeconds(5),
                "Progress indicator test activity did not finish.");
        }
    }

    static async Task<ProgressIndicatorTestActivity> StartActivity()
    {
        var context = global::Android.App.Application.Context
            ?? throw new InvalidOperationException(
                "Application.Context not set for progress indicator tests.");
        using var intent = new global::Android.Content.Intent(
            context,
            typeof(ProgressIndicatorTestActivity));
        intent.AddFlags(global::Android.Content.ActivityFlags.NewTask);
        context.StartActivity(intent);

        return await WaitFor(
            static () => ProgressIndicatorTestActivity.Current,
            static value => value is not null,
            TimeSpan.FromSeconds(5),
            "Progress indicator test activity did not start.")
            ?? throw new InvalidOperationException(
                "Progress indicator test activity was not available.");
    }

    static async Task<T> WaitFor<T>(
        Func<T> read,
        Func<T, bool> predicate,
        TimeSpan timeout,
        string message)
    {
        var deadline = DateTime.UtcNow + timeout;
        T value;
        do
        {
            value = read();
            if (predicate(value))
                return value;
            await Task.Delay(50);
        } while (DateTime.UtcNow < deadline);

        Assert.Fail(message);
        return value;
    }

    static async Task<int> WaitForRedPixelCount(
        ProgressIndicatorTestActivity activity,
        Func<int, bool> predicate,
        string message)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(10);
        int redPixels;
        do
        {
            redPixels = await CaptureRedPixelCount(activity);
            if (predicate(redPixels))
                return redPixels;
            await Task.Delay(50);
        } while (DateTime.UtcNow < deadline);

        Assert.Fail($"{message} Last red pixel count: {redPixels}.");
        return redPixels;
    }

    static Task<int> CaptureRedPixelCount(
        ProgressIndicatorTestActivity activity)
    {
        var completion = new TaskCompletionSource<int>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        activity.RunOnUiThread(() =>
        {
            try
            {
                var decorView = activity.Window?.DecorView
                    ?? throw new InvalidOperationException(
                        "DecorView not set on ProgressIndicatorTestActivity.");
                int width = decorView.Width;
                int height = decorView.Height;
                if (width <= 0 || height <= 0)
                    throw new InvalidOperationException(
                        "ProgressIndicatorTestActivity had no drawable bounds.");

                var bitmapConfig = global::Android.Graphics.Bitmap.Config.Argb8888
                    ?? throw new InvalidOperationException(
                        "ARGB_8888 bitmap configuration was unavailable.");
                using var bitmap = global::Android.Graphics.Bitmap.CreateBitmap(
                    width,
                    height,
                    bitmapConfig)
                    ?? throw new InvalidOperationException(
                        "Could not allocate progress indicator snapshot.");
                using var canvas = new global::Android.Graphics.Canvas(bitmap);
                decorView.Draw(canvas);
                int[] pixels = new int[width * height];
                bitmap.GetPixels(pixels, 0, width, 0, 0, width, height);

                int redPixels = 0;
                foreach (int pixel in pixels)
                {
                    uint argb = unchecked((uint)pixel);
                    int red = (int)(argb >> 16) & 0xFF;
                    int green = (int)(argb >> 8) & 0xFF;
                    int blue = (int)argb & 0xFF;
                    if (red >= 200 && green <= 60 && blue <= 60)
                        redPixels++;
                }
                completion.SetResult(redPixels);
            }
            catch (Exception ex)
            {
                completion.SetException(ex);
            }
        });
        return completion.Task;
    }
}
