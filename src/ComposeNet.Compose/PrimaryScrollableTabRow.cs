using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>PrimaryScrollableTabRow</c>. Like
/// <see cref="PrimaryTabRow"/> but tabs are laid out at their natural
/// width and the row scrolls horizontally — useful for primary
/// destinations when there are too many tabs to fit on one screen.
/// </summary>
public sealed class PrimaryScrollableTabRow : ComposableContainer
{
    readonly int _selectedTabIndex;
    public PrimaryScrollableTabRow(int selectedTabIndex) => _selectedTabIndex = selectedTabIndex;

    internal override void Render(IComposer composer)
    {
        var tabs = new ComposableLambda2(c => RenderChildren(c));
        ComposeBridges.PrimaryScrollableTabRow(
            _selectedTabIndex, BuildModifier(), scrollState: null, tabs, composer);
    }
}
