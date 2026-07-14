using System.ComponentModel;
using AndroidX.Compose.Runtime;

namespace AndroidX.Compose;

/// <summary>Restores the composer that preceded an implicit composition scope.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public readonly struct ComposableContextScope : IDisposable
{
    readonly IComposer? _previous;

    internal ComposableContextScope(IComposer? previous) => _previous = previous;

    /// <summary>Restores the preceding composer.</summary>
    public void Dispose() => ComposableContext.Restore(_previous);
}
