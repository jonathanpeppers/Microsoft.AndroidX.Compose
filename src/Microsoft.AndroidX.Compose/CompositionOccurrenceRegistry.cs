namespace AndroidX.Compose;

internal sealed class CompositionOccurrenceRegistry<TComposition> where TComposition : class
{
    readonly object _gate = new();
    readonly System.Collections.Generic.Dictionary<TComposition,
        System.Collections.Generic.Dictionary<(long Parent, string Site), CompositionOccurrencePool>> _compositions = [];

    internal int CompositionCount
    {
        get { lock (_gate) return _compositions.Count; }
    }

    internal object[] GetOwners()
    {
        lock (_gate)
        {
            System.Collections.Generic.List<object> owners = [];
            foreach (var sites in _compositions.Values)
                foreach (var pool in sites.Values)
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
            var sites = _compositions[composition];
            var pool = sites[(parent, site)];
            pool.Release(ordinal);
            if (pool.Count == 0)
                sites.Remove((parent, site));
            if (sites.Count == 0)
                _compositions.Remove(composition);
        }
    }
}
