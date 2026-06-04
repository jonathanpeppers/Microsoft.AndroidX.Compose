using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>IconToggleButton</c>:
/// <code>
/// new IconToggleButton(checked: state.Value, onCheckedChange: v => state.Value = v) { new Text("★") }
/// </code>
/// </summary>
public sealed class IconToggleButton : ComposableContainer
{
    readonly bool _checked;
    readonly System.Action<bool> _onCheckedChange;

    public IconToggleButton(bool @checked, System.Action<bool> onCheckedChange)
    {
        _checked = @checked;
        _onCheckedChange = onCheckedChange;
    }

    internal override void Render(IComposer composer)
    {
        var onChange = new ComposableLambda1(v =>
            _onCheckedChange(v is Java.Lang.Boolean b && b.BooleanValue()));
        var content = ComposableLambdas.Wrap2(composer, c => RenderChildren(c));
        ComposeBridges.IconToggleButton(_checked, onChange, BuildModifier(), content, composer);
    }
}
