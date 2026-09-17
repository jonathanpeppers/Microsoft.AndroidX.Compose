using AndroidX.Compose.Runtime;

namespace AndroidX.Compose;

/// <summary>Overflow configuration for <see cref="FlowRow"/>.</summary>
/// <remarks>
/// Retains Foundation 1.11.3's deprecated overflow API. Indicators are composed
/// for measurement even when not placed. Emit one layout root per indicator;
/// change the owning flow's maxLines in event callbacks to expand or collapse.
/// See <see cref="FlowOverflowScope"/> for post-measurement count access.
/// </remarks>
public sealed class FlowRowOverflow
{
    readonly FlowOverflowContent? _expand;
    readonly FlowOverflowContent? _collapse;
    readonly int? _minRows;
    readonly Dp? _minHeight;

    FlowRowOverflow(FlowOverflowContent? expand = null, FlowOverflowContent? collapse = null,
        int? minRows = null, Dp? minHeight = null)
    {
        _expand = expand;
        _collapse = collapse;
        _minRows = minRows;
        _minHeight = minHeight;
    }

    /// <summary>Clips items that do not fit. This is also Kotlin's default.</summary>
    public static FlowRowOverflow Clip { get; } = new();

    /// <summary>Shows a tree indicator when regular items do not fit.</summary>
    public static FlowRowOverflow ExpandIndicator(ComposableNode content) =>
        new(FlowOverflowContent.FromNode(content));

    /// <summary>Builds a tree indicator with post-layout count access.</summary>
    public static FlowRowOverflow ExpandIndicator(Func<FlowOverflowScope, ComposableNode> content) =>
        new(FlowOverflowContent.FromFactory(content));

    /// <summary>Renders composable indicator content when regular items do not fit.</summary>
    public static FlowRowOverflow ExpandIndicator([ComposableContent] Action<FlowOverflowScope> content) =>
        new(FlowOverflowContent.FromAction(content));

    /// <summary>Uses tree expand/collapse indicators. Null thresholds use Kotlin's one row and zero dp.</summary>
    public static FlowRowOverflow ExpandOrCollapseIndicator(ComposableNode expandIndicator,
        ComposableNode collapseIndicator, int? minRowsToShowCollapse = null, Dp? minHeightToShowCollapse = null) =>
        new(FlowOverflowContent.FromNode(expandIndicator), FlowOverflowContent.FromNode(collapseIndicator),
            minRowsToShowCollapse, minHeightToShowCollapse);

    /// <summary>Builds tree expand/collapse indicators with post-layout count access.</summary>
    public static FlowRowOverflow ExpandOrCollapseIndicator(Func<FlowOverflowScope, ComposableNode> expandIndicator,
        Func<FlowOverflowScope, ComposableNode> collapseIndicator,
        int? minRowsToShowCollapse = null, Dp? minHeightToShowCollapse = null) =>
        new(FlowOverflowContent.FromFactory(expandIndicator), FlowOverflowContent.FromFactory(collapseIndicator),
            minRowsToShowCollapse, minHeightToShowCollapse);

    /// <summary>Renders composable expand/collapse indicators with post-layout count access.</summary>
    public static FlowRowOverflow ExpandOrCollapseIndicator(
        [ComposableContent] Action<FlowOverflowScope> expandIndicator,
        [ComposableContent] Action<FlowOverflowScope> collapseIndicator,
        int? minRowsToShowCollapse = null, Dp? minHeightToShowCollapse = null) =>
        new(FlowOverflowContent.FromAction(expandIndicator), FlowOverflowContent.FromAction(collapseIndicator),
            minRowsToShowCollapse, minHeightToShowCollapse);

#pragma warning disable CS0618 // Pinned Foundation 1.11.3 overflow compatibility.
    internal Foundation.Layout.FlowRowOverflow Build(IComposer composer)
    {
        composer.StartReplaceableGroup(SourceLocationKey.Compute(1, "FlowRowOverflow"));
        try
        {
            if (_expand is null)
                return FlowOverflowNative.Row.Clip;
            var expand = _expand.Wrap(composer, horizontal: true);
            if (_collapse is null)
                return composer.Remember(() => FlowOverflowNative.Row.ExpandIndicator(expand), expand);
            var collapse = _collapse.Wrap(composer, horizontal: true);
            var defaults = FlowRowIndicatorDefault.All;
            if (_minRows is not null) defaults &= ~FlowRowIndicatorDefault.MinRowsToShowCollapse;
            if (_minHeight is not null) defaults &= ~FlowRowIndicatorDefault.MinHeightToShowCollapse;
            return FlowOverflowNative.Row.ExpandOrCollapseIndicator__jt2gSs(
                expand, collapse, _minRows ?? 0, _minHeight?.Value ?? 0, composer, 0, (int)defaults);
        }
        finally { composer.EndReplaceableGroup(); }
    }
#pragma warning restore CS0618
}
