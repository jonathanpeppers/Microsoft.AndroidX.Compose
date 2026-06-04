using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>MediumTopAppBar</c> — a two-row top app bar that
/// collapses to a single row as the underlying scroll behavior
/// progresses. Same slots as <see cref="TopAppBar"/>.
/// </summary>
public sealed class MediumTopAppBar : ComposableNode
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
                "MediumTopAppBar.Title is required (the Kotlin parameter has no default).");

        var title = ComposableLambdas.Wrap2(composer, c => Title.Render(c));
        var nav = NavigationIcon is null ? null
            : ComposableLambdas.Wrap2(composer, c => NavigationIcon.Render(c));
        var actions = Actions is null ? null
            : ComposableLambdas.Wrap3(composer, c => Actions.Render(c));

        int defaults = (int)TwoRowsTopAppBarDefault.All;
        var modifier = BuildModifier();
        if (modifier is not null) defaults &= ~(int)TwoRowsTopAppBarDefault.Modifier;
        if (nav      is not null) defaults &= ~(int)TwoRowsTopAppBarDefault.NavigationIcon;
        if (actions  is not null) defaults &= ~(int)TwoRowsTopAppBarDefault.Actions;

        ComposeBridges.MediumTopAppBar(title, modifier, nav, actions, defaults, composer);
    }
}
