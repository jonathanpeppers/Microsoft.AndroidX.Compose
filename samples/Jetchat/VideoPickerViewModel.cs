namespace AndroidX.Compose.Samples.Jetchat;

/// <summary>Retains an in-flight video picker result across activity recreation.</summary>
[Android.Runtime.Register("net/compose/samples/jetchat/VideoPickerViewModel")]
public sealed class VideoPickerViewModel : ViewModel
{
    Action<VideoPickResult>? _consumer;
    VideoPickResult? _undelivered;
    bool _pickerOpen;

    internal bool Begin()
    {
        if (_pickerOpen)
            return false;
        _pickerOpen = true;
        return true;
    }

    internal void Connect(Action<VideoPickResult> consumer)
    {
        ArgumentNullException.ThrowIfNull(consumer);
        _consumer = consumer;
        if (_undelivered is not VideoPickResult result)
            return;
        _undelivered = null;
        consumer(result);
    }

    internal void Complete(VideoPickResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        _pickerOpen = false;
        var consumer = _consumer;
        if (consumer is null)
            _undelivered = result;
        else
            consumer(result);
    }

    internal void Disconnect() => _consumer = null;

    protected override void OnClearedCore()
    {
        _consumer = null;
        _undelivered = null;
    }
}
