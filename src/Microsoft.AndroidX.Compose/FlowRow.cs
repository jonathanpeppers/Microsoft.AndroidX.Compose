namespace AndroidX.Compose;

/// <summary>
/// Foundation <c>FlowRow</c> — like <see cref="Row"/>, but children
/// that overflow the row's width wrap onto a new line below. Use for
/// dynamic chip groups, tag clouds, or any horizontal layout where the
/// number of items is unknown at compile time.
///
/// <code>
/// new FlowRow
/// {
///     new AssistChip(onClick: ...) { Text = new Text("Music") },
///     new AssistChip(onClick: ...) { Text = new Text("Movies") },
///     // …more chips, will wrap when out of horizontal space.
/// }
/// </code>
///
/// Set <see cref="Overflow"/> to supply expand/collapse indicators.
/// Omission uses Kotlin's clip behavior. Overflow support retains the deprecated
/// Foundation 1.11.3 API; see <see cref="FlowOverflowScope"/> for count timing.
/// Because <c>FlowRowScope</c> extends
/// <c>RowScope</c>, scope-aware modifiers like
/// <see cref="Modifier.Weight(float, bool)"/> and
/// <see cref="Modifier.Align(Alignment.Vertical)"/> work on children
/// here exactly as they do inside a plain <see cref="Row"/>.
/// </summary>
public sealed partial class FlowRow;
