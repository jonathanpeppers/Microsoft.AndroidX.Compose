namespace Microsoft.AndroidX.Compose.Maui.Handlers;

internal sealed class CollectionViewportObserver
{
    readonly System.Collections.Generic.List<System.WeakReference<System.Action>>
        _listeners = [];
    LazyListScrollSnapshot? _previous;

    public void Register(System.Action listener)
    {
        System.ArgumentNullException.ThrowIfNull(listener);
        foreach (var existing in _listeners)
        {
            if (existing.TryGetTarget(out var target) && target == listener)
                return;
        }
        _listeners.Add(new System.WeakReference<System.Action>(listener));
    }

    public void Unregister(System.Action listener)
    {
        System.ArgumentNullException.ThrowIfNull(listener);
        _listeners.RemoveAll(existing =>
            !existing.TryGetTarget(out var target) || target == listener);
    }

    public void ResetTracking() => _previous = null;

    public bool HasSignificantChange(
        LazyListScrollSnapshot current,
        float density)
    {
        System.ArgumentNullException.ThrowIfNull(current);
        if (density <= 0f)
            throw new System.ArgumentOutOfRangeException(
                nameof(density), density, "Density must be positive.");

        var previous = _previous;
        _previous = current;
        if (previous is null || previous.VisibleItems.Length == 0 ||
            current.VisibleItems.Length == 0)
        {
            return false;
        }

        foreach (var oldItem in previous.VisibleItems)
        {
            foreach (var newItem in current.VisibleItems)
            {
                if (oldItem.Index != newItem.Index)
                    continue;
                return System.Math.Abs(oldItem.Offset - newItem.Offset) / density > 10f;
            }
        }

        return !SameViewport(previous, current);
    }

    public void NotifySignificantChange()
    {
        for (int i = _listeners.Count - 1; i >= 0; i--)
        {
            if (_listeners[i].TryGetTarget(out var listener))
                listener();
            else
                _listeners.RemoveAt(i);
        }
    }

    static bool SameViewport(
        LazyListScrollSnapshot left,
        LazyListScrollSnapshot right)
    {
        if (left.VisibleItems.Length != right.VisibleItems.Length)
            return false;
        for (int i = 0; i < left.VisibleItems.Length; i++)
        {
            if (left.VisibleItems[i].Index != right.VisibleItems[i].Index ||
                left.VisibleItems[i].Offset != right.VisibleItems[i].Offset)
            {
                return false;
            }
        }
        return true;
    }
}
