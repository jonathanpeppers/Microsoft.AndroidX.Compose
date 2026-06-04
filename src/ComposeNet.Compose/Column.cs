using AndroidX.Compose.Foundation.Layout;
using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>Foundation <c>Column</c> composable.</summary>
public sealed class Column : ComposableContainer
{
    internal override void Render(IComposer composer)
    {
        var modifier = BuildModifier();
        var content = ComposableLambdas.Wrap3(composer, (scope, c) =>
        {
            using var _ = RenderContext.PushScope(scope, ScopeKind.Column);
            RenderChildren(c);
        });
        int defaults = (int)ColumnDefault.All;
        if (modifier is not null) defaults &= ~(int)ColumnDefault.Modifier;
        ColumnKt.Column(
            modifier:            modifier,
            verticalArrangement: null,
            horizontalAlignment: null,
            content:             content,
            _composer:           composer,
            p5:                  0,
            _changed:            defaults);
    }
}
