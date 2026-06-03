using AndroidX.Compose.Foundation.Layout;
using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Foundation <c>Box</c> composable — stacks its children on top of
/// each other (later children draw above earlier ones). Useful for
/// overlays, badges, and absolute positioning via alignment-modifier
/// chains. Always uses the 7-param Kotlin overload so children are
/// rendered through the content slot.
/// </summary>
public sealed class Box : ComposableContainer
{
    internal override void Render(IComposer composer)
    {
        var modifier = BuildModifier();
        var content  = new ComposableLambda3(c => RenderChildren(c));

        int defaults = (int)BoxDefault.All;
        if (modifier is not null) defaults &= ~(int)BoxDefault.Modifier;

        BoxKt.Box(
            modifier:                 modifier,
            contentAlignment:         null,
            propagateMinConstraints:  false,
            content:                  content,
            _composer:                composer,
            p5:                       0,
            _changed:                 defaults);
    }
}
