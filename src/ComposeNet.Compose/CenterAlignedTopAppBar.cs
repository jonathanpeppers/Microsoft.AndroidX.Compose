using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>CenterAlignedTopAppBar</c> — same shape as
/// <see cref="TopAppBar"/> but centers the <see cref="Title"/>
/// horizontally between the navigation icon and actions.
/// </summary>
public sealed class CenterAlignedTopAppBar : ComposableNode
{
    /// <summary>Required: the bar's title slot, rendered centered.</summary>
    public ComposableNode? Title { get; set; }

    /// <summary>Optional: leading slot.</summary>
    public ComposableNode? NavigationIcon { get; set; }

    /// <summary>Optional: trailing slot, laid out in a <c>RowScope</c>.</summary>
    public ComposableNode? Actions { get; set; }

    internal override void Render(IComposer composer)
    {
        if (Title is null)
            throw new System.InvalidOperationException(
                "CenterAlignedTopAppBar.Title is required (the Kotlin parameter has no default).");

        var title = new ComposableLambda2(c => Title.Render(c));
        ComposableLambda2? nav = NavigationIcon is null ? null
            : new ComposableLambda2(c => NavigationIcon.Render(c));
        ComposableLambda3? actions = Actions is null ? null
            : new ComposableLambda3(c => Actions.Render(c));

        int defaults = (int)TopAppBarDefault.All;
        var modifier = BuildModifier();
        if (modifier is not null) defaults &= ~(int)TopAppBarDefault.Modifier;
        if (nav      is not null) defaults &= ~(int)TopAppBarDefault.NavigationIcon;
        if (actions  is not null) defaults &= ~(int)TopAppBarDefault.Actions;

        ComposeBridges.CenterAlignedTopAppBar(title, modifier, nav, actions, defaults, composer);
    }
}
