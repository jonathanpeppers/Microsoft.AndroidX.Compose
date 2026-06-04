using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>ScrollableTabRow</c>. Like <see cref="TabRow"/> but the
/// tabs are laid out at their natural width and the row scrolls
/// horizontally — useful when there are too many tabs to fit on one
/// screen.
/// </summary>
public sealed class ScrollableTabRow : ComposableContainer
{
    readonly int _selectedTabIndex;
    public ScrollableTabRow(int selectedTabIndex) => _selectedTabIndex = selectedTabIndex;

    internal override void Render(IComposer composer)
    {
        var tabs = ComposableLambdas.Wrap2(composer, c => RenderChildren(c));
        ComposeBridges.ScrollableTabRow(_selectedTabIndex, BuildModifier(), tabs, composer);
    }
}
