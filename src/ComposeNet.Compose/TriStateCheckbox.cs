using AndroidX.Compose.Material3;
using AndroidX.Compose.Runtime;
using AndroidX.Compose.UI.State;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>TriStateCheckbox</c> — a checkbox with an
/// indeterminate state in addition to checked/unchecked. The
/// <see cref="ToggleableState"/> values are <c>On</c>, <c>Off</c>,
/// and <c>Indeterminate</c>:
/// <code>
/// new TriStateCheckbox(
///     state: ToggleableState.Indeterminate,
///     onClick: () => state.Value = ToggleableState.On)
/// </code>
/// </summary>
public sealed class TriStateCheckbox : ComposableNode
{
    readonly ToggleableState _state;
    readonly System.Action _onClick;

    public TriStateCheckbox(ToggleableState state, System.Action onClick)
    {
        _state = state;
        _onClick = onClick;
    }

    internal override void Render(IComposer composer)
    {
        var onClick = new ComposableLambda0(_onClick);

        var modifier = BuildModifier();
        int defaults = (int)TriStateCheckboxDefault.All;
        if (modifier is not null) defaults &= ~(int)TriStateCheckboxDefault.Modifier;

        CheckboxKt.TriStateCheckbox(
            state:             _state,
            onClick:           onClick,
            modifier:          modifier,
            enabled:           true,
            colors:            null,
            interactionSource: null,
            _composer:         composer,
            p7:                0,
            _changed:          defaults);
    }
}
