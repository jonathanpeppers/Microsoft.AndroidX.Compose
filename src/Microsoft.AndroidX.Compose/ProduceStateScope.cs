using Android.Runtime;
using AndroidX.Compose.Runtime;

namespace AndroidX.Compose;

/// <summary>
/// Slot value used by <see cref="ComposeExtensions.ProduceState{T}(T, Func{MutableState{T}, CancellationToken, Task}, int, string)"/>:
/// owns one keyed producer task / cancellation lifecycle. Implements
/// <see cref="IRememberObserver"/> so it starts the producer when
/// Compose adds the value to the composition
/// (<see cref="OnRemembered"/>) and cancels it when the value is
/// removed (<see cref="OnForgotten"/> / <see cref="OnAbandoned"/>).
///
/// <c>ProduceStateScope</c> must be the <em>direct</em> slot value
/// (not wrapped in <see cref="RememberHolder"/>) — Compose only
/// inspects the exact object handed to
/// <see cref="IComposer.UpdateRememberedValue"/> for the
/// <see cref="IRememberObserver"/> interface.
/// </summary>
[Register("net/compose/ProduceStateScope")]
internal sealed class ProduceStateScope<T> : Java.Lang.Object, IRememberObserver
{
    readonly object _gate = new();
    readonly Func<MutableState<T>, CancellationToken, Task>? _producer;
    readonly MutableState<T>? _state;
    readonly ProduceStateWriter<T>? _writer;
    CancellationTokenSource? _cts;
    bool _active;
    bool _started;
    bool _disposed;

    internal object?[]? Keys { get; }

    public ProduceStateScope(
        MutableState<T> state,
        Func<MutableState<T>, CancellationToken, Task> producer,
        object?[]? keys)
    {
        _state = state;
        _producer = producer;
        _writer = new ProduceStateWriter<T>(state, TrySetValue);
        Keys = keys is null ? null : (object?[])keys.Clone();
    }

    // Peer-rehydration ctor — the .NET-for-Android runtime invokes
    // this when an existing JNI handle for our [Register]'d class
    // crosses back into managed code without a live peer. We never
    // create new scope instances this way (Compose hands back the
    // original peer from its slot table), but the runtime requires
    // the constructor to exist.
    internal ProduceStateScope(IntPtr handle, JniHandleOwnership transfer)
        : base(handle, transfer)
    {
    }

    public void OnRemembered() => Start();

    public void OnForgotten() => Stop();

    public void OnAbandoned() => Stop();

    void Start()
    {
        Func<MutableState<T>, CancellationToken, Task> producer;
        ProduceStateWriter<T> writer;
        CancellationToken token;
        lock (_gate)
        {
            if (_started || _disposed)
                return;
            producer = _producer
                ?? throw new InvalidOperationException(
                    "ProduceState producer is unavailable on a rehydrated peer.");
            writer = _writer
                ?? throw new InvalidOperationException(
                    "ProduceState writer is unavailable on a rehydrated peer.");
            _started = true;
            _active = true;
            _cts = new CancellationTokenSource();
            token = _cts.Token;
        }

        Task task;
        try
        {
            task = producer(writer, token)
                ?? Task.FromException(
                    new InvalidOperationException(
                        "ProduceState producer returned a null Task."));
        }
        catch (Exception ex)
        {
            task = Task.FromException(ex);
        }

        // Surface producer faults to logcat instead of leaving them
        // as unobserved task exceptions. Match ComposeExtensions'
        // logging tag so all our diagnostics share a single filter.
        _ = task.ContinueWith(static t =>
        {
            if (t.Exception is { } ex)
            {
                Android.Util.Log.Error(
                    "AndroidX.Compose",
                    "ProduceState producer faulted: " + ex);
            }
        }, TaskScheduler.Default);
    }

    void Stop()
    {
        CancellationTokenSource? cts;
        lock (_gate)
        {
            if (_disposed)
                return;
            _disposed = true;
            _active = false;
            cts = _cts;
            _cts = null;
        }

        try
        {
            cts?.Cancel();
        }
        catch (Exception ex)
        {
            Android.Util.Log.Error(
                "AndroidX.Compose",
                "ProduceState producer cancellation callback faulted: " + ex);
        }
        finally
        {
            cts?.Dispose();
        }
    }

    void TrySetValue(T value)
    {
        lock (_gate)
        {
            if (_active)
            {
                var state = _state
                    ?? throw new InvalidOperationException(
                        "ProduceState state is unavailable on a rehydrated peer.");
                state.Value = value;
            }
        }
    }
}
