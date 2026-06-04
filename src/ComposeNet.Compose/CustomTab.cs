using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>Tab</c> with fully custom content (the <c>Tab-bogVsAg</c>
/// overload that takes a ColumnScope-receiver content lambda — useful
/// when the text/icon slots on the regular <see cref="Tab"/> don't fit
/// your layout, e.g. multi-line labels or a leading dot indicator):
/// <code>
/// new CustomTab(selected: tab == 0, onClick: () =&gt; tab.Value = 0)
/// {
///     new Column { new Text("Inbox"), new Text("5 unread") },
/// }
/// </code>
/// Children are stacked vertically inside the tab's <c>ColumnScope</c>.
/// </summary>
public sealed class CustomTab : ComposableContainer
{
    readonly bool _selected;
    readonly System.Action _onClick;

    public CustomTab(bool selected, System.Action onClick)
    {
        _selected = selected;
        _onClick  = onClick;
    }

    internal override void Render(IComposer composer)
    {
        var click = new ComposableLambda0(_onClick);
        var content = ComposableLambdas.Wrap3(composer, (scope, c) =>
        {
            using var _ = RenderContext.PushScope(scope, ScopeKind.Column);
            RenderChildren(c);
        });

        ComposeBridges.TabContent(_selected, click, BuildModifier(), content, composer);
    }
}
