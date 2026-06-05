namespace ComposeNet;

/// <summary>
/// Material 3 <c>TextField</c> (filled variant). Pass a <c>value</c> + <c>onValueChange</c>
/// pair, or use the <see cref="TextField(MutableState{string})"/> convenience ctor to bind
/// to a <see cref="MutableState{T}"/> directly so user edits trigger recomposition.
/// </summary>
public sealed partial class TextField
{
    /// <summary>
    /// Bind this <c>TextField</c> to a <see cref="MutableState{T}"/> of <see cref="string"/>
    /// so user edits trigger recomposition automatically.
    /// </summary>
    public TextField(MutableState<string> state)
        : this(state.Value ?? string.Empty, v => state.Value = v)
    {
    }
}
