using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>SecondaryScrollableTabRow</c>. Like
/// <see cref="SecondaryTabRow"/> but tabs are laid out at their natural
/// width and the row scrolls horizontally — useful for secondary
/// sub-navigation when there are too many tabs to fit on one screen.
/// </summary>
public sealed class SecondaryScrollableTabRow : ComposableContainer
{
    readonly int _selectedTabIndex;
    public SecondaryScrollableTabRow(int selectedTabIndex) => _selectedTabIndex = selectedTabIndex;

    internal override void Render(IComposer composer)
    {
        var tabs = ComposableLambdas.Wrap2(composer, c => RenderChildren(c));
        ComposeBridges.SecondaryScrollableTabRow(
            selectedTabIndex: _selectedTabIndex,
            modifier:         BuildModifier(),
            scrollState:      null,
            tabs:             tabs,
            composer:         composer);
    }
}
