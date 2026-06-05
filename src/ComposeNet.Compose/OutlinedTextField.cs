namespace ComposeNet;

/// <summary>
/// Material 3 <c>OutlinedTextField</c>. Same binding contract as
/// <see cref="TextField"/>.
/// </summary>
public sealed partial class OutlinedTextField
{
    /// <summary>
    /// Bind this <c>OutlinedTextField</c> to a <see cref="MutableState{T}"/> of <see cref="string"/>
    /// so user edits trigger recomposition automatically.
    /// </summary>
    public OutlinedTextField(MutableState<string> state)
        : this(state.Value ?? string.Empty, v => state.Value = v)
    {
    }
}
