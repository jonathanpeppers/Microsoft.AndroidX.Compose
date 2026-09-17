namespace AndroidX.Compose;

/// <summary>Managed count access for a flow overflow indicator.</summary>
/// <remarks>
/// Foundation 1.11.3 computes counts during measurement, not composition.
/// Read them in the indicator's drawing or post-layout callback, never while
/// constructing its content. Kotlin caches the first read on each native scope;
/// do not retain a scope across indicator invocations.
/// A new managed callback invocation can still receive a previously cached native
/// scope. These getters are native snapshots, not live item-source counts:
/// content-only changes at an unchanged line limit can retain the old total until
/// the native scope is recreated. The facade preserves this pinned Kotlin behavior.
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

    /// <summary>Native scope's cached total of regular items, including hidden items. Read after layout.</summary>
    public int TotalItemCount => _total();

    /// <summary>Native scope's cached shown-item count. Reading before measurement throws.</summary>
    public int ShownItemCount => _shown();
}
