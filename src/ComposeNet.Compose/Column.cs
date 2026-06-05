using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Foundation <c>Column</c> composable — lays its children out
/// vertically. Mirror of <see cref="Row"/>; same collection-init shape:
/// <code>
/// new Column { new Text("A"), new Text("B") }
/// </code>
/// Pass an optional <see cref="Arrangement"/> to control how children
/// are distributed along the vertical axis:
/// <code>
/// new Column(verticalArrangement: Arrangement.SpacedBy(8)) { /* … */ }
/// </code>
/// </summary>
public sealed class Column : ComposableContainer
{
    readonly Arrangement? _verticalArrangement;

    /// <summary>
    /// Lay out children top-to-bottom with Compose's default
    /// vertical arrangement (<c>Arrangement.Top</c>).
    /// </summary>
    public Column() : this(null) { }

    /// <summary>
    /// Lay out children top-to-bottom using
    /// <paramref name="verticalArrangement"/> to distribute them along
    /// the vertical axis. Pass <see langword="null"/> (or use the
    /// parameterless ctor) for Compose's default.
    /// </summary>
    /// <param name="verticalArrangement">
    /// One of the <see cref="Arrangement"/> static factories. Must wrap
    /// a vertical-capable Kotlin arrangement
    /// (<see cref="Arrangement.Top"/>, <see cref="Arrangement.Bottom"/>,
    /// <see cref="Arrangement.Center"/>, <see cref="Arrangement.SpaceBetween"/>,
    /// <see cref="Arrangement.SpaceAround"/>, <see cref="Arrangement.SpaceEvenly"/>,
    /// or <see cref="Arrangement.SpacedBy(int)"/>). Passing a
    /// horizontal-only arrangement (<see cref="Arrangement.Start"/>,
    /// <see cref="Arrangement.End"/>) throws
    /// <see cref="System.ArgumentException"/>.
    /// </param>
    public Column(Arrangement? verticalArrangement)
    {
        if (verticalArrangement is not null && verticalArrangement.Vertical is null)
        {
            throw new System.ArgumentException(
                $"{nameof(verticalArrangement)} must wrap a vertical " +
                $"or horizontal-or-vertical Compose Arrangement; got a " +
                $"horizontal-only value (e.g. Arrangement.Start / Arrangement.End).",
                nameof(verticalArrangement));
        }

        _verticalArrangement = verticalArrangement;
    }

    internal override void Render(IComposer composer)
    {
        var vertical = _verticalArrangement?.Vertical;
        var modifier = BuildModifier();

        int defaults = (int)ColumnDefault.All;
        if (modifier is not null) defaults &= ~(int)ColumnDefault.Modifier;
        if (vertical is not null) defaults &= ~(int)ColumnDefault.VerticalArrangement;

        var content = ComposableLambdas.Wrap3(composer, (scope, c) =>
        {
            using var __scopeFrame = RenderContext.PushScope(scope, ScopeKind.Column);
            RenderChildren(c);
        });

        ComposeBridges.Column(modifier, vertical, content, defaults, composer);
    }
}
