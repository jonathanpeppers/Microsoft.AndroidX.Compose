using AndroidX.Compose.Foundation.Lazy.Grid;
using AndroidX.Compose.Runtime;
using BindingArrangement = AndroidX.Compose.Foundation.Layout.Arrangement;

namespace AndroidX.Compose;

/// <summary>
/// Foundation <c>LazyVerticalGrid</c> — lazy 2D grid that scrolls
/// vertically. Cells are arranged into <see cref="GridCells.Fixed"/>
/// columns (count) or <see cref="GridCells.Adaptive"/> columns (minimum
/// cell width in dp); items flow left-to-right, top-to-bottom.
///
/// <code>
/// new LazyVerticalGrid&lt;int&gt;(
///     columns:     GridCells.Fixed(3),
///     items:       Enumerable.Range(0, 50).ToList(),
///     itemContent: i =&gt; new Card { new Text($"Cell {i}") })
/// </code>
/// </summary>
public sealed class LazyVerticalGrid<T> : ComposableNode
{
    readonly IGridCells _columns;
    readonly IReadOnlyList<T> _items;
    readonly Func<T, ComposableNode> _itemContent;

    public LazyVerticalGrid(IGridCells columns, IReadOnlyList<T> items, Func<T, ComposableNode> itemContent)
    {
        ArgumentNullException.ThrowIfNull(columns);
        _columns     = columns;
        ArgumentNullException.ThrowIfNull(items);
        _items       = items;
        ArgumentNullException.ThrowIfNull(itemContent);
        _itemContent = itemContent;
    }

    /// <summary>
    /// Optional scroll state. Leave null to let Compose substitute its
    /// own remembered state.
    /// </summary>
    public LazyGridState? State { get; set; }

    /// <summary>
    /// Optional fixed content padding applied inside the grid (not as a
    /// modifier on the grid frame). Items can scroll behind this area
    /// but the first/last items stay fully reachable.
    /// </summary>
    public PaddingValues? ContentPadding { get; set; }

    /// <summary>
    /// Optional vertical arrangement (e.g. <see cref="Arrangement.SpacedBy(int)"/>)
    /// applied between grid rows. Leave <see langword="null"/> to use Compose's
    /// default (<c>Arrangement.Top</c>). Must wrap a vertical-capable Compose
    /// <see cref="Arrangement"/>; throws <see cref="ArgumentException"/> at
    /// render time for a horizontal-only value.
    /// </summary>
    public Arrangement? VerticalArrangement { get; set; }

    /// <summary>
    /// Optional horizontal arrangement (e.g. <see cref="Arrangement.SpacedBy(int)"/>)
    /// applied between grid columns. Leave <see langword="null"/> to use
    /// Compose's default (<c>Arrangement.Start</c>). Must wrap a
    /// horizontal-capable Compose <see cref="Arrangement"/>; throws
    /// <see cref="ArgumentException"/> at render time for a vertical-only value.
    /// </summary>
    public Arrangement? HorizontalArrangement { get; set; }

    public override void Render(IComposer composer)
    {
        var modifier = BuildModifier();
        var content  = new ComposableLambda1(scopeObj =>
        {
            var scope = Android.Runtime.Extensions.JavaCast<ILazyGridScope>(scopeObj!);
            // span: null = 1 cell per item (the Compose default).
            scope.Items(
                _items.Count,
                key:         null,
                span:        null,
                contentType: new ComposableLambda1(_ => { }),
                p4:          ComposableLambdas.Instantiate4((_, indexBoxed, comp) =>
                {
                    var i = ((Java.Lang.Integer)indexBoxed!).IntValue();
                    _itemContent(_items[i]).Render(comp);
                }));
        });

        // Start from "use all defaults", then clear the columns bit
        // (always supplied) and any other params the caller customized.
        int defaults = (int)LazyVerticalGridDefault.All & ~(int)LazyVerticalGridDefault.Columns;
        if (modifier       is not null) defaults &= ~(int)LazyVerticalGridDefault.Modifier;
        if (State          is not null) defaults &= ~(int)LazyVerticalGridDefault.State;
        if (ContentPadding is not null) defaults &= ~(int)LazyVerticalGridDefault.ContentPadding;

        BindingArrangement.IVertical? vertical = null;
        if (VerticalArrangement is not null)
        {
            vertical = VerticalArrangement.Vertical
                ?? throw new ArgumentException(
                    $"{nameof(VerticalArrangement)} must wrap a vertical " +
                    $"or horizontal-or-vertical Compose Arrangement; got a " +
                    $"horizontal-only value (e.g. Arrangement.Start / Arrangement.End).",
                    nameof(VerticalArrangement));
            defaults &= ~(int)LazyVerticalGridDefault.VerticalArrangement;
        }

        BindingArrangement.IHorizontal? horizontal = null;
        if (HorizontalArrangement is not null)
        {
            horizontal = HorizontalArrangement.Horizontal
                ?? throw new ArgumentException(
                    $"{nameof(HorizontalArrangement)} must wrap a horizontal " +
                    $"or horizontal-or-vertical Compose Arrangement; got a " +
                    $"vertical-only value (e.g. Arrangement.Top / Arrangement.Bottom).",
                    nameof(HorizontalArrangement));
            defaults &= ~(int)LazyVerticalGridDefault.HorizontalArrangement;
        }

        // 11 user params → Compose splits $changed into two ints
        // (p12, _changed) and $default lives in the trailing _changed1.
        LazyGridDslKt.LazyVerticalGrid(
            columns:               _columns,
            modifier:              modifier,
            state:                 State,
            contentPadding:        ContentPadding?.Jvm,
            reverseLayout:         false,
            verticalArrangement:   vertical,
            horizontalArrangement: horizontal,
            flingBehavior:         null,
            userScrollEnabled:     true,
            overscrollEffect:      null,
            content:               content,
            _composer:             composer,
            p12:                   0,
            _changed:              0,
            _changed1:             defaults);
    }
}
