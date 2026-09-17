namespace AndroidX.Compose.Samples.Jetchat;

internal sealed class RecordButtonClickState(Action callback)
{
    Action _callback = callback;

    internal void Invoke() => _callback();

    internal void Update(Action callback) => _callback = callback;
}
