using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>PrimaryTabRow</c> — the M3 variant of
/// <see cref="TabRow"/> with the primary indicator style (rounded pill
/// under the selected tab). Use when the tabs are the top-level
/// destination switcher of a screen.
/// </summary>
public sealed class PrimaryTabRow : ComposableContainer
{
    readonly int _selectedTabIndex;
    public PrimaryTabRow(int selectedTabIndex) => _selectedTabIndex = selectedTabIndex;

    internal override void Render(IComposer composer)
    {
        var tabs = new ComposableLambda2(c => RenderChildren(c));
        ComposeBridges.PrimaryTabRow(_selectedTabIndex, BuildModifier(), tabs, composer);
    }
}
