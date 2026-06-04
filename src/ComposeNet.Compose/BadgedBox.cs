using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>BadgedBox</c>. Wraps a <see cref="Content"/> node
/// (typically an <see cref="Icon"/>) and overlays a <see cref="Badge"/>
/// on its top-end corner:
/// <code>
/// new BadgedBox
/// {
///     Badge   = new Badge { new Text("3") },
///     Content = new Icon(Resource.Drawable.ic_inbox, "Inbox"),
/// }
/// </code>
/// </summary>
public sealed class BadgedBox : ComposableNode
{
    /// <summary>Required: the badge to overlay on the content.</summary>
    public ComposableNode? Badge { get; set; }

    /// <summary>Required: the underlying content the badge attaches to.</summary>
    public ComposableNode? Content { get; set; }

    internal override void Render(IComposer composer)
    {
        if (Badge is null || Content is null)
            throw new System.InvalidOperationException(
                "BadgedBox requires both Badge and Content.");

        var badge   = new ComposableLambda3(c => Badge.Render(c));
        var content = new ComposableLambda3(c => Content.Render(c));

        ComposeBridges.BadgedBox(badge, BuildModifier(), content, composer);
    }
}
