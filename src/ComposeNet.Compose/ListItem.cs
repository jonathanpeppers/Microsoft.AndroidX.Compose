using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>ListItem</c>. A row in a list with a required
/// <see cref="Headline"/> and four optional content slots
/// (<see cref="Overline"/>, <see cref="Supporting"/>,
/// <see cref="Leading"/>, <see cref="Trailing"/>):
/// <code>
/// new ListItem
/// {
///     Headline   = new Text("Inbox"),
///     Supporting = new Text("3 unread"),
///     Leading    = new Icon(Resource.Drawable.ic_inbox, "Inbox"),
///     Trailing   = new Text("→"),
/// }
/// </code>
/// </summary>
public sealed class ListItem : ComposableNode
{
    /// <summary>Required: primary text slot.</summary>
    public ComposableNode? Headline { get; set; }

    /// <summary>Optional: text rendered above the headline.</summary>
    public ComposableNode? Overline { get; set; }

    /// <summary>Optional: secondary text rendered below the headline.</summary>
    public ComposableNode? Supporting { get; set; }

    /// <summary>Optional: leading slot, typically an icon or avatar.</summary>
    public ComposableNode? Leading { get; set; }

    /// <summary>Optional: trailing slot.</summary>
    public ComposableNode? Trailing { get; set; }

    internal override void Render(IComposer composer)
    {
        if (Headline is null)
            throw new System.InvalidOperationException(
                "ListItem.Headline is required (the Kotlin parameter has no default).");

        var headline = ComposableLambdas.Wrap2(composer, c => Headline.Render(c));
        var overline   = Overline   is null ? null : ComposableLambdas.Wrap2(composer, c => Overline.Render(c));
        var supporting = Supporting is null ? null : ComposableLambdas.Wrap2(composer, c => Supporting.Render(c));
        var leading    = Leading    is null ? null : ComposableLambdas.Wrap2(composer, c => Leading.Render(c));
        var trailing   = Trailing   is null ? null : ComposableLambdas.Wrap2(composer, c => Trailing.Render(c));

        int defaults = (int)ListItemDefault.All;
        var modifier = BuildModifier();
        if (modifier   is not null) defaults &= ~(int)ListItemDefault.Modifier;
        if (overline   is not null) defaults &= ~(int)ListItemDefault.OverlineContent;
        if (supporting is not null) defaults &= ~(int)ListItemDefault.SupportingContent;
        if (leading    is not null) defaults &= ~(int)ListItemDefault.LeadingContent;
        if (trailing   is not null) defaults &= ~(int)ListItemDefault.TrailingContent;

        ComposeBridges.ListItem(headline, modifier, overline, supporting, leading, trailing, defaults, composer);
    }
}
