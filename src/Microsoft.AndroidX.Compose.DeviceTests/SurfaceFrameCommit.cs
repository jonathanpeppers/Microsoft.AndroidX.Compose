using Android.Views;

namespace Microsoft.AndroidX.Compose.DeviceTests;

internal sealed class SurfaceFrameCommit : IDisposable
{
    readonly ViewTreeObserver _observer;
    readonly Java.Lang.Runnable _callback;
    readonly TaskCompletionSource _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal SurfaceFrameCommit(ViewTreeObserver observer)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(29))
            throw new PlatformNotSupportedException("Surface frame capture requires Android 10 or newer.");
        _observer = observer;
        _callback = new Java.Lang.Runnable(() => _completion.TrySetResult());
        _observer.RegisterFrameCommitCallback(_callback);
    }

    internal Task Committed => _completion.Task;

    /// <inheritdoc/>
    public void Dispose()
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(29)
            && _observer.IsAlive && _observer.UnregisterFrameCommitCallback(_callback))
            _completion.TrySetCanceled();
        // A callback already handed to RenderThread still owns its peer.
        _ = _completion.Task.ContinueWith(_ => _callback.Dispose(),
            CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }
}
