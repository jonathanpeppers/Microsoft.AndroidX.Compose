using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>LargeFlexibleTopAppBar</c> — a two-row top app bar
/// similar to <see cref="LargeTopAppBar"/> but with a configurable
/// expanded height and an optional second-line <see cref="Subtitle"/>
/// slot.
/// </summary>
public sealed class LargeFlexibleTopAppBar : ComposableNode
{
    /// <summary>Required: the bar's title slot.</summary>
    public ComposableNode? Title { get; set; }

    /// <summary>Optional: second-line slot below the title.</summary>
    public ComposableNode? Subtitle { get; set; }

    /// <summary>Optional: leading slot.</summary>
    public ComposableNode? NavigationIcon { get; set; }

    /// <summary>Optional: trailing slot, laid out in a <c>RowScope</c>.</summary>
    public ComposableNode? Actions { get; set; }

    internal override void Render(IComposer composer)
    {
        if (Title is null)
            throw new System.InvalidOperationException(
                "LargeFlexibleTopAppBar.Title is required (the Kotlin parameter has no default).");

        var title = new ComposableLambda2(c => Title.Render(c));
        ComposableLambda2? subtitle = Subtitle is null ? null
            : new ComposableLambda2(c => Subtitle.Render(c));
        ComposableLambda2? nav = NavigationIcon is null ? null
            : new ComposableLambda2(c => NavigationIcon.Render(c));
        ComposableLambda3? actions = Actions is null ? null
            : new ComposableLambda3(c => Actions.Render(c));

        int defaults = (int)FlexibleTopAppBarDefault.All;
        var modifier = BuildModifier();
        if (modifier is not null) defaults &= ~(int)FlexibleTopAppBarDefault.Modifier;
        if (subtitle is not null) defaults &= ~(int)FlexibleTopAppBarDefault.Subtitle;
        if (nav      is not null) defaults &= ~(int)FlexibleTopAppBarDefault.NavigationIcon;
        if (actions  is not null) defaults &= ~(int)FlexibleTopAppBarDefault.Actions;

        ComposeBridges.LargeFlexibleTopAppBar(
            title, modifier, subtitle, nav, actions, defaults, composer);
    }
}
