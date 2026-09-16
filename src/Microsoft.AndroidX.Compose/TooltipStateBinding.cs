using AndroidX.Compose.Material3;

namespace AndroidX.Compose;

internal sealed class TooltipStateBinding
{
    TooltipState? _holder;
    ITooltipState? _state;

    internal void Publish(TooltipState holder, ITooltipState state)
    {
        if (!ReferenceEquals(_holder, holder)
            && _holder is not null
            && ReferenceEquals(_holder.Jvm, _state))
        {
            _holder.Jvm = null;
        }

        _holder = holder;
        _state = state;
        holder.Jvm = state;
    }

    internal Action CreateCleanup() => Clear;

    void Clear()
    {
        if (_holder is not null && ReferenceEquals(_holder.Jvm, _state))
            _holder.Jvm = null;
        _holder = null;
        _state = null;
    }
}
