using System.Collections;
using System.Collections.Specialized;
using AndroidX.Compose;
using AndroidX.Compose.Runtime;
using AndroidX.Compose.UI.Platform;
using Microsoft.AndroidX.Compose.Maui.Platform;
using Microsoft.Maui.Handlers;
using ComposeText           = AndroidX.Compose.Text;
using MauiCollectionView    = Microsoft.Maui.Controls.CollectionView;
using MauiDataTemplate      = Microsoft.Maui.Controls.DataTemplate;
using MauiDataTemplateSel   = Microsoft.Maui.Controls.DataTemplateSelector;
using MauiGridItemsLayout   = Microsoft.Maui.Controls.GridItemsLayout;
using MauiItemsLayout       = Microsoft.Maui.Controls.IItemsLayout;
using MauiItemsLayoutOrient = Microsoft.Maui.Controls.ItemsLayoutOrientation;
using MauiLinearItemsLayout = Microsoft.Maui.Controls.LinearItemsLayout;

namespace Microsoft.AndroidX.Compose.Maui.Handlers;

/// <summary>
/// MAUI <see cref="MauiCollectionView"/> handler that folds the list
/// directly into the enclosing page composition as a Compose
/// <see cref="LazyColumn{T}"/>, <see cref="LazyRow{T}"/>, or
/// <see cref="LazyVerticalGrid{T}"/> chosen by
/// <see cref="Microsoft.Maui.Controls.StructuredItemsView.ItemsLayout"/>.
/// Replaces the stock <c>RecyclerView</c> island when the consumer
/// calls <see cref="Hosting.AppHostBuilderExtensions.UseAndroidXCompose"/>.
/// </summary>
/// <remarks>
/// <para>The stock <c>CollectionViewHandler</c> wraps each cell in its
/// own <c>ComposeView</c> when the cell contains Compose-folded leaves.
/// That works, but pays for one Recomposer per visible row and re-installs
/// the <c>MaterialTheme</c> per cell. Folding into the page composition
/// means one Recomposer, one snapshot graph, and one
/// <c>MaterialTheme</c> scope for the whole list.</para>
///
/// <para><b>Layout dispatch.</b>
/// <see cref="Microsoft.Maui.Controls.StructuredItemsView.ItemsLayout"/>
/// is read live inside <see cref="BuildNode(IComposer)"/> (it's a
/// <see cref="BindableObject"/> the consumer can swap at runtime):</para>
/// <list type="bullet">
///   <item><description><see cref="MauiLinearItemsLayout"/>
///     <see cref="MauiItemsLayoutOrient.Vertical"/> → <see cref="LazyColumn{T}"/>.</description></item>
///   <item><description><see cref="MauiLinearItemsLayout"/>
///     <see cref="MauiItemsLayoutOrient.Horizontal"/> → <see cref="LazyRow{T}"/>.</description></item>
///   <item><description><see cref="MauiGridItemsLayout"/>
///     <see cref="MauiItemsLayoutOrient.Vertical"/> → <see cref="LazyVerticalGrid{T}"/>
///     with <see cref="GridCells.Fixed(int)"/>.</description></item>
/// </list>
///
/// <para><b>Horizontal grid not yet wired.</b> Compose's
/// <c>LazyHorizontalGrid</c> is bound but its facade isn't exercised here
/// because <see cref="MauiGridItemsLayout"/> with horizontal orientation
/// is uncommon and surfaces additional spacing nuances; tracked as a
/// follow-up. Until then a horizontal grid layout falls back to a
/// <see cref="LazyRow{T}"/> with single-row items.</para>
///
/// <para><b>Item template.</b>
/// <see cref="Microsoft.Maui.Controls.ItemsView.ItemTemplate"/> /
/// <see cref="MauiDataTemplateSel"/> is invoked per item — each item gets
/// a fresh <see cref="BindableObject"/> with its
/// <see cref="BindableObject.BindingContext"/> assigned to the item, then
/// walked through <see cref="ComposeWalker.Render(IView, IComposer, IMauiContext)"/>.
/// Per-item handler allocation cost is accepted for this slice;
/// memoization is a follow-up.</para>
///
/// <para><b>Reactive sources.</b> Any
/// <see cref="MauiCollectionView.ItemsSource"/> implementing
/// <see cref="INotifyCollectionChanged"/> is subscribed to; any
/// mutation bumps the items version slot, which re-runs
/// <see cref="BuildNode(IComposer)"/> and re-snapshots the source into
/// an <see cref="IReadOnlyList{T}"/> the lazy facades index into.</para>
///
/// <para><b>Empty view.</b> When the source is null or empty and
/// <see cref="MauiCollectionView.EmptyView"/> is set, the handler renders
/// the empty-view content instead of the list. A <see cref="string"/> is
/// surfaced as a centered <see cref="AndroidX.Compose.Text"/>; an
/// <see cref="IView"/> is walked through <see cref="ComposeWalker"/>;
/// <see cref="MauiCollectionView.EmptyViewTemplate"/> is honored when
/// set (the template's content is materialised once and re-used).</para>
///
/// <para><b>Out of scope for this slice (follow-up):</b>
/// selected-row highlight styling (the handler already routes Single +
/// Multiple selection writes into MAUI via
/// <see cref="Microsoft.Maui.Controls.SelectableItemsView.SelectedItem"/> /
/// <see cref="Microsoft.Maui.Controls.SelectableItemsView.SelectedItems"/>
/// so <see cref="Microsoft.Maui.Controls.SelectableItemsView.SelectionChanged"/>
/// fires — but the selected row is not yet visually emphasised);
/// <see cref="Microsoft.Maui.Controls.ItemsView.ScrollTo(int, int, Microsoft.Maui.Controls.ScrollToPosition, bool)"/>
/// /
/// <see cref="Microsoft.Maui.Controls.ItemsView.Scrolled"/> event;
/// <see cref="Microsoft.Maui.Controls.ItemsView.ItemsUpdatingScrollMode"/>
/// stability;
/// <see cref="Microsoft.Maui.Controls.ItemsView.RemainingItemsThreshold"/>
/// endless-scroll;
/// <see cref="Microsoft.Maui.Controls.StructuredItemsView.Header"/> /
/// <see cref="Microsoft.Maui.Controls.StructuredItemsView.Footer"/> /
/// <see cref="Microsoft.Maui.Controls.StructuredItemsView.ItemSizingStrategy"/>;
/// grouping;
/// item-handler caching.</para>
/// </remarks>
public partial class CollectionViewHandler : ComposeElementHandler<MauiCollectionView>
{
    /// <summary>
    /// Property mapper that forwards MAUI <see cref="MauiCollectionView"/>
    /// changes to the Compose-backed <see cref="ComposeView"/>.
    /// </summary>
    public static IPropertyMapper<MauiCollectionView, CollectionViewHandler> Mapper =
        new PropertyMapper<MauiCollectionView, CollectionViewHandler>(ViewHandler.ViewMapper)
        {
            [nameof(MauiCollectionView.ItemsSource)]                            = MapItemsSource,
            [nameof(MauiCollectionView.ItemTemplate)]                           = MapTemplateChanged,
            [nameof(MauiCollectionView.ItemsLayout)]                            = MapLayoutChanged,
            [nameof(MauiCollectionView.EmptyView)]                              = MapEmptyChanged,
            [nameof(MauiCollectionView.EmptyViewTemplate)]                      = MapEmptyChanged,
            [nameof(MauiCollectionView.SelectionMode)]                          = MapSelectionModeChanged,
            [nameof(Microsoft.Maui.Controls.VisualElement.WidthRequest)]        = MapSizeRequest,
            [nameof(Microsoft.Maui.Controls.VisualElement.HeightRequest)]       = MapSizeRequest,
        };

    /// <summary>Command mapper (inherits view-level commands; no extras).</summary>
    public static CommandMapper<MauiCollectionView, CollectionViewHandler> CommandMapper =
        new(ViewCommandMapper);

    // One version slot for "anything that should re-snapshot the items
    // source and re-walk the templates". Bumped by every mapper above
    // plus by INotifyCollectionChanged events on the live source. Read
    // off the top of BuildNode so a single Compose dependency edge fans
    // out to the whole list subtree.
    readonly MutableState<int> _itemsVersion = new(0);

    // Tracks the currently-subscribed INotifyCollectionChanged source so
    // we can unsubscribe before swapping to a new source or disposing
    // the handler. Null when the source doesn't implement the
    // notification interface (we still snapshot it on every version
    // bump — the swap itself triggers a bump).
    INotifyCollectionChanged? _observedSource;

    /// <summary>Construct a handler with the default mappers.</summary>
    public CollectionViewHandler() : base(Mapper, CommandMapper) { }

    /// <summary>Construct a handler with custom mappers.</summary>
    public CollectionViewHandler(IPropertyMapper? mapper, CommandMapper? commandMapper = null)
        : base(mapper ?? Mapper, commandMapper ?? CommandMapper) { }

    /// <inheritdoc/>
    protected override void DisconnectHandler(ComposeView platformView)
    {
        UnsubscribeFromSource();
        base.DisconnectHandler(platformView);
    }

    /// <inheritdoc/>
    public override ComposableNode BuildNode(IComposer composer)
    {
        // Re-run when anything list-shaped or template-shaped changes.
        _ = _itemsVersion.Value;
        SubscribeToViewProperties();

        var view = VirtualView
            ?? throw new InvalidOperationException("VirtualView not set on CollectionViewHandler.");
        var context = MauiContext
            ?? throw new InvalidOperationException("MauiContext not set on CollectionViewHandler.");

        var items    = Snapshot(view.ItemsSource);
        var template = view.ItemTemplate;
        var clickable = view.SelectionMode != Microsoft.Maui.Controls.SelectionMode.None;

        // Empty-view fallback. Stock MAUI surfaces EmptyView whenever
        // ItemsSource is null or empty AND EmptyView is set; we match.
        // ItemTemplate being null is NOT an empty-state trigger — stock
        // MAUI falls through to ToString-per-item rendering in that case.
        if (items.Count == 0 && view.EmptyView is not null)
        {
            return BuildEmptyView(view, context);
        }

        // No template + non-empty source: stock MAUI renders the
        // ToString() of each item in a TextCell-equivalent. Match that
        // so the handler degrades gracefully instead of throwing.
        Func<object, ComposableNode> rawItemContent = template is null
            ? item => new ComposeText(item?.ToString() ?? string.Empty)
            : item => BuildFromTemplate(template, item, view, context);

        // SelectionMode != None: wrap each item in a Clickable Box so
        // tapping a row fires MAUI's SelectionChanged + Command. Single
        // assigns SelectedItem; Multiple toggles SelectedItems
        // membership. We intentionally do NOT highlight the selected
        // row yet — selection styling is a follow-up slice. The
        // wrapper is necessary even when the template's root is a
        // touchable Layout because MAUI's stock click delivery routes
        // through CollectionView's adapter, which we're replacing.
        Func<object, ComposableNode> itemContent = clickable
            ? item => WrapClickable(rawItemContent(item), () => OnItemTapped(view, item))
            : rawItemContent;

        // Read the layout live — it's a BindableObject the consumer can
        // swap at runtime, and the mapper bumped _itemsVersion if so.
        var layout = view.ItemsLayout;
        return BuildList(view, layout, items, itemContent, context);
    }

    ComposableNode BuildList(
        MauiCollectionView view,
        MauiItemsLayout? layout,
        IReadOnlyList<object> items,
        Func<object, ComposableNode> itemContent,
        IMauiContext context)
    {
        // ApplyViewProperties wraps the OUTER frame so opacity, scale,
        // rotation, clip, shadow, IsVisible affect the entire list.
        // Gestures + semantics chain in the canonical order so a list
        // tagged with SemanticProperties.Description aggregates under
        // mergeDescendants on the same node hit by tap detectors.
        var outer = Modifier.Companion
            .ApplyViewProperties(view)
            .ApplyGestures(view, context)
            .ApplySemantics(view);
        outer = ApplyListSize(outer, view);

        return layout switch
        {
            MauiGridItemsLayout grid when grid.Orientation == MauiItemsLayoutOrient.Vertical =>
                BuildVerticalGrid(items, itemContent, grid, outer, context),

            // Horizontal grid: no LazyHorizontalGrid facade dispatch
            // yet — fall back to LazyRow per the <remarks> note.
            MauiGridItemsLayout grid =>
                BuildLazyRow(items, itemContent, ItemSpacingOf(grid.HorizontalItemSpacing), outer),

            MauiLinearItemsLayout lin when lin.Orientation == MauiItemsLayoutOrient.Horizontal =>
                BuildLazyRow(items, itemContent, ItemSpacingOf(lin.ItemSpacing), outer),

            // Vertical linear is the default for CollectionView when no
            // ItemsLayout is set (LinearItemsLayout.Vertical singleton).
            MauiLinearItemsLayout lin =>
                BuildLazyColumn(items, itemContent, ItemSpacingOf(lin.ItemSpacing), outer),

            // Unknown layout: degrade to a vertical list with no spacing.
            _ => BuildLazyColumn(items, itemContent, spacing: null, outer),
        };
    }

    static ComposableNode BuildLazyColumn(
        IReadOnlyList<object> items,
        Func<object, ComposableNode> itemContent,
        Arrangement? spacing,
        Modifier outer)
    {
        return new LazyColumn<object>(items, itemContent)
        {
            Modifier            = outer,
            VerticalArrangement = spacing,
        };
    }

    static ComposableNode BuildLazyRow(
        IReadOnlyList<object> items,
        Func<object, ComposableNode> itemContent,
        Arrangement? spacing,
        Modifier outer)
    {
        return new LazyRow<object>(items, itemContent)
        {
            Modifier              = outer,
            HorizontalArrangement = spacing,
        };
    }

    static ComposableNode BuildVerticalGrid(
        IReadOnlyList<object> items,
        Func<object, ComposableNode> itemContent,
        MauiGridItemsLayout grid,
        Modifier outer,
        IMauiContext context)
    {
        _ = context; // reserved for future per-cell handler caching
        int span = grid.Span > 0 ? grid.Span : 1;
        return new LazyVerticalGrid<object>(GridCells.Fixed(span), items, itemContent)
        {
            Modifier              = outer,
            VerticalArrangement   = ItemSpacingOf(grid.VerticalItemSpacing),
            HorizontalArrangement = ItemSpacingOf(grid.HorizontalItemSpacing),
        };
    }

    static Arrangement? ItemSpacingOf(double spacingDp) =>
        spacingDp > 0 ? Arrangement.SpacedBy(new Dp((float)spacingDp)) : null;

    ComposableNode BuildEmptyView(MauiCollectionView view, IMauiContext context)
    {
        // Column with `verticalArrangement: Arrangement.Center` +
        // `horizontalAlignment: CenterHorizontally` centers the empty
        // payload inside the list's slot. Box-with-contentAlignment
        // would also work but Box's facade is parameterless — Column
        // takes both alignments straight on the ctor.
        var outer = Modifier.Companion
            .ApplyViewProperties(view)
            .ApplyGestures(view, context)
            .ApplySemantics(view);
        outer = ApplyListSize(outer, view);

        // Template wins over raw EmptyView when both are set, matching
        // stock MAUI's resolution order (EmptyViewTemplate is preferred
        // because it carries data binding for the EmptyView object).
        if (view.EmptyViewTemplate is MauiDataTemplate emptyTemplate)
        {
            var content = MaterialiseTemplate(emptyTemplate, view.EmptyView ?? view);
            if (content is IView contentView)
            {
                return CenteredColumn(outer, new DeferredViewNode(contentView, context));
            }
        }

        return view.EmptyView switch
        {
            IView emptyView => CenteredColumn(outer, new DeferredViewNode(emptyView, context)),
            string text     => CenteredColumn(outer, new ComposeText(text)),
            // Unknown object: surface its ToString so the empty state
            // is still visible rather than rendering a blank list area.
            { } other => CenteredColumn(outer, new ComposeText(other.ToString() ?? string.Empty)),
            null      => CenteredColumn(outer, child: null),
        };
    }

    static ComposableNode CenteredColumn(Modifier outer, ComposableNode? child)
    {
        var column = new Column(
            verticalArrangement: Arrangement.Center,
            horizontalAlignment: Alignment.Horizontal.CenterHorizontally)
        {
            Modifier = outer,
        };
        if (child is not null)
            column.Add(child);
        return column;
    }

    static ComposableNode BuildFromTemplate(MauiDataTemplate template, object item, MauiCollectionView view, IMauiContext context)
    {
        // Pass `view` (the source CollectionView) as the container so
        // DataTemplateSelector subclasses that branch on the host
        // (e.g. "different template under a CarouselView vs a list")
        // see the same BindableObject stock MAUI's adapter passes.
        var resolved = template is MauiDataTemplateSel selector
            ? selector.SelectTemplate(item, container: view)
            : template;

        var content = MaterialiseTemplate(resolved, item);
        if (content is not IView contentView)
        {
            // Template materialised to a non-View (e.g. a Cell only). We
            // fall back to ToString so the list keeps rendering.
            return new ComposeText(item?.ToString() ?? string.Empty);
        }

        // ComposeWalker.Render needs a live IComposer, but the lazy
        // facade defers item rendering until the Lazy* scope's measure
        // pass actually pulls the index. We materialise the template up
        // front so its BindingContext is set on the UI thread before any
        // composer reads, then wrap the resolved IView in a deferred
        // node whose own Render(IComposer) runs ComposeWalker.Render at
        // the correct moment inside Instantiate4's composition.
        return new DeferredViewNode(contentView, context);
    }

    /// <summary>
    /// Wraps a MAUI <see cref="IView"/> so that
    /// <see cref="ComposeWalker.Render(IView, IComposer, IMauiContext)"/>
    /// runs lazily, with the live composer supplied by the lazy facade's
    /// <c>Instantiate4</c> path. Materialised eagerly so the
    /// template's <see cref="BindableObject.BindingContext"/> is bound
    /// on the UI thread before any composition reads.
    /// </summary>
    sealed class DeferredViewNode : ComposableNode
    {
        readonly IView _view;
        readonly IMauiContext _context;

        public DeferredViewNode(IView view, IMauiContext context)
        {
            _view    = view;
            _context = context;
        }

        public override void Render(IComposer composer) =>
            ComposeWalker.Render(_view, composer, _context).Render(composer);
    }

    static object? MaterialiseTemplate(MauiDataTemplate template, object item)
    {
        var content = template.CreateContent();
        if (content is BindableObject bindable)
        {
            bindable.BindingContext = item;
        }
        return content;
    }

    static IReadOnlyList<object> Snapshot(IEnumerable? source)
    {
        if (source is null)
            return [];

        if (source is IReadOnlyList<object> roList)
            return roList;

        if (source is IList list)
        {
            var arr = new object[list.Count];
            for (int i = 0; i < list.Count; i++)
                arr[i] = list[i] ?? new object();
            return arr;
        }

        var buffer = new List<object>();
        foreach (var item in source)
            buffer.Add(item ?? new object());
        return buffer;
    }

    void SubscribeToSource(MauiCollectionView view)
    {
        UnsubscribeFromSource();
        if (view.ItemsSource is INotifyCollectionChanged ncc)
        {
            _observedSource = ncc;
            ncc.CollectionChanged += OnSourceCollectionChanged;
        }
    }

    void UnsubscribeFromSource()
    {
        if (_observedSource is not null)
        {
            _observedSource.CollectionChanged -= OnSourceCollectionChanged;
            _observedSource = null;
        }
    }

    void OnSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Add / Remove / Move / Reset / Replace all collapse to "re-snapshot
        // and rebuild" for this slice. ItemsUpdatingScrollMode is a
        // follow-up that distinguishes between these.
        _itemsVersion.Value++;
    }

    /// <summary>
    /// Swap the observed <see cref="INotifyCollectionChanged"/> subscription
    /// to the new <see cref="MauiCollectionView.ItemsSource"/> and bump
    /// the items version slot.
    /// </summary>
    public static void MapItemsSource(CollectionViewHandler handler, MauiCollectionView view)
    {
        handler.SubscribeToSource(view);
        handler._itemsVersion.Value++;
    }

    /// <summary>Bump the items version slot when the item template changes.</summary>
    public static void MapTemplateChanged(CollectionViewHandler handler, MauiCollectionView _) =>
        handler._itemsVersion.Value++;

    /// <summary>Bump the items version slot when the layout object changes.</summary>
    public static void MapLayoutChanged(CollectionViewHandler handler, MauiCollectionView _) =>
        handler._itemsVersion.Value++;

    /// <summary>Bump the items version slot when the empty-view payload changes.</summary>
    public static void MapEmptyChanged(CollectionViewHandler handler, MauiCollectionView _) =>
        handler._itemsVersion.Value++;

    /// <summary>Bump the items version slot when selection mode flips.</summary>
    public static void MapSelectionModeChanged(CollectionViewHandler handler, MauiCollectionView _) =>
        handler._itemsVersion.Value++;

    /// <summary>
    /// Bump the items version slot when
    /// <see cref="Microsoft.Maui.Controls.VisualElement.WidthRequest"/> or
    /// <see cref="Microsoft.Maui.Controls.VisualElement.HeightRequest"/>
    /// changes. Honoring these is mandatory because the lazy facades
    /// inherit their parent's max constraints — nesting an unsized
    /// list inside a vertical <see cref="Microsoft.Maui.Controls.ScrollView"/>
    /// throws "Vertically scrollable component was measured with an
    /// infinity maximum height constraints" from
    /// <c>androidx.compose.foundation.CheckScrollableContainerConstraintsKt</c>.
    /// </summary>
    public static void MapSizeRequest(CollectionViewHandler handler, MauiCollectionView _) =>
        handler._itemsVersion.Value++;

    /// <summary>
    /// Apply <see cref="Microsoft.Maui.Controls.VisualElement.WidthRequest"/> /
    /// <see cref="Microsoft.Maui.Controls.VisualElement.HeightRequest"/> to
    /// the outer list modifier. Mirrors <c>BoxViewHandler</c>'s size
    /// switch. The "neither set" path falls back to <c>FillMaxSize</c>
    /// for the common case where the list is the page's primary
    /// content (e.g. a navigation list directly under <c>ContentPage</c>);
    /// when the consumer hosts the list inside a vertical
    /// <see cref="Microsoft.Maui.Controls.ScrollView"/> they MUST set
    /// <see cref="Microsoft.Maui.Controls.VisualElement.HeightRequest"/>
    /// (or wrap the list in a bounded
    /// <see cref="Microsoft.Maui.Controls.Layout"/>) — the same constraint
    /// Compose enforces on raw <c>LazyColumn</c>.
    /// </summary>
    static Modifier ApplyListSize(Modifier modifier, MauiCollectionView view)
    {
        var widthReq  = view.WidthRequest;
        var heightReq = view.HeightRequest;
        return (widthReq, heightReq) switch
        {
            ( >= 0d, >= 0d ) => modifier.Size(new Dp((float)widthReq), new Dp((float)heightReq)),
            ( >= 0d, _    ) => modifier.Width(new Dp((float)widthReq)).FillMaxHeight(),
            ( _,    >= 0d ) => modifier.FillMaxWidth().Height(new Dp((float)heightReq)),
            _                => modifier.FillMaxSize(),
        };
    }

    static ComposableNode WrapClickable(ComposableNode child, Action onClick)
    {
        // Box facade is parameterless; the wrapper carries the
        // Clickable modifier (which Compose merges into pointer-input
        // and accessibility semantics) plus FillMaxWidth so the whole
        // row receives taps, not just the slot the inner content
        // happens to fill. Compose's default Clickable applies the
        // platform ripple via `indication = LocalIndication.current`.
        // Object-init + collection-init can't be mixed in one
        // initializer, so build the Box then Add the child explicitly.
        var box = new Box
        {
            Modifier = Modifier.Companion.FillMaxWidth().Clickable(onClick),
        };
        box.Add(child);
        return box;
    }

    static void OnItemTapped(MauiCollectionView view, object item)
    {
        switch (view.SelectionMode)
        {
            case Microsoft.Maui.Controls.SelectionMode.Single:
                // MAUI's SelectedItem two-way binding + SelectionChanged
                // event fire from the BindableProperty setter, so a plain
                // assignment is enough. No feedback-loop guard is needed
                // because we're not reading SelectedItem inside BuildNode
                // — clicks are pure write-throughs into MAUI.
                view.SelectedItem = item;
                break;

            case Microsoft.Maui.Controls.SelectionMode.Multiple:
                // Toggle membership. The SelectedItems list is a
                // `MarshalingObservableCollection` under the hood;
                // mutating it raises CollectionChanged which MAUI
                // wires into SelectionChanged.
                var selected = view.SelectedItems;
                if (selected is null)
                    return;
                if (selected.Contains(item))
                    selected.Remove(item);
                else
                    selected.Add(item);
                break;

            // SelectionMode.None never reaches this method because the
            // Clickable wrapper is suppressed in BuildNode.
        }
    }
}
