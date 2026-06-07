namespace ComposeNet;

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
/// v1 wires up the simplest 7-param Kotlin overload (no
/// <c>FlowRowOverflow</c> handle and no scope-receiver helpers). The
/// scope-only <c>Modifier.weight</c> extension and overflow indicator
/// slot are intentional follow-ups.
/// </summary>
public sealed partial class FlowRow;
