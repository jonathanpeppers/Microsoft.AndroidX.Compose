namespace Microsoft.AndroidX.Compose.Maui.Handlers;

internal sealed class CollectionViewportObserver
{
    readonly System.Collections.Generic.List<System.WeakReference<System.Action>>
        _listeners = [];
    LazyListScrollSnapshot? _previous;
    int? _previousOffset;

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

    public void ResetTracking()
    {
        _previous = null;
        _previousOffset = null;
    }

    public bool HasSignificantOffsetChange(int current, float density)
    {
        if (density <= 0f)
            throw new System.ArgumentOutOfRangeException(
                nameof(density), density, "Density must be positive.");
        if (_previousOffset is not int previous)
        {
            _previousOffset = current;
            return false;
        }
        if (System.Math.Abs(current - previous) / density <= 10f)
            return false;
        _previousOffset = current;
        return true;
    }

    public bool HasSignificantChange(
        LazyListScrollSnapshot current,
        float density)
    {
        System.ArgumentNullException.ThrowIfNull(current);
        if (density <= 0f)
            throw new System.ArgumentOutOfRangeException(
                nameof(density), density, "Density must be positive.");

        var previous = _previous;
        if (previous is null || previous.VisibleItems.Length == 0 ||
            current.VisibleItems.Length == 0)
        {
            _previous = current;
            return false;
        }

        foreach (var oldItem in previous.VisibleItems)
        {
            foreach (var newItem in current.VisibleItems)
            {
                if (oldItem.Index != newItem.Index)
                    continue;
                if (System.Math.Abs(oldItem.Offset - newItem.Offset) / density <= 10f)
                    return false;
                _previous = current;
                return true;
            }
        }

        if (SameViewport(previous, current))
            return false;
        _previous = current;
        return true;
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
