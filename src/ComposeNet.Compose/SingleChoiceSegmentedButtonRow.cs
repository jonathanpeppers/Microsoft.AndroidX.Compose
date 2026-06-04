using AndroidX.Compose.Material3;
using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>SingleChoiceSegmentedButtonRow</c>. Container for
/// <see cref="SegmentedButton"/> children that exposes a
/// <c>SingleChoiceSegmentedButtonRowScope</c> receiver — the scope is
/// captured here and published via <see cref="RenderContext"/> so child
/// <see cref="SegmentedButton"/>s can pass it to the underlying
/// scope-extension Kotlin static.
/// <code>
/// new SingleChoiceSegmentedButtonRow
/// {
///     new SegmentedButton(selected: tab == 0, onClick: () =&gt; tab.Value = 0) { new Text("Day") },
///     new SegmentedButton(selected: tab == 1, onClick: () =&gt; tab.Value = 1) { new Text("Week") },
/// }
/// </code>
/// </summary>
public sealed class SingleChoiceSegmentedButtonRow : ComposableContainer
{
    internal override void Render(IComposer composer)
    {
        var content = new ComposableLambda3((scope, c) =>
        {
            using var _    = RenderContext.PushScope(scope);
            using var rows = RenderContext.PushRow(Children.Count);
            for (int i = 0; i < Children.Count; i++)
            {
                rows.SetIndex(i);
                Children[i].Render(c);
            }
        });

        var modifier = BuildModifier();
        int defaults = (int)SegmentedButtonRowDefault.All;
        if (modifier is not null) defaults &= ~(int)SegmentedButtonRowDefault.Modifier;

        SegmentedButtonKt.SingleChoiceSegmentedButtonRow(
            modifier:  modifier,
            space:     0f,
            content:   content,
            _composer: composer,
            p4:        0,
            _changed:  defaults);
    }
}
