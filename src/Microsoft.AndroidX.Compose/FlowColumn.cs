namespace AndroidX.Compose;

/// <summary>
/// Foundation <c>FlowColumn</c> — vertical mirror of <see cref="FlowRow"/>.
/// Children are stacked top-to-bottom in the first column; once a
/// column runs out of vertical space, subsequent children flow into a
/// new column to the right.
///
/// <code>
/// new FlowColumn
/// {
///     new Text("Item A"),
///     new Text("Item B"),
///     // …more items, will start a new column when out of vertical space.
/// }
/// </code>
///
/// Set <see cref="Overflow"/> to supply expand/collapse indicators.
/// Omission uses Kotlin's clip behavior. Overflow support retains the deprecated
/// Foundation 1.11.3 API; see <see cref="FlowOverflowScope"/> for count timing. Because
/// <c>FlowColumnScope</c> extends <c>ColumnScope</c>, scope-aware
/// modifiers like <see cref="Modifier.Weight(float, bool)"/> and
/// <see cref="Modifier.Align(Alignment.Horizontal)"/> work on
/// children here exactly as they do inside a plain
/// <see cref="Column"/>.
/// </summary>
public sealed partial class FlowColumn;
