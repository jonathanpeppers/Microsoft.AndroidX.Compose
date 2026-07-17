using AndroidX.Compose.Material3;

namespace AndroidX.Compose;

/// <summary>
/// Caller-supplied state holder for <see cref="ModalWideNavigationRail"/>.
/// Wraps Kotlin's <c>WideNavigationRailState</c> (created via
/// <c>rememberWideNavigationRailState</c>) so a facade can carry an
/// initial <see cref="WideNavigationRailValue"/> across recompositions.
/// </summary>
/// <remarks>
/// Construct inside <c>Remember</c> so the same instance survives
/// recomposition:
/// <code>
/// var rail = Remember(() =&gt; new WideNavigationRailState(WideNavigationRailValue.Expanded));
/// new ModalWideNavigationRail(state: rail) { … };
/// </code>
/// </remarks>
public sealed class WideNavigationRailState
{
    internal IWideNavigationRailState? Jvm;

    /// <summary>
    /// Initial <see cref="WideNavigationRailValue"/> the rail
    /// remembers on first composition. Defaults to
    /// <see cref="WideNavigationRailValue.Expanded"/> so the rail
    /// pops open as soon as it mounts (matches the modal-overlay
    /// pattern documented on <see cref="ModalWideNavigationRail"/>).
    /// </summary>
    public WideNavigationRailValue InitialValue { get; }

    /// <summary>
    /// Construct a holder with the given <paramref name="initialValue"/>
    /// (or <see cref="WideNavigationRailValue.Expanded"/> when omitted).
    /// </summary>
    public WideNavigationRailState(WideNavigationRailValue? initialValue = null)
    {
        InitialValue = initialValue
            ?? WideNavigationRailValue.Expanded
            ?? throw new InvalidOperationException(
                "WideNavigationRailValue.Expanded was unavailable.");
    }

    /// <summary>
    /// The rail's current visual state. Falls back to
    /// <see cref="InitialValue"/> until the holder is bound to a live
    /// peer (i.e. the first render has happened).
    /// </summary>
    public WideNavigationRailValue CurrentValue =>
        Jvm?.CurrentValue ?? InitialValue;

    /// <summary>
    /// The rail's target state during animation, or the resting state
    /// otherwise. Falls back to <see cref="InitialValue"/> until bound.
    /// </summary>
    public WideNavigationRailValue TargetValue =>
        Jvm?.TargetValue ?? InitialValue;

    /// <summary>Whether the rail is currently animating.</summary>
    public bool IsAnimating => Jvm?.IsAnimating ?? false;

    /// <summary>Animates the rail to its expanded state.</summary>
    /// <param name="cancellationToken">Cancels the returned task and stops the Kotlin animation at its next cancellable suspend point.</param>
    public Task ExpandAsync(CancellationToken cancellationToken = default) =>
        SuspendBridge.Invoke(
            cont => ComposeBridges.WideNavigationRailStateExpand(
                ((Java.Lang.Object)RequireJvm()).Handle, cont),
            cancellationToken);

    /// <summary>Animates the rail to its collapsed state.</summary>
    /// <param name="cancellationToken">Cancels the returned task and stops the Kotlin animation at its next cancellable suspend point.</param>
    public Task CollapseAsync(CancellationToken cancellationToken = default) =>
        SuspendBridge.Invoke(
            cont => ComposeBridges.WideNavigationRailStateCollapse(
                ((Java.Lang.Object)RequireJvm()).Handle, cont),
            cancellationToken);

    /// <summary>Animates the rail to the opposite of its current target state.</summary>
    /// <param name="cancellationToken">Cancels the returned task and stops the Kotlin animation at its next cancellable suspend point.</param>
    public Task ToggleAsync(CancellationToken cancellationToken = default) =>
        SuspendBridge.Invoke(
            cont => ComposeBridges.WideNavigationRailStateToggle(
                ((Java.Lang.Object)RequireJvm()).Handle, cont),
            cancellationToken);

    /// <summary>Snaps the rail immediately to a collapsed or expanded state.</summary>
    /// <param name="targetValue">Target rail value.</param>
    /// <param name="cancellationToken">Cancels the returned task and the underlying Kotlin operation.</param>
    public Task SnapToAsync(
        WideNavigationRailValue targetValue,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(targetValue);
        return SuspendBridge.Invoke(
            cont => ComposeBridges.WideNavigationRailStateSnapTo(
                ((Java.Lang.Object)RequireJvm()).Handle, targetValue, cont),
            cancellationToken);
    }

    IWideNavigationRailState RequireJvm() =>
        Jvm ?? throw new InvalidOperationException(
            "WideNavigationRailState is not bound. Render it with ModalWideNavigationRail before controlling it.");
}
