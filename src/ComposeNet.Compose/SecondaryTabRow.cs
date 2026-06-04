using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>SecondaryTabRow</c> — the M3 variant of
/// <see cref="TabRow"/> with the secondary indicator style (full-width
/// underline). Use for tab groups nested inside a screen.
/// </summary>
public sealed class SecondaryTabRow : ComposableContainer
{
    readonly int _selectedTabIndex;
    public SecondaryTabRow(int selectedTabIndex) => _selectedTabIndex = selectedTabIndex;

    internal override void Render(IComposer composer)
    {
        var tabs = new ComposableLambda2(c => RenderChildren(c));
        ComposeBridges.SecondaryTabRow(_selectedTabIndex, BuildModifier(), tabs, composer);
    }
}
