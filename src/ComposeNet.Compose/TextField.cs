using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>TextField</c> (filled variant). Bound to a
/// <see cref="MutableState{T}"/> of <see cref="string"/> so user edits
/// trigger recomposition.
/// </summary>
public sealed class TextField : ComposableNode
{
    readonly MutableState<string> _state;
    public TextField(MutableState<string> state) => _state = state;

    internal override void Render(IComposer composer)
    {
        var onChange = new ComposableLambda1(v => _state.Value = v?.ToString() ?? string.Empty);
        ComposeBridges.TextField(_state.Value ?? string.Empty, onChange, BuildModifier(), composer);
    }
}
