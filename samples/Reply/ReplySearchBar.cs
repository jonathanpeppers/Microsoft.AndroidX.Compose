using System;
using AndroidX.Compose.Runtime;
using ComposeNet;

namespace ComposeNet.Samples.Reply;

/// <summary>
/// Simplified top "search" entry used by <see cref="ReplyInboxScreen"/>.
/// The upstream sample uses
/// <c>DockedSearchBar</c>, which isn't bound in
/// <c>Xamarin.AndroidX.Compose.Material3</c>; this is a placeholder row
/// with the same visual footprint so the inbox list visually matches.
/// </summary>
public sealed class ReplySearchBar : ComposableNode
{
    /// <inheritdoc />
    public override void Render(IComposer composer)
    {
        var node = new Composed(c =>
        {
            var scheme = MaterialTheme.CurrentColorScheme(c);
            return new Box
            {
                Modifier.Companion
                    .FillMaxWidth()
                    .Padding(horizontal: 16, vertical: 8)
                    .Clip(Shape.RoundedCorners(28))
                    .Background(new Color(scheme.SurfaceVariant)),
                new Row
                {
                    Modifier.Companion.FillMaxWidth().Padding(horizontal: 16, vertical: 12),
                    new Icon(Resource.Drawable.ic_search, "Search")
                    {
                        TintArgb = scheme.OnSurface,
                    },
                    new Text("Search replies")
                    {
                        FontSize = 14,
                        Color    = new Color(scheme.OnSurface),
                        Modifier = Modifier.Companion.Padding(start: 16, end: 0, top: 0, bottom: 0).Weight(1f),
                    },
                    ReplyProfileImage.Build(
                        drawableResource: Resource.Drawable.avatar_6,
                        description:      "Profile"),
                },
            };
        });
        node.Render(composer);
    }
}
