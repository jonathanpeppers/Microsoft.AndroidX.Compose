namespace AndroidX.Compose;

/// <summary>
/// Material 3 <c>LargeFloatingActionButton</c>. Identical shape to
/// <see cref="FloatingActionButton"/> — just the larger (96&#x202F;dp)
/// container. Use the collection-initializer form for a single icon
/// child:
///
/// <code>
/// new LargeFloatingActionButton(onClick: () => count.Value++)
/// {
///     new Text("+"),
/// }
/// </code>
/// </summary>
/// <remarks>
/// Optional colors, native elevation, and a hoisted interaction source follow
/// <see cref="FloatingActionButton"/>. Unset properties use Kotlin defaults.
/// </remarks>
public sealed partial class LargeFloatingActionButton;
