using Android.Views;

namespace Microsoft.AndroidX.Compose.DeviceTests;

internal sealed class FabFrameCommit : IDisposable
{
    readonly ViewTreeObserver observer;
    readonly Java.Lang.Runnable callback;
    readonly TaskCompletionSource completion = FabStylingTestActivity.NewCompletion();
    int disposed;

    internal Task Completion => completion.Task;

    internal FabFrameCommit(ViewTreeObserver observer, Action committed)
    {
        ArgumentNullException.ThrowIfNull(observer);
        ArgumentNullException.ThrowIfNull(committed);
        if (!OperatingSystem.IsAndroidVersionAtLeast(29))
            throw new PlatformNotSupportedException("FAB acceptance requires native frame-commit callbacks.");
        this.observer = observer;
        callback = new Java.Lang.Runnable(() =>
        {
            try
            {
                committed();
                completion.TrySetResult();
            }
            catch (Exception error)
            {
                completion.TrySetException(error);
            }
        });
        try
        {
            observer.RegisterFrameCommitCallback(callback);
        }
        catch
        {
            callback.Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0) return;
        if (completion.Task.IsCompleted)
        {
            callback.Dispose();
            return;
        }
        if (OperatingSystem.IsAndroidVersionAtLeast(29) && observer.IsAlive
            && observer.UnregisterFrameCommitCallback(callback))
        {
            completion.TrySetCanceled();
            callback.Dispose();
            return;
        }
        FabStylingTestActivity.RetireAfterNativeCompletion(completion.Task, callback.Dispose, "frame commit");
    }
}
