using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Tree-syntax wrapper around
/// <see cref="Compose.LaunchedEffect(object?, System.Func{System.Threading.CancellationToken, System.Threading.Tasks.Task})"/>.
/// Launches <c>Body</c> as a C# <see cref="System.Threading.Tasks.Task"/>
/// on first composition and any time a key changes; cancels the
/// supplied <see cref="System.Threading.CancellationToken"/> on key
/// change or when this node leaves the composition.
/// </summary>
/// <remarks>
/// <code>
/// new LaunchedEffect(_sessionId, async ct =&gt;
/// {
///     while (!ct.IsCancellationRequested)
///     {
///         await Task.Delay(1000, ct);
///         _ticks.Value++;
///     }
/// })
/// </code>
/// </remarks>
public sealed class LaunchedEffect : ComposableNode
{
    readonly object? _key1, _key2, _key3;
    readonly int _keyCount;
    readonly System.Func<System.Threading.CancellationToken, System.Threading.Tasks.Task> _body;

    /// <summary>Single-key form.</summary>
    public LaunchedEffect(
        object? key1,
        System.Func<System.Threading.CancellationToken, System.Threading.Tasks.Task> body)
    {
        System.ArgumentNullException.ThrowIfNull(body);
        _key1 = key1; _keyCount = 1; _body = body;
    }

    /// <summary>Two-key form.</summary>
    public LaunchedEffect(
        object? key1,
        object? key2,
        System.Func<System.Threading.CancellationToken, System.Threading.Tasks.Task> body)
    {
        System.ArgumentNullException.ThrowIfNull(body);
        _key1 = key1; _key2 = key2; _keyCount = 2; _body = body;
    }

    /// <summary>Three-key form.</summary>
    public LaunchedEffect(
        object? key1,
        object? key2,
        object? key3,
        System.Func<System.Threading.CancellationToken, System.Threading.Tasks.Task> body)
    {
        System.ArgumentNullException.ThrowIfNull(body);
        _key1 = key1; _key2 = key2; _key3 = key3; _keyCount = 3; _body = body;
    }

    public override void Render(IComposer composer)
    {
        switch (_keyCount)
        {
            case 1: Compose.LaunchedEffect(_key1, _body); break;
            case 2: Compose.LaunchedEffect(_key1, _key2, _body); break;
            default: Compose.LaunchedEffect(_key1, _key2, _key3, _body); break;
        }
    }
}
