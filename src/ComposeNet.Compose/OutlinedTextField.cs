using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>OutlinedTextField</c>. Same binding contract as
/// <see cref="TextField"/>.
/// </summary>
public sealed class OutlinedTextField : ComposableNode
{
    readonly MutableState<string> _state;
    public OutlinedTextField(MutableState<string> state) => _state = state;

    internal override void Render(IComposer composer)
    {
        var onChange = new ComposableLambda1(v => _state.Value = v?.ToString() ?? string.Empty);
        ComposeBridges.OutlinedTextField(_state.Value ?? string.Empty, onChange, BuildModifier(), composer);
    }
}
