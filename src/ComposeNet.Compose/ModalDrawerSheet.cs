namespace ComposeNet;

/// <summary>
/// Material 3 <c>ModalDrawerSheet</c> — the panel shown by a modal-style
/// navigation drawer. Lays out children as a Column. Typically holds nav
/// items; in this facade any <see cref="ComposableNode"/> works.
/// </summary>
/// <remarks>
/// <see cref="ContainerColor"/> defaults to <c>0L</c>, which resolves to
/// <c>MaterialTheme.colorScheme.secondaryContainer</c> (visibly distinct
/// from <c>surface</c>). Set to any non-zero packed Compose <c>Color</c> to
/// override.
/// </remarks>
public sealed partial class ModalDrawerSheet;
