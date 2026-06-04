using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>LargeTopAppBar</c> — a taller variant of
/// <see cref="MediumTopAppBar"/> that gives the title a full-width row
/// when expanded. Same slots as <see cref="TopAppBar"/>.
/// </summary>
public sealed class LargeTopAppBar : ComposableNode
{
    /// <summary>Required: the bar's title slot.</summary>
    public ComposableNode? Title { get; set; }

    /// <summary>Optional: leading slot.</summary>
    public ComposableNode? NavigationIcon { get; set; }

    /// <summary>Optional: trailing slot, laid out in a <c>RowScope</c>.</summary>
    public ComposableNode? Actions { get; set; }

    internal override void Render(IComposer composer)
    {
        if (Title is null)
            throw new System.InvalidOperationException(
                "LargeTopAppBar.Title is required (the Kotlin parameter has no default).");

        var title = new ComposableLambda2(c => Title.Render(c));
        ComposableLambda2? nav = NavigationIcon is null ? null
            : new ComposableLambda2(c => NavigationIcon.Render(c));
        ComposableLambda3? actions = Actions is null ? null
            : new ComposableLambda3(c => Actions.Render(c));

        int defaults = (int)TwoRowsTopAppBarDefault.All;
        var modifier = BuildModifier();
        if (modifier is not null) defaults &= ~(int)TwoRowsTopAppBarDefault.Modifier;
        if (nav      is not null) defaults &= ~(int)TwoRowsTopAppBarDefault.NavigationIcon;
        if (actions  is not null) defaults &= ~(int)TwoRowsTopAppBarDefault.Actions;

        ComposeBridges.LargeTopAppBar(title, modifier, nav, actions, defaults, composer);
    }
}
