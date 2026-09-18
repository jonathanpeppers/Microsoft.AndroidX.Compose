namespace AndroidX.Compose;

internal sealed class ProduceStateWriter<T> : MutableState<T>
{
    readonly Action<T> _write;

    internal ProduceStateWriter(MutableState<T> state, Action<T> write)
        : base(state._state)
    {
        _write = write;
    }

    public override T Value
    {
        get => base.Value;
        set => _write(value);
    }
}
