namespace AndroidX.Compose;

internal sealed class CompositionOccurrencePool
{
    readonly System.Collections.Generic.List<object?> _owners = [];
    System.Collections.Generic.SortedSet<int>? _free;

    internal int Count => _owners.Count - (_free?.Count ?? 0);
    internal System.Collections.Generic.IEnumerable<object> Owners
    {
        get
        {
            foreach (var owner in _owners)
                if (owner is not null)
                    yield return owner;
        }
    }

    internal int Acquire(object? owner = null)
    {
        if (_free is not { Count: > 0 })
        {
            _owners.Add(owner ?? this);
            return _owners.Count - 1;
        }
        int ordinal = _free.Min;
        _free.Remove(ordinal);
        _owners[ordinal] = owner ?? this;
        return ordinal;
    }

    internal void Release(int ordinal)
    {
        if (ordinal < 0 || ordinal >= _owners.Count || _owners[ordinal] is null)
            throw new System.InvalidOperationException("Composition occurrence was already released.");
        _owners[ordinal] = null;
        if (ordinal < _owners.Count - 1)
            (_free ??= []).Add(ordinal);
        while (_owners.Count > 0 && _owners[^1] is null)
        {
            _free?.Remove(_owners.Count - 1);
            _owners.RemoveAt(_owners.Count - 1);
        }
    }
}
