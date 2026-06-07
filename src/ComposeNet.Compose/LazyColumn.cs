using System;
using System.Collections.Generic;
using Android.Runtime;
using AndroidX.Compose.Foundation.Lazy;
using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Foundation <c>LazyColumn</c> — the lazy equivalent of <see cref="Column"/>.
/// Only the items currently visible (plus a small lookahead) are
/// composed and laid out, so it's the right choice any time the child
/// count exceeds what fits on screen.
///
/// The facade is data-driven instead of collection-init: you pass the
/// items list and a per-item content callback at construction time.
/// Compose handles scrolling automatically — no scroll-state plumbing
/// required for the common case.
///
/// <code>
/// new LazyColumn&lt;int&gt;(
///     items: Enumerable.Range(0, 1000).ToList(),
///     itemContent: i =&gt; new Text($"Row {i}"))
/// </code>
///
/// Set <see cref="State"/> (typically the result of a remembered
/// <see cref="LazyListState"/>) to read scroll position or drive
/// programmatic scrolling. When left null Compose creates its own
/// internal state via the Kotlin default <c>rememberLazyListState()</c>.
/// </summary>
public sealed class LazyColumn<T> : ComposableNode
{
    readonly IReadOnlyList<T> _items;
    readonly Func<T, ComposableNode> _itemContent;

    public LazyColumn(IReadOnlyList<T> items, Func<T, ComposableNode> itemContent)
    {
        _items       = items       ?? throw new ArgumentNullException(nameof(items));
        _itemContent = itemContent ?? throw new ArgumentNullException(nameof(itemContent));
    }

    /// <summary>
    /// Optional scroll state. Leave null to let Compose substitute its
    /// own remembered state.
    /// </summary>
    public LazyListState? State { get; set; }

    public override void Render(IComposer composer)
    {
        var modifier = BuildModifier();
        var content  = new ComposableLambda1(scopeObj =>
        {
            var scope = Android.Runtime.Extensions.JavaCast<ILazyListScope>(scopeObj!);
            // contentType is non-nullable in Kotlin (defaults to { null }),
            // so we always provide a stub; key is nullable and we pass null
            // to keep the Compose-default positional keying.
            // itemContent IS @Composable but runs at measure time inside the
            // lazy list's SubcomposeLayout, so route through Instantiate4
            // (composer-less ComposableLambdaInstance) — Wrap4 would crash
            // because there's no active composer once the DSL builder runs.
            scope.Items(
                _items.Count,
                key:         null,
                contentType: new ComposableLambda1(_ => { }),
                itemContent: ComposableLambdas.Instantiate4((_, indexBoxed, comp) =>
                {
                    var i = ((Java.Lang.Integer)indexBoxed!).IntValue();
                    _itemContent(_items[i]).Render(comp);
                }));
        });

        int defaults = (int)LazyColumnDefault.All;
        if (modifier is not null) defaults &= ~(int)LazyColumnDefault.Modifier;
        if (State    is not null) defaults &= ~(int)LazyColumnDefault.State;

        LazyDslKt.LazyColumn(
            modifier:            modifier,
            state:               State,
            contentPadding:      null,
            reverseLayout:       false,
            verticalArrangement: null,
            horizontalAlignment: null,
            flingBehavior:       null,
            userScrollEnabled:   true,
            overscrollEffect:    null,
            content:             content,
            _composer:           composer,
            p11:                 0,
            _changed:            defaults);
    }
}
