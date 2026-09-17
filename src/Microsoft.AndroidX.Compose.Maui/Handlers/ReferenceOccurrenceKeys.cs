namespace Microsoft.AndroidX.Compose.Maui.Handlers;

internal sealed class ReferenceOccurrenceKeys
{
    readonly System.Collections.Generic.Dictionary<
        object,
        System.Collections.Generic.List<ReferenceOccurrenceKey>> _keys =
        new(System.Collections.Generic.ReferenceEqualityComparer.Instance);
    long _nextValue = 1;

    public ReferenceOccurrenceKey[] GetKeys(
        System.Collections.Generic.IReadOnlyList<object> items)
    {
        System.ArgumentNullException.ThrowIfNull(items);
        var counts = new System.Collections.Generic.Dictionary<object, int>(
            System.Collections.Generic.ReferenceEqualityComparer.Instance);
        var result = new ReferenceOccurrenceKey[items.Count];
        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            counts.TryGetValue(item, out int occurrence);
            counts[item] = occurrence + 1;
            if (!_keys.TryGetValue(item, out var occurrences))
            {
                occurrences = [];
                _keys.Add(item, occurrences);
            }
            while (occurrences.Count <= occurrence)
                occurrences.Add(new ReferenceOccurrenceKey(_nextValue++));
            result[i] = occurrences[occurrence];
        }

        foreach (var item in System.Linq.Enumerable.ToArray(_keys.Keys))
        {
            if (!counts.TryGetValue(item, out int liveCount))
            {
                _keys.Remove(item);
                continue;
            }
            var occurrences = _keys[item];
            if (occurrences.Count > liveCount)
            {
                occurrences.RemoveRange(
                    liveCount,
                    occurrences.Count - liveCount);
            }
        }
        return result;
    }

    public void Clear()
    {
        _keys.Clear();
        _nextValue = 1;
    }
}

internal sealed record ReferenceOccurrenceKey(long Value);
