namespace Microsoft.AndroidX.Compose.Maui.Handlers;

internal sealed class ReferenceItemCache<TValue>
    where TValue : class
{
    readonly System.Collections.Generic.Dictionary<object, TValue> _values =
        new(System.Collections.Generic.ReferenceEqualityComparer.Instance);

    public TValue GetOrReplace(
        object key,
        System.Func<TValue, bool> matches,
        System.Func<TValue> factory,
        System.Action<TValue>? onRemoved = null)
    {
        System.ArgumentNullException.ThrowIfNull(key);
        System.ArgumentNullException.ThrowIfNull(matches);
        System.ArgumentNullException.ThrowIfNull(factory);

        if (_values.TryGetValue(key, out var existing))
        {
            if (matches(existing))
                return existing;
            _values.Remove(key);
            onRemoved?.Invoke(existing);
        }

        var value = factory();
        _values.Add(key, value);
        return value;
    }

    public void Prune(
        System.Collections.Generic.IReadOnlyList<object> liveKeys,
        System.Action<TValue>? onRemoved = null)
    {
        System.ArgumentNullException.ThrowIfNull(liveKeys);
        var live = new System.Collections.Generic.HashSet<object>(
            liveKeys,
            System.Collections.Generic.ReferenceEqualityComparer.Instance);
        foreach (var key in System.Linq.Enumerable.ToArray(_values.Keys))
        {
            if (live.Contains(key))
                continue;
            var value = _values[key];
            _values.Remove(key);
            onRemoved?.Invoke(value);
        }
    }

    public void Clear(System.Action<TValue>? onRemoved = null)
    {
        foreach (var value in _values.Values)
            onRemoved?.Invoke(value);
        _values.Clear();
    }
}
