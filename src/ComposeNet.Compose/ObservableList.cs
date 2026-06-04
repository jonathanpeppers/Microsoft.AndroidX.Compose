using System.Collections;
using System.Collections.Generic;

namespace ComposeNet;

/// <summary>
/// A managed <see cref="IList{T}"/> wrapper that participates in
/// Compose's snapshot system without needing a Kotlin
/// <c>SnapshotStateList</c> binding. Every read (<see cref="Count"/>,
/// the indexer, <see cref="GetEnumerator"/>) touches an internal
/// <see cref="MutableNumberState{Int32}"/> version counter so the
/// composition scope subscribes; every mutation increments that
/// counter, triggering a recomposition.
///
/// <code>
/// var messages = Remember(() => new ObservableList&lt;Message&gt;());
/// // In a Render body:
/// foreach (var m in messages)
///     new Text(m.Content);
/// // Anywhere — triggers recomposition:
/// messages.Add(new Message(...));
/// </code>
///
/// Reads MUST happen during composition for the subscription to take
/// effect. Mutations from a UI button handler (which run outside
/// composition) are fine — the next recomposition will see the new
/// version. Not thread-safe; intended for UI/composition-thread use.
/// </summary>
public sealed class ObservableList<T> : IList<T>, IReadOnlyList<T>
{
    readonly List<T> _items;
    readonly MutableNumberState<int> _tick = new(0);

    public ObservableList() => _items = new List<T>();
    public ObservableList(IEnumerable<T> initial) => _items = new List<T>(initial);

    // Subscribe the current composition scope to mutations.
    void Track() { _ = _tick.Value; }
    // Notify all subscribed scopes that the list has changed.
    void Bump() => _tick.Value++;

    public int Count
    {
        get { Track(); return _items.Count; }
    }

    public bool IsReadOnly => false;

    public T this[int index]
    {
        get { Track(); return _items[index]; }
        set { _items[index] = value; Bump(); }
    }

    public void Add(T item) { _items.Add(item); Bump(); }

    public void Insert(int index, T item) { _items.Insert(index, item); Bump(); }

    public bool Remove(T item)
    {
        if (_items.Remove(item)) { Bump(); return true; }
        return false;
    }

    public void RemoveAt(int index) { _items.RemoveAt(index); Bump(); }

    public void Clear()
    {
        if (_items.Count == 0) return;
        _items.Clear();
        Bump();
    }

    public bool Contains(T item) { Track(); return _items.Contains(item); }
    public int IndexOf(T item)  { Track(); return _items.IndexOf(item); }
    public void CopyTo(T[] array, int arrayIndex) { Track(); _items.CopyTo(array, arrayIndex); }

    public IEnumerator<T> GetEnumerator() { Track(); return _items.GetEnumerator(); }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
