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
            using var _    = RenderContext.PushScope(scope, ScopeKind.Other);
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

        // The bound binding's parameter names are misleading. The JNI
        // descriptor is `(...;Composer;II)V` — two trailing `I` slots —
        // and the Kotlin layout for a `@Composable` with defaults is
        // `(...userParams, Composer, $changed, $default)`. So
        // positionally `p4` is `$changed` and `_changed` is the
        // `$default` mask. Pass `0` for `$changed` (pessimistic — every
        // param treated as new) and the `SegmentedButtonRowDefault`
        // bitmask for `$default`.
        SegmentedButtonKt.MultiChoiceSegmentedButtonRow(
            modifier:  modifier,
            space:     0f,
            content:   content,
            _composer: composer,
            p4:        0,
            _changed:  defaults);
    }
}
