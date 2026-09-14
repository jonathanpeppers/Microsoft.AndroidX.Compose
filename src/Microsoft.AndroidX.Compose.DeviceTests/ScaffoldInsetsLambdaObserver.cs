#if DEBUG
using Android.Runtime;
using AndroidX.Compose;
using AndroidX.Compose.Foundation.Layout;
using Kotlin.Jvm.Functions;

namespace Microsoft.AndroidX.Compose.DeviceTests;

// Owns independent global references for exactly one activity lifetime.
internal sealed class ScaffoldInsetsLambdaObserver : IDisposable
{
    static ScaffoldInsetsLambdaObserver? s_active;
    readonly object _lock = new();
    readonly List<IntPtr> _references = [];
    readonly Action<IFunction3, IWindowInsets> _callback;
    readonly ScaffoldInsetsTestActivity _activity;
    bool _disposed;

    internal ScaffoldInsetsLambdaObserver(ScaffoldInsetsTestActivity activity)
    {
        EnsureMainThread();
        _activity = activity;
        _callback = Observe;
        if (Scaffold.ContentLambdaObserver is not null)
            throw new InvalidOperationException("A Scaffold content observer is already installed.");
        Scaffold.ContentLambdaObserver = _callback;
        Volatile.Write(ref s_active, this);
    }

    void Observe(IFunction3 content, IWindowInsets insets)
    {
        EnsureMainThread();
        var peer = (Java.Lang.Object)content;
        IntPtr reference = IntPtr.Zero;
        try
        {
            reference = JNIEnv.NewGlobalRef(peer.Handle);
            if (reference == IntPtr.Zero)
                throw new InvalidOperationException("Could not retain the native Scaffold content lambda.");
            lock (_lock)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                _references.Add(reference);
                var owned = reference;
                reference = IntPtr.Zero;
                _activity.RecordNativeArguments(owned, insets);
            }
        }
        finally
        {
            if (reference != IntPtr.Zero)
                JNIEnv.DeleteGlobalRef(reference);
            GC.KeepAlive(content);
        }
    }

    /// <summary>Unsubscribes the hook and releases only the JNI references owned by this activity.</summary>
    public void Dispose()
    {
        EnsureMainThread();
        lock (_lock)
        {
            if (_disposed)
                return;
            if (ReferenceEquals(Scaffold.ContentLambdaObserver, _callback))
                Scaffold.ContentLambdaObserver = null;
            while (_references.Count > 0)
            {
                int index = _references.Count - 1;
                JNIEnv.DeleteGlobalRef(_references[index]);
                _references.RemoveAt(index);
            }
            _disposed = true;
            Interlocked.CompareExchange(ref s_active, null, this);
        }
    }

    internal static async Task ReleaseActiveAsync()
    {
        var active = Volatile.Read(ref s_active);
        if (active is null)
            return;
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        // OnCreate may have installed the hook before activity availability was published.
        active._activity.RunOnUiThread(() =>
        {
            try
            {
                Volatile.Read(ref s_active)?.Dispose();
                completion.TrySetResult();
            }
            catch (Exception ex)
            {
                completion.TrySetException(ex);
            }
        });
        await completion.Task.WaitAsync(TimeSpan.FromSeconds(10));
    }

    static void EnsureMainThread()
    {
        if (global::Android.OS.Looper.MainLooper?.IsCurrentThread != true)
            throw new InvalidOperationException("Scaffold lambda observation must run on the main thread.");
    }
}
#endif
