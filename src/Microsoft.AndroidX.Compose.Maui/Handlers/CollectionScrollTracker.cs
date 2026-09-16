namespace Microsoft.AndroidX.Compose.Maui.Handlers;

internal sealed class CollectionScrollTracker
{
    readonly bool _horizontal;
    LazyListScrollSnapshot? _previous;
    double _totalOffset;

    public CollectionScrollTracker(bool horizontal) => _horizontal = horizontal;

    public bool TryObserve(
        LazyListScrollSnapshot current,
        float density,
        out Microsoft.Maui.Controls.ItemsViewScrolledEventArgs? args)
    {
        System.ArgumentNullException.ThrowIfNull(current);
        if (density <= 0f)
            throw new System.ArgumentOutOfRangeException(
                nameof(density), density, "Density must be positive.");

        var previous = _previous;
        _previous = current;
        args = null;
        if (previous is null || previous.VisibleItems.Length == 0 ||
            current.VisibleItems.Length == 0)
        {
            return false;
        }

        int? deltaPixels = null;
        foreach (var oldItem in previous.VisibleItems)
        {
            foreach (var newItem in current.VisibleItems)
            {
                if (oldItem.Index != newItem.Index)
                    continue;
                deltaPixels = oldItem.Offset - newItem.Offset;
                break;
            }
            if (deltaPixels.HasValue)
                break;
        }

        if (!deltaPixels.HasValue || deltaPixels.Value == 0)
            return false;

        double delta = deltaPixels.Value / density;
        _totalOffset += delta;
        var indexes = new int[current.VisibleItems.Length];
        for (int i = 0; i < current.VisibleItems.Length; i++)
            indexes[i] = current.VisibleItems[i].Index;
        System.Array.Sort(indexes);

        double viewportCenter =
            (current.ViewportStart + current.ViewportEnd) / 2d;
        int centerIndex = current.VisibleItems[0].Index;
        double closestDistance = double.MaxValue;
        foreach (var item in current.VisibleItems)
        {
            double itemCenter = item.Offset + item.Size / 2d;
            double distance = System.Math.Abs(itemCenter - viewportCenter);
            if (distance >= closestDistance)
                continue;
            closestDistance = distance;
            centerIndex = item.Index;
        }

        args = new Microsoft.Maui.Controls.ItemsViewScrolledEventArgs
        {
            HorizontalDelta = _horizontal ? delta : 0d,
            VerticalDelta = _horizontal ? 0d : delta,
            HorizontalOffset = _horizontal ? _totalOffset : 0d,
            VerticalOffset = _horizontal ? 0d : _totalOffset,
            FirstVisibleItemIndex = indexes[0],
            CenterItemIndex = centerIndex,
            LastVisibleItemIndex = indexes[^1],
        };
        return true;
    }
}
