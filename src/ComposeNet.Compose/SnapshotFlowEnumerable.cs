using System.Collections.Generic;
using System.Threading;

namespace ComposeNet;

/// <summary>
/// Backing implementation for
/// <see cref="Compose.SnapshotFlow{T}(System.Func{T})"/>. Each call to
/// <see cref="GetAsyncEnumerator(CancellationToken)"/> spins up a
/// fresh Kotlin <c>snapshotFlow</c> collection so multiple consumers
/// of the same producer don't share state.
/// </summary>
internal sealed class SnapshotFlowEnumerable<T> : IAsyncEnumerable<T>
{
    readonly System.Func<T> _producer;

    public SnapshotFlowEnumerable(System.Func<T> producer)
    {
        _producer = producer;
    }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
        new SnapshotFlowEnumerator<T>(_producer, cancellationToken);
}
