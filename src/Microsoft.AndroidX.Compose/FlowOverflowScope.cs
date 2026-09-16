namespace AndroidX.Compose;

/// <summary>Managed count access for a flow overflow indicator.</summary>
/// <remarks>
/// Foundation 1.11.3 computes counts during measurement, not composition.
/// Read them in the indicator's drawing or post-layout callback, never while
/// constructing its content. Kotlin caches the first read on each native scope;
/// do not retain a scope across indicator invocations.
/// Counts exclude the overflow indicator itself.
/// </remarks>
public sealed class FlowOverflowScope
{
    readonly Func<int> _total;
    readonly Func<int> _shown;

    internal FlowOverflowScope(Func<int> total, Func<int> shown)
    {
        _total = total;
        _shown = shown;
    }

    /// <summary>Total regular items, including hidden items. Read after layout.</summary>
    public int TotalItemCount => _total();

    /// <summary>Regular items shown by the layout. Reading before measurement throws.</summary>
    public int ShownItemCount => _shown();
}
