using AndroidX.Compose;
using AndroidX.Compose.Runtime;

namespace Microsoft.AndroidX.Compose.Maui.Handlers;

sealed class FlyoutPresentationObserver : ComposableNode
{
    readonly DrawerStateHolder _state;
    readonly Action<bool> _onSettled;

    public FlyoutPresentationObserver(DrawerStateHolder state, Action<bool> onSettled)
    {
        _state = state;
        _onSettled = onSettled;
    }

    public override void Render(IComposer composer)
    {
        var current = _state.CurrentValue;
        var target = _state.TargetValue;
        if (!current.Equals(target))
            return;

        var isOpen = _state.IsOpen;
        composer.SideEffect(() => _onSettled(isOpen));
    }
}
