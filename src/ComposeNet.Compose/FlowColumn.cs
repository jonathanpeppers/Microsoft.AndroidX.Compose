namespace ComposeNet;

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
/// As with <see cref="FlowRow"/>, the v1 facade uses the simpler
/// 7-param overload — no <c>FlowColumnOverflow</c> slot or scope
/// helpers.
/// </summary>
public sealed partial class FlowColumn;
