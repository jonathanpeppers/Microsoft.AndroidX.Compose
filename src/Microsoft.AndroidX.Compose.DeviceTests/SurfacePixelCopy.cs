using System.Runtime.InteropServices;
using Android.Graphics;
using Android.OS;
using Android.Views;

namespace Microsoft.AndroidX.Compose.DeviceTests;

internal sealed class SurfacePixelCopy : Java.Lang.Object, PixelCopy.IOnPixelCopyFinishedListener
{
    readonly Bitmap _bitmap;
    readonly Handler _handler;
    readonly TaskCompletionSource<Bitmap> _completion;
    GCHandle _root;

    SurfacePixelCopy(Bitmap bitmap, Handler handler, TaskCompletionSource<Bitmap> completion)
    {
        _bitmap = bitmap;
        _handler = handler;
        _completion = completion;
    }

    internal static Task<Bitmap> Capture(SurfaceStylingTestActivity activity) =>
        Capture(activity, activity.OnUi);

    internal static async Task<Bitmap> Capture(global::AndroidX.Activity.ComponentActivity activity, Func<Action, Task> onUi)
    {
        var completion = new TaskCompletionSource<Bitmap>(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            await onUi(() =>
            {
                if (!OperatingSystem.IsAndroidVersionAtLeast(26))
                    throw new PlatformNotSupportedException("Window PixelCopy requires Android 8 or newer.");
                var window = activity.Window ?? throw new InvalidOperationException("Surface window unavailable.");
                var decor = window.DecorView ?? throw new InvalidOperationException("Surface decor unavailable.");
                var config = Bitmap.Config.Argb8888 ?? throw new InvalidOperationException("ARGB8888 unavailable.");
                var looper = Looper.MainLooper ?? throw new InvalidOperationException("Main looper unavailable.");
                var handler = new Handler(looper);
                Bitmap? bitmap = null;
                SurfacePixelCopy? copy = null;
                try
                {
                    bitmap = Bitmap.CreateBitmap(decor.Width, decor.Height, config);
                    copy = new SurfacePixelCopy(bitmap, handler, completion);
                    copy._root = GCHandle.Alloc(copy);
                    PixelCopy.Request(window, bitmap, copy, handler);
                }
                catch (Exception error)
                {
                    bitmap?.Dispose();
                    if (copy is not null) copy.Release();
                    else handler.Dispose();
                    completion.TrySetException(error);
                    throw;
                }
            });
            return await completion.Task.WaitAsync(TimeSpan.FromSeconds(10));
        }
        catch
        {
            // PixelCopy cannot be cancelled. A timed-out copy retains its bitmap
            // and callback until native completion; only then may cleanup run.
            _ = completion.Task.ContinueWith(task =>
            {
                if (task.IsCompletedSuccessfully)
                    task.Result.Dispose();
                else
                    _ = task.Exception;
            }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            throw;
        }
    }

    /// <inheritdoc/>
    public void OnPixelCopyFinished(int copyResult)
    {
        try
        {
            if (copyResult == (int)PixelCopyResult.Success)
                _completion.TrySetResult(_bitmap);
            else
            {
                _bitmap.Dispose();
                _completion.TrySetException(new InvalidOperationException($"Window PixelCopy failed: {copyResult}."));
            }
        }
        finally { Release(); }
    }

    void Release()
    {
        _handler.Dispose();
        if (_root.IsAllocated) _root.Free();
        Dispose();
    }
}
