using AndroidX.Compose.Runtime;

namespace AndroidX.Compose;

/// <summary>
/// Adapter <see cref="ComposableNode"/> that delegates its
/// <c>Render</c> to a callback so Tier 2 content lambdas can be
/// embedded inside collection-init tree-style containers.
/// </summary>
internal sealed class Tier2InlineContent : ComposableNode
{
    readonly Action<IComposer> _body;

    public Tier2InlineContent(Action<IComposer> body)
    {
        ArgumentNullException.ThrowIfNull(body);
        _body = body;
    }

    public static ComposableNode? Create(Action<IComposer>? body) =>
        body is null ? null : new Tier2InlineContent(body);

    public override void Render(IComposer composer)
    {
        ArgumentNullException.ThrowIfNull(composer);
        using var scope = ComposableContext.Enter(composer);
        _body(composer);
    }

    internal static void RenderDirect(IComposer composer, Action<IComposer> body, bool indexed)
    {
        ArgumentNullException.ThrowIfNull(composer);
        ArgumentNullException.ThrowIfNull(body);
        using var rows = indexed ? RenderContext.PushRow(1) : default;
        if (indexed)
            rows.SetIndex(0);
        composer.StartReplaceableGroup(HashCode.Combine(0, typeof(Tier2InlineContent)));
        try
        {
            using var scope = ComposableContext.Enter(composer);
            body(composer);
        }
        finally
        {
            composer.EndReplaceableGroup();
        }
    }

    internal static void RenderDirect(IComposer composer, Action body, bool indexed)
    {
        ArgumentNullException.ThrowIfNull(composer);
        ArgumentNullException.ThrowIfNull(body);
        using var rows = indexed ? RenderContext.PushRow(1) : default;
        if (indexed)
            rows.SetIndex(0);
        composer.StartReplaceableGroup(HashCode.Combine(0, typeof(Tier2InlineContent)));
        try
        {
            using var scope = ComposableContext.Enter(composer);
            body();
        }
        finally
        {
            composer.EndReplaceableGroup();
        }
    }
}
