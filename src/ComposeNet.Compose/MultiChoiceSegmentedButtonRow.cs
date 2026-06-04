using AndroidX.Compose.Material3;
using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>MultiChoiceSegmentedButtonRow</c>. Container for
/// <see cref="SegmentedButton"/> children with toggle (multi-select)
/// semantics — see <see cref="SingleChoiceSegmentedButtonRow"/> for the
/// radio-button variant.
/// </summary>
public sealed class MultiChoiceSegmentedButtonRow : ComposableContainer
{
    internal override void Render(IComposer composer)
    {
        var content = ComposableLambdas.Wrap3(composer, (scope, c) =>
        {
            using var _    = RenderContext.PushScope(scope);
            using var rows = RenderContext.PushRow(Children.Count);
            for (int i = 0; i < Children.Count; i++)
            {
                rows.SetIndex(i);
                c.StartReplaceableGroup(i);
                try { Children[i].Render(c); }
                finally { c.EndReplaceableGroup(); }
            }
        });

        var modifier = BuildModifier();
        int defaults = (int)SegmentedButtonRowDefault.All;
        if (modifier is not null) defaults &= ~(int)SegmentedButtonRowDefault.Modifier;

        SegmentedButtonKt.MultiChoiceSegmentedButtonRow(
            modifier:  modifier,
            space:     0f,
            content:   content,
            _composer: composer,
            p4:        0,
            _changed:  defaults);
    }
}
