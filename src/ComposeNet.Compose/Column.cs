using AndroidX.Compose.Foundation.Layout;
using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>Foundation <c>Column</c> composable.</summary>
public sealed class Column : ComposableContainer
{
    internal override void Render(IComposer composer)
    {
        var content = new ComposableLambda3(c => RenderChildren(c));
        ColumnKt.Column(
            modifier:            null,
            verticalArrangement: null,
            horizontalAlignment: null,
            content:             content,
            _composer:           composer,
            p5:                  0,
            _changed:            (int)ColumnDefault.All);
    }
}
