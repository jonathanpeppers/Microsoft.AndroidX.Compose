using global::AndroidX.Compose.Runtime;
using Microsoft.AndroidX.Compose.Gallery.Registry;

namespace Microsoft.AndroidX.Compose.Gallery.Screens;

/// <summary>
/// One clickable row in <see cref="CategoryScreen"/>: the demo's title
/// and one-line description; tapping pushes <c>demo/{id}</c> on the
/// supplied <see cref="NavController"/>.
/// </summary>
public sealed class DemoRow : ComposableNode
{
    readonly Demo _demo;
    readonly NavController _nav;

    /// <summary>Create a row for <paramref name="demo"/>.</summary>
    public DemoRow(Demo demo, NavController nav)
    {
        _demo = demo;
        _nav  = nav;
    }

    public override void Render(IComposer composer)
    {
        new ListItem
        {
            Modifier = Modifier.Companion
                .FillMaxWidth()
                .Clickable(() => _nav.Navigate($"demo/{_demo.Id}")),
            Headline   = new Text(_demo.Title),
            Supporting = new Text(_demo.Description),
            Trailing   = new Text("›"),
        }.Render(composer);
        new HorizontalDivider().Render(composer);
    }
}
