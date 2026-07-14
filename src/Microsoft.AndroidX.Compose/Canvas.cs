using AndroidX.Compose.Foundation;
using AndroidX.Compose.Runtime;

namespace AndroidX.Compose;

/// <summary>Composable drawing surface backed by Compose's <c>Canvas</c>.</summary>
public sealed class Canvas : ComposableNode
{
    readonly DrawScopeCallback _callback;

    /// <summary>Creates a canvas that runs <paramref name="draw"/> on each draw pass.</summary>
    public Canvas(Action<DrawScope> draw)
    {
        ArgumentNullException.ThrowIfNull(draw);
        _callback = new DrawScopeCallback(draw);
    }

    /// <inheritdoc/>
    public override void Render(IComposer composer)
    {
        var modifier = BuildModifier() ?? Modifier.BuildEmpty();
        CanvasKt.Canvas(modifier, _callback, composer, 0);
    }
}
