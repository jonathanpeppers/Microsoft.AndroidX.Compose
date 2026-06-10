using AndroidX.Compose.Runtime;

namespace AndroidX.Compose;

/// <summary>
/// Composer-aware <see cref="ComposableNode"/> that defers tree
/// construction until <see cref="Render(IComposer)"/> runs. The wrapped
/// <see cref="Func{T, TResult}"/> receives the active
/// <see cref="IComposer"/> so the builder can read snapshot-state
/// values, call <see cref="ComposeExtensions.ColorScheme(IComposer)"/>,
/// or invoke any other API that takes an <see cref="IComposer"/>.
/// </summary>
/// <remarks>
/// <para>
/// Use this where you'd otherwise need to subclass
/// <see cref="ComposableNode"/> and override
/// <see cref="ComposableNode.Render(IComposer)"/> — for example a
/// sample-side helper that needs to thread the composer into a static
/// builder method:
/// </para>
/// <code>
/// new Composed(c =&gt;
/// {
///     var scheme = c.ColorScheme();
///     return new Text("Hello")
///     {
///         Color = Color.FromArgb(scheme.OnSurfaceVariant),
///     };
/// })
/// </code>
/// <para>
/// The wrapped builder is invoked once per composition pass; the
/// returned subtree is then rendered with the same
/// <see cref="IComposer"/> instance. The builder is not memoized — if
/// you need to cache a subtree across recompositions, wrap the
/// per-render allocation behind <c>composer.Remember(...)</c>.
/// </para>
/// <para>
/// Returning <see langword="null"/> from the builder is allowed and
/// renders nothing.
/// </para>
/// </remarks>
public sealed class Composed : ComposableNode
{
    readonly Func<IComposer, ComposableNode?> _builder;

    /// <summary>
    /// Create a composer-aware wrapper around <paramref name="builder"/>.
    /// </summary>
    public Composed(Func<IComposer, ComposableNode?> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        _builder = builder;
    }

    /// <inheritdoc />
    public override void Render(IComposer composer)
    {
        var node = _builder(composer);
        node?.Render(composer);
    }
}
