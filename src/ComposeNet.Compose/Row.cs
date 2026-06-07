using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Foundation <c>Row</c> composable — lays its children out
/// horizontally. Mirror of <see cref="Column"/>; same collection-init
/// shape:
/// <code>
/// new Row { new Text("A"), new Spacer { Modifier = ... }, new Text("B") }
/// </code>
/// Pass an optional <see cref="Arrangement"/> to control how children
/// are distributed along the horizontal axis:
/// <code>
/// new Row(horizontalArrangement: Arrangement.SpaceBetween) { /* … */ }
/// </code>
/// </summary>
public sealed class Row : ComposableContainer
{
    readonly Arrangement? _horizontalArrangement;

    /// <summary>
    /// Lay out children left-to-right with Compose's default
    /// horizontal arrangement (<c>Arrangement.Start</c>).
    /// </summary>
    public Row() : this(null) { }

    /// <summary>
    /// Lay out children left-to-right using
    /// <paramref name="horizontalArrangement"/> to distribute them along
    /// the horizontal axis. Pass <see langword="null"/> (or use the
    /// parameterless ctor) for Compose's default.
    /// </summary>
    /// <param name="horizontalArrangement">
    /// One of the <see cref="Arrangement"/> static factories. Must wrap
    /// a horizontal-capable Kotlin arrangement
    /// (<see cref="Arrangement.Start"/>, <see cref="Arrangement.End"/>,
    /// <see cref="Arrangement.Center"/>, <see cref="Arrangement.SpaceBetween"/>,
    /// <see cref="Arrangement.SpaceAround"/>, <see cref="Arrangement.SpaceEvenly"/>,
    /// or <see cref="Arrangement.SpacedBy(int)"/>). Passing a
    /// vertical-only arrangement (<see cref="Arrangement.Top"/>,
    /// <see cref="Arrangement.Bottom"/>) throws
    /// <see cref="System.ArgumentException"/>.
    /// </param>
    public Row(Arrangement? horizontalArrangement)
    {
        if (horizontalArrangement is not null && horizontalArrangement.Horizontal is null)
        {
            throw new System.ArgumentException(
                $"{nameof(horizontalArrangement)} must wrap a horizontal " +
                $"or horizontal-or-vertical Compose Arrangement; got a " +
                $"vertical-only value (e.g. Arrangement.Top / Arrangement.Bottom).",
                nameof(horizontalArrangement));
        }

        _horizontalArrangement = horizontalArrangement;
    }

    public override void Render(IComposer composer)
    {
        var horizontal = _horizontalArrangement?.Horizontal;
        var modifier = BuildModifier();

        int defaults = (int)RowDefault.All;
        if (modifier is not null)   defaults &= ~(int)RowDefault.Modifier;
        if (horizontal is not null) defaults &= ~(int)RowDefault.HorizontalArrangement;

        var content = ComposableLambdas.Wrap3(composer, (scope, c) =>
        {
            using var __scopeFrame = RenderContext.PushScope(scope, ScopeKind.Row);
            RenderChildren(c);
        });

        ComposeBridges.Row(modifier, horizontal, content, defaults, composer);
    }
}
