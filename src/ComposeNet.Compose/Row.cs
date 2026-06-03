using AndroidX.Compose.Foundation.Layout;
using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Foundation <c>Row</c> composable — lays its children out
/// horizontally. Mirror of <see cref="Column"/>; same collection-init
/// shape:
/// <code>
/// new Row { new Text("A"), new Spacer { Modifier = ... }, new Text("B") }
/// </code>
/// </summary>
public sealed class Row : ComposableContainer
{
    internal override void Render(IComposer composer)
    {
        var modifier = BuildModifier();
        var content  = new ComposableLambda3(c => RenderChildren(c));

        int defaults = (int)RowDefault.All;
        if (modifier is not null) defaults &= ~(int)RowDefault.Modifier;

        RowKt.Row(
            modifier:              modifier,
            horizontalArrangement: null,
            verticalAlignment:     null,
            content:               content,
            _composer:             composer,
            p5:                    0,
            _changed:              defaults);
    }
}
