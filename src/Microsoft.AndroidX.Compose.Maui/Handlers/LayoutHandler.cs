using AndroidX.Compose;
using AndroidX.Compose.Runtime;
using Microsoft.AndroidX.Compose.Maui.Platform;
using Microsoft.Maui.Handlers;
using ILayout = Microsoft.Maui.ILayout;
using MauiHorizontalStackLayout = Microsoft.Maui.Controls.HorizontalStackLayout;
using MauiVerticalStackLayout = Microsoft.Maui.Controls.VerticalStackLayout;

namespace Microsoft.AndroidX.Compose.Maui.Handlers;

/// <summary>
/// Handler for the simple stack layouts
/// (<see cref="Microsoft.Maui.Controls.VerticalStackLayout"/>,
/// <see cref="Microsoft.Maui.Controls.HorizontalStackLayout"/>) that
/// folds into the page's single <see cref="AndroidX.Compose.UI.Platform.ComposeView"/>
/// via <see cref="IComposeHandler"/>. <c>VerticalStackLayout</c> →
/// Compose <see cref="Column"/>, <c>HorizontalStackLayout</c> →
/// Compose <see cref="Row"/>.
/// </summary>
/// <remarks>
/// <para>Registered <em>only</em> for those two concrete types in
/// <see cref="Hosting.AppHostBuilderExtensions.UseAndroidXCompose"/>.
/// <c>Grid</c>, <c>AbsoluteLayout</c>, <c>FlexLayout</c>,
/// <c>StackLayout</c> stay on MAUI's stock
/// <see cref="Microsoft.Maui.Handlers.LayoutHandler"/> — their
/// measure/arrange logic lives inside
/// <c>Microsoft.Maui.Controls.Layout.CrossPlatformLayout</c> (an
/// <see cref="ILayoutManager"/>) which the stock
/// <c>LayoutViewGroup</c> drives. Children of those stock layouts
/// resolve their handlers normally; any Compose-backed leaf
/// (Label/Button/Entry/Image) returns a <c>ComposeView</c> as its
/// platform view and self-hosts when attached (one composition per
/// leaf, same as the pre-refactor behaviour).</para>
///
/// <para>Adding richer Grid / Absolute / Flex support is a follow-up
/// that needs a generic <c>Compose Layout {}</c> measure-policy
/// bridge wrapping MAUI's <see cref="ILayoutManager"/>. See
/// <c>plan.md</c>.</para>
/// </remarks>
public partial class LayoutHandler : ComposeElementHandler<ILayout>
{
    /// <summary>
    /// Property mapper. <see cref="IStackLayout.Spacing"/> +
    /// <see cref="IPadding.Padding"/> are read live during
    /// <see cref="BuildNode(IComposer)"/>; eager mapper writes go
    /// into the spacing slot so a runtime <c>Spacing</c> change
    /// triggers recomposition.
    /// </summary>
    public static IPropertyMapper<ILayout, LayoutHandler> Mapper =
        new PropertyMapper<ILayout, LayoutHandler>(ViewHandler.ViewMapper)
        {
            // Children tree changes: bumping the version slot
            // invalidates the layout's container subtree (Compose
            // smart-skips siblings whose state is unchanged).
            ["Children"]                          = MapChildren,
            [nameof(IStackLayout.Spacing)]        = MapSpacing,
            [nameof(IPadding.Padding)]            = MapPadding,
        };

    /// <summary>Command mapper (inherits view-level commands; no extras).</summary>
    public static CommandMapper<ILayout, LayoutHandler> CommandMapper =
        new(ViewCommandMapper);

    readonly MutableState<int> _childrenVersion = new(0);
    readonly MutableState<float> _spacing = new(0f);
    // Thickness is a MAUI struct; not a Java type, primitive, or
    // Nullable<primitive>, so MutableState<Thickness> throws
    // NotSupportedException at construction. Use a version counter
    // and re-read VirtualView.Padding live in BuildNode (same trick
    // as _childrenVersion).
    readonly MutableState<int> _paddingVersion = new(0);

    /// <summary>Construct a handler with the default mappers.</summary>
    public LayoutHandler() : base(Mapper, CommandMapper) { }

    /// <summary>Construct a handler with custom mappers.</summary>
    public LayoutHandler(IPropertyMapper? mapper, CommandMapper? commandMapper = null)
        : base(mapper ?? Mapper, commandMapper ?? CommandMapper) { }

    /// <inheritdoc/>
    public override ComposableNode BuildNode(IComposer composer)
    {
        var layout = VirtualView
            ?? throw new InvalidOperationException("VirtualView not set on LayoutHandler.");
        var context = MauiContext
            ?? throw new InvalidOperationException("MauiContext not set on LayoutHandler.");

        return layout switch
        {
            // MAUI doesn't expose an IVerticalStackLayout / IHorizontalStackLayout
            // interface (only the concrete control types + the
            // IStackLayout interface with Spacing). We registered for
            // the two concrete VirtualView types in
            // UseAndroidXCompose, so check those.
            MauiVerticalStackLayout   => BuildStack(layout, context, vertical: true),
            MauiHorizontalStackLayout => BuildStack(layout, context, vertical: false),
            _ => throw new InvalidOperationException(
                $"Unsupported layout type '{layout.GetType().Name}'. " +
                "LayoutHandler currently only handles VerticalStackLayout / HorizontalStackLayout " +
                "— Grid/AbsoluteLayout/FlexLayout/StackLayout stay on MAUI's stock LayoutHandler. " +
                "If you're seeing this, the registration in UseAndroidXCompose claimed too much."),
        };
    }

    ComposableNode BuildStack(ILayout layout, IMauiContext context, bool vertical)
    {
        // Read live state on the composition thread so changes
        // observed via the mapper slots trigger recomposition.
        SubscribeToViewProperties();
        var spacing = _spacing.Value;
        _ = _paddingVersion.Value;  // subscribe — padding change bumps this
        _ = _childrenVersion.Value; // subscribe — a tree mutation bumps this
        var padding = layout is IPadding pad ? pad.Padding : Thickness.Zero;

        var arrangement = spacing > 0f ? Arrangement.SpacedBy(new Dp(spacing)) : null;

        ComposableContainer container = vertical
            ? new Column(verticalArrangement: arrangement)
            : new Row(horizontalArrangement: arrangement);

        // Build the prepended modifier in one chain:
        // 1. ApplyViewProperties wraps the OUTER box (Opacity / Scale /
        //    Rotation / Clip / Shadow / IsVisible affect the entire
        //    padded layout including its own background-painting slot).
        // 2. Padding goes innermost so the inner content gets the
        //    inset, while alpha / clip / shadow trace the outer box.
        // PrependModifier replaces, so emit a single chained call.
        var outer = Modifier.Companion.ApplyViewProperties(layout);
        if (padding != Thickness.Zero)
        {
            outer = outer.Padding(
                start:  new Dp((float)padding.Left),
                top:    new Dp((float)padding.Top),
                end:    new Dp((float)padding.Right),
                bottom: new Dp((float)padding.Bottom));
        }
        container.Modifier = outer;

        for (int i = 0; i < layout.Count; i++)
        {
            var child = layout[i];
            container.Add(c => ComposeWalker.Render(child, c, context));
        }

        return container;
    }

    /// <summary>
    /// Bump the children-version slot so any composition reading it
    /// (the layout container) recomposes. Stock MAUI's
    /// <c>LayoutHandlerUpdate</c> command pushes the same signal
    /// through <see cref="ILayoutHandler"/>'s
    /// <c>Add</c>/<c>Insert</c>/<c>Remove</c>/<c>Clear</c>; we
    /// short-circuit at the mapper layer instead.
    /// </summary>
    public static void MapChildren(LayoutHandler handler, ILayout layout)
    {
        handler._childrenVersion.Value++;
    }

    /// <summary>Map <see cref="IStackLayout.Spacing"/> to the spacing slot (dp).</summary>
    public static void MapSpacing(LayoutHandler handler, ILayout layout)
    {
        if (layout is IStackLayout stack)
            handler._spacing.Value = (float)stack.Spacing;
    }

    /// <summary>
    /// Bump the padding-version slot so any composition reading
    /// <see cref="IPadding.Padding"/> on the live <c>VirtualView</c>
    /// recomposes. Wraps silently around <see cref="int.MaxValue"/>;
    /// the only thing Compose checks is value-not-equal-to-previous.
    /// </summary>
    public static void MapPadding(LayoutHandler handler, ILayout layout)
    {
        handler._paddingVersion.Value++;
    }
}
