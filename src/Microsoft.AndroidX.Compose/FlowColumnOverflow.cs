using AndroidX.Compose.Runtime;

namespace AndroidX.Compose;

/// <summary>Overflow configuration for <see cref="FlowColumn"/>.</summary>
/// <remarks>
/// Retains Foundation 1.11.3's deprecated overflow API. Indicators are composed
/// for measurement even when not placed. Emit one layout root per indicator;
/// change the owning flow's maxLines in event callbacks to expand or collapse.
/// See <see cref="FlowOverflowScope"/> for post-measurement count access.
/// </remarks>
public sealed class FlowColumnOverflow
{
    readonly FlowOverflowContent? _expand;
    readonly FlowOverflowContent? _collapse;
    readonly int? _minColumns;
    readonly Dp? _minWidth;

    FlowColumnOverflow(FlowOverflowContent? expand = null, FlowOverflowContent? collapse = null,
        int? minColumns = null, Dp? minWidth = null)
    {
        _expand = expand;
        _collapse = collapse;
        _minColumns = minColumns;
        _minWidth = minWidth;
    }

    /// <summary>Clips items that do not fit. This is also Kotlin's default.</summary>
    public static FlowColumnOverflow Clip { get; } = new();

    /// <summary>Shows a tree indicator when regular items do not fit.</summary>
    public static FlowColumnOverflow ExpandIndicator(ComposableNode content) =>
        new(FlowOverflowContent.FromNode(content));

    /// <summary>Builds a tree indicator with post-layout count access.</summary>
    public static FlowColumnOverflow ExpandIndicator(Func<FlowOverflowScope, ComposableNode> content) =>
        new(FlowOverflowContent.FromFactory(content));

    /// <summary>Renders composable indicator content when regular items do not fit.</summary>
    public static FlowColumnOverflow ExpandIndicator([ComposableContent] Action<FlowOverflowScope> content) =>
        new(FlowOverflowContent.FromAction(content));

    /// <summary>Uses tree expand/collapse indicators. Null thresholds use Kotlin's one column and zero dp.</summary>
    public static FlowColumnOverflow ExpandOrCollapseIndicator(ComposableNode expandIndicator,
        ComposableNode collapseIndicator, int? minColumnsToShowCollapse = null, Dp? minWidthToShowCollapse = null) =>
        new(FlowOverflowContent.FromNode(expandIndicator), FlowOverflowContent.FromNode(collapseIndicator),
            minColumnsToShowCollapse, minWidthToShowCollapse);

    /// <summary>Builds tree expand/collapse indicators with post-layout count access.</summary>
    public static FlowColumnOverflow ExpandOrCollapseIndicator(Func<FlowOverflowScope, ComposableNode> expandIndicator,
        Func<FlowOverflowScope, ComposableNode> collapseIndicator,
        int? minColumnsToShowCollapse = null, Dp? minWidthToShowCollapse = null) =>
        new(FlowOverflowContent.FromFactory(expandIndicator), FlowOverflowContent.FromFactory(collapseIndicator),
            minColumnsToShowCollapse, minWidthToShowCollapse);

    /// <summary>Renders composable expand/collapse indicators with post-layout count access.</summary>
    public static FlowColumnOverflow ExpandOrCollapseIndicator(
        [ComposableContent] Action<FlowOverflowScope> expandIndicator,
        [ComposableContent] Action<FlowOverflowScope> collapseIndicator,
        int? minColumnsToShowCollapse = null, Dp? minWidthToShowCollapse = null) =>
        new(FlowOverflowContent.FromAction(expandIndicator), FlowOverflowContent.FromAction(collapseIndicator),
            minColumnsToShowCollapse, minWidthToShowCollapse);

#pragma warning disable CS0618 // Pinned Foundation 1.11.3 overflow compatibility.
    internal Foundation.Layout.FlowColumnOverflow Build(IComposer composer)
    {
        composer.StartReplaceableGroup(SourceLocationKey.Compute(1, "FlowColumnOverflow"));
        try
        {
            if (_expand is null)
                return FlowOverflowNative.Column.Clip;
            var expand = _expand.Wrap(composer, horizontal: false);
            if (_collapse is null)
                return composer.Remember(() => FlowOverflowNative.Column.ExpandIndicator(expand), expand);
            var collapse = _collapse.Wrap(composer, horizontal: false);
            var defaults = FlowColumnIndicatorDefault.All;
            if (_minColumns is not null) defaults &= ~FlowColumnIndicatorDefault.MinColumnsToShowCollapse;
            if (_minWidth is not null) defaults &= ~FlowColumnIndicatorDefault.MinWidthToShowCollapse;
            return FlowOverflowNative.Column.ExpandOrCollapseIndicator__jt2gSs(
                expand, collapse, _minColumns ?? 0, _minWidth?.Value ?? 0, composer, 0, (int)defaults);
        }
        finally { composer.EndReplaceableGroup(); }
    }
#pragma warning restore CS0618
}
