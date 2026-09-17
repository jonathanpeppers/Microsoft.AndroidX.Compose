namespace Microsoft.AndroidX.Compose.Maui.Handlers;

internal sealed class ReferenceOccurrenceKeys
{
    readonly System.Collections.Generic.List<Entry> _entries = [];
    long _nextValue = 1;

    public ReferenceOccurrenceKey[] GetKeys(
        System.Collections.Generic.IReadOnlyList<object> items)
    {
        System.ArgumentNullException.ThrowIfNull(items);
        var available = new System.Collections.Generic.Dictionary<
            ItemIdentityKey,
            System.Collections.Generic.Queue<Entry>>();
        foreach (var entry in _entries)
        {
            var identity = new ItemIdentityKey(entry.Item);
            if (!available.TryGetValue(identity, out var queue))
            {
                queue = new System.Collections.Generic.Queue<Entry>();
                available.Add(identity, queue);
            }
            queue.Enqueue(entry);
        }
        var next = new System.Collections.Generic.List<Entry>(items.Count);
        var result = new ReferenceOccurrenceKey[items.Count];
        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            Entry entry;
            var identity = new ItemIdentityKey(item);
            if (available.TryGetValue(identity, out var queue) &&
                queue.Count > 0)
            {
                entry = queue.Dequeue();
            }
            else
            {
                entry = NewEntry(item);
            }
            next.Add(entry);
            result[i] = entry.Key;
        }

        _entries.Clear();
        _entries.AddRange(next);
        return result;
    }

    public void ApplyCollectionChanged(
        System.Collections.Specialized.NotifyCollectionChangedEventArgs change)
    {
        System.ArgumentNullException.ThrowIfNull(change);
        switch (change.Action)
        {
            case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                Insert(change.NewStartingIndex, change.NewItems);
                break;
            case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                Remove(change.OldStartingIndex, change.OldItems?.Count ?? 0);
                break;
            case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
                Move(
                    change.OldStartingIndex,
                    change.NewStartingIndex,
                    change.OldItems?.Count ?? 0);
                break;
            case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                Remove(change.OldStartingIndex, change.OldItems?.Count ?? 0);
                Insert(change.NewStartingIndex, change.NewItems);
                break;
            case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                _entries.Clear();
                break;
        }
    }

    public void Clear()
    {
        _entries.Clear();
    }

    Entry NewEntry(object item) =>
        new(item, new ReferenceOccurrenceKey(_nextValue++));

    void Insert(int index, System.Collections.IList? items)
    {
        if (items is null || index < 0 || index > _entries.Count)
            return;
        for (int i = 0; i < items.Count; i++)
            _entries.Insert(index + i, NewEntry(items[i] ?? new object()));
    }

    void Remove(int index, int count)
    {
        if (index < 0 || count <= 0 || index + count > _entries.Count)
            return;
        _entries.RemoveRange(index, count);
    }

    void Move(int oldIndex, int newIndex, int count)
    {
        if (oldIndex < 0 || newIndex < 0 || count <= 0 ||
            oldIndex + count > _entries.Count)
        {
            return;
        }
        var moved = _entries.GetRange(oldIndex, count);
        _entries.RemoveRange(oldIndex, count);
        if (newIndex > _entries.Count)
            return;
        _entries.InsertRange(newIndex, moved);
    }

    readonly struct ItemIdentityKey : System.IEquatable<ItemIdentityKey>
    {
        readonly object _item;

        public ItemIdentityKey(object item) => _item = item;

        public bool Equals(ItemIdentityKey other)
        {
            if (ReferenceEquals(_item, other._item))
                return true;
            var type = _item.GetType();
            return type.IsValueType &&
                type == other._item.GetType() &&
                _item.Equals(other._item);
        }

        public override bool Equals(object? obj) =>
            obj is ItemIdentityKey other && Equals(other);

        public override int GetHashCode() =>
            _item.GetType().IsValueType
                ? System.HashCode.Combine(_item.GetType(), _item)
                : System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(_item);
    }

    sealed record Entry(object Item, ReferenceOccurrenceKey Key);
}

internal sealed record ReferenceOccurrenceKey(long Value);
