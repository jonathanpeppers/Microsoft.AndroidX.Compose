using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>FilledIconToggleButton</c>. Same shape as <see cref="IconToggleButton"/>.
/// </summary>
public sealed class FilledIconToggleButton : ComposableContainer
{
    readonly bool _checked;
    readonly System.Action<bool> _onCheckedChange;

    public FilledIconToggleButton(bool @checked, System.Action<bool> onCheckedChange)
    {
        _checked = @checked;
        _onCheckedChange = onCheckedChange;
    }

    internal override void Render(IComposer composer)
    {
        var onChange = new ComposableLambda1(v =>
            _onCheckedChange(v is Java.Lang.Boolean b && b.BooleanValue()));
        var content = ComposableLambdas.Wrap2(composer, c => RenderChildren(c));
        ComposeBridges.FilledIconToggleButton(_checked, onChange, BuildModifier(), content, composer);
    }
}
