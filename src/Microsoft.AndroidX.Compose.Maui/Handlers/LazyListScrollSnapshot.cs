namespace Microsoft.AndroidX.Compose.Maui.Handlers;

internal sealed class LazyListScrollSnapshot
{
    public LazyListScrollSnapshot(
        LazyListVisibleItemSnapshot[] visibleItems)
    {
        System.ArgumentNullException.ThrowIfNull(visibleItems);
        VisibleItems = visibleItems;
    }

    public LazyListVisibleItemSnapshot[] VisibleItems { get; }

    public static LazyListScrollSnapshot Parse(string value)
    {
        System.ArgumentNullException.ThrowIfNull(value);
        if (value.Length == 0)
            return new LazyListScrollSnapshot([]);

        var entries = value.Split(';');
        var items = new LazyListVisibleItemSnapshot[entries.Length];
        for (int i = 0; i < entries.Length; i++)
        {
            var fields = entries[i].Split(',');
            if (fields.Length != 3 ||
                !int.TryParse(
                    fields[0],
                    System.Globalization.NumberStyles.Integer,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out int index) ||
                !int.TryParse(
                    fields[1],
                    System.Globalization.NumberStyles.Integer,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out int offset) ||
                !int.TryParse(
                    fields[2],
                    System.Globalization.NumberStyles.Integer,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out int size))
            {
                throw new System.FormatException(
                    $"Invalid lazy-list viewport snapshot entry '{entries[i]}'.");
            }
            items[i] = new LazyListVisibleItemSnapshot(index, offset, size);
        }
        return new LazyListScrollSnapshot(items);
    }
}
