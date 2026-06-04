using System;
using System.Collections.Generic;
using Android.Runtime;
using AndroidX.Compose.Foundation.Lazy.Grid;
using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Foundation <c>LazyHorizontalGrid</c> — lazy 2D grid that scrolls
/// horizontally. Cells are arranged into <see cref="GridCells.Fixed"/>
/// rows (count) or <see cref="GridCells.Adaptive"/> rows (minimum cell
/// height in dp); items flow top-to-bottom, left-to-right.
///
/// <code>
/// new LazyHorizontalGrid&lt;int&gt;(
///     rows:        GridCells.Fixed(2),
///     items:       items,
///     itemContent: i =&gt; new Text($"{i}"))
/// </code>
/// </summary>
public sealed class LazyHorizontalGrid<T> : ComposableNode
{
    readonly IGridCells _rows;
    readonly IReadOnlyList<T> _items;
    readonly Func<T, ComposableNode> _itemContent;

    public LazyHorizontalGrid(IGridCells rows, IReadOnlyList<T> items, Func<T, ComposableNode> itemContent)
    {
        _rows        = rows        ?? throw new ArgumentNullException(nameof(rows));
        _items       = items       ?? throw new ArgumentNullException(nameof(items));
        _itemContent = itemContent ?? throw new ArgumentNullException(nameof(itemContent));
    }

    /// <summary>
    /// Optional scroll state. Leave null to let Compose substitute its
    /// own remembered state.
    /// </summary>
    public LazyGridState? State { get; set; }

    internal override void Render(IComposer composer)
    {
        var modifier = BuildModifier();
        var content  = new ComposableLambda1(scopeObj =>
        {
            var scope = Android.Runtime.Extensions.JavaCast<ILazyGridScope>(scopeObj!);
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

        int defaults = (int)LazyHorizontalGridDefault.All & ~(int)LazyHorizontalGridDefault.Rows;
        if (modifier is not null) defaults &= ~(int)LazyHorizontalGridDefault.Modifier;
        if (State    is not null) defaults &= ~(int)LazyHorizontalGridDefault.State;

        LazyGridDslKt.LazyHorizontalGrid(
            rows:                  _rows,
            modifier:              modifier,
            state:                 State,
            contentPadding:        null,
            reverseLayout:         false,
            horizontalArrangement: null,
            verticalArrangement:   null,
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
