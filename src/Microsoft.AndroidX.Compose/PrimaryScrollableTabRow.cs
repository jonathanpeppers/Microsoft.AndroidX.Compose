using AndroidX.Compose.Runtime;

namespace AndroidX.Compose;

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

    public override void Render(IComposer composer)
    {
        var tabs = ComposableLambdas.Wrap2(composer, c => RenderChildren(c));
        ComposeBridges.PrimaryScrollableTabRow(
            selectedTabIndex: _selectedTabIndex,
            modifier:         BuildModifier(),
            scrollState:      null,
            tabs:             tabs,
            composer:         composer);
    }
}
