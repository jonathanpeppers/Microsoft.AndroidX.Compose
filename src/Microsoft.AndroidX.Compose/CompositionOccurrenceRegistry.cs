namespace AndroidX.Compose;

internal sealed class CompositionOccurrenceRegistry<TComposition> where TComposition : class
{
    readonly object _gate = new();
    // Native forgetting can stop at another observer's exception. A value-to-key cycle must not become a permanent root.
    readonly System.Runtime.CompilerServices.ConditionalWeakTable<TComposition,
        System.Collections.Generic.Dictionary<(long Parent, string Site), CompositionOccurrencePool>> _compositions = new();

    internal int CompositionCount
    {
        get
        {
            lock (_gate)
            {
                int count = 0;
                foreach (var entry in _compositions)
                    count++;
                return count;
            }
        }
    }

    internal object[] GetOwners()
    {
        lock (_gate)
        {
            System.Collections.Generic.List<object> owners = [];
            foreach (var entry in _compositions)
                foreach (var pool in entry.Value.Values)
                    owners.AddRange(pool.Owners);
            return owners.ToArray();
        }
    }

    internal int Acquire(TComposition composition, long parent, string site, object? owner = null)
    {
        lock (_gate)
        {
            if (!_compositions.TryGetValue(composition, out var sites))
                _compositions.Add(composition, sites = []);
            if (!sites.TryGetValue((parent, site), out var pool))
                sites.Add((parent, site), pool = new());
            return pool.Acquire(owner);
        }
    }

    internal void Release(TComposition composition, long parent, string site, int ordinal)
    {
        lock (_gate)
        {
            if (!_compositions.TryGetValue(composition, out var sites))
                throw new System.InvalidOperationException("Occurrence composition pool is missing during release.");
            var pool = sites[(parent, site)];
            pool.Release(ordinal);
            if (pool.Count == 0)
                sites.Remove((parent, site));
            if (sites.Count == 0)
                _compositions.Remove(composition);
        }
    }
}
