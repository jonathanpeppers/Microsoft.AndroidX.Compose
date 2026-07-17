namespace AndroidX.Compose;

/// <summary>
/// Material 3 <c>ModalWideNavigationRail</c> — the modal-overlay
/// variant of <see cref="WideNavigationRail"/>. Drawn on top of the
/// host screen with a scrim, suitable for narrow-width adaptive
/// layouts. Children flow into the rail body; <c>Header</c> is an
/// optional slot drawn above them.
/// </summary>
/// <remarks>
/// <para>The Kotlin signature has no <c>onDismissRequest</c> callback —
/// dismissal is driven internally by the rail's
/// <see cref="WideNavigationRailState"/>. The facade is wired with
/// <c>hideOnCollapse = true</c>, so its content visually unmounts after
/// collapse. Use <see cref="WideNavigationRailState.ExpandAsync"/>,
/// <see cref="WideNavigationRailState.CollapseAsync"/>,
/// <see cref="WideNavigationRailState.ToggleAsync"/>, or
/// <see cref="WideNavigationRailState.SnapToAsync"/> to control it, and
/// observe <see cref="WideNavigationRailState.CurrentValue"/>,
/// <see cref="WideNavigationRailState.TargetValue"/>, and
/// <see cref="WideNavigationRailState.IsAnimating"/>.</para>
/// <code>
/// var rail = Remember(() =&gt; new WideNavigationRailState(
///     WideNavigationRailValue.Collapsed));
/// new Column
/// {
///     new Button(onClick: () =&gt; _ = rail.ToggleAsync())
///     {
///         new Text("Toggle menu"),
///     },
///     new ModalWideNavigationRail(state: rail)
///     {
///         Header = new Text("Sections"),
///         new WideNavigationRailItem(selected: true, onClick: () =&gt; { })
///         {
///             Icon = new Text("Home"),
///             Label = new Text("Home"),
///         },
///     },
/// };
/// </code>
/// </remarks>
public sealed partial class ModalWideNavigationRail;
