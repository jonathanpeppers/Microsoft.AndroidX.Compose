using AndroidX.Compose.Material3;

namespace AndroidX.Compose;

/// <summary>
/// Caller-supplied state for a <see cref="Tooltip"/> that exposes
/// programmatic show and dismiss control.
/// </summary>
/// <remarks>
/// Create one instance inside <c>Remember</c>, then pass it to
/// <see cref="Tooltip(TooltipState)"/>. The state binds to
/// Compose's remembered <c>TooltipState</c> when the tooltip first renders.
/// </remarks>
public sealed class TooltipState
{
    internal ITooltipState? Jvm;

    /// <summary>Whether the tooltip remains visible until explicitly dismissed.</summary>
    public bool IsPersistent { get; }

    /// <summary>Whether the tooltip is currently visible.</summary>
    public bool IsVisible => Jvm?.IsVisible ?? false;

    /// <summary>Creates tooltip state with the requested persistence behavior.</summary>
    /// <param name="isPersistent">
    /// <c>true</c> to keep the tooltip visible until <see cref="Dismiss"/> is
    /// called; <c>false</c> to use Compose's timed dismissal.
    /// </param>
    public TooltipState(bool isPersistent = false) =>
        IsPersistent = isPersistent;

    /// <summary>
    /// Shows the tooltip and completes when Compose dismisses it.
    /// </summary>
    /// <param name="cancellationToken">
    /// Cancels the returned task and the underlying Kotlin show operation.
    /// </param>
    public Task ShowAsync(CancellationToken cancellationToken = default) =>
        SuspendBridge.Invoke(
            cont => ComposeBridges.TooltipStateShow(
                RequireJvm().Handle, mutatePriority: null, cont),
            cancellationToken);

    /// <summary>Dismisses the tooltip immediately.</summary>
    public void Dismiss() => RequireJvm().Dismiss();

    ITooltipState RequireJvm() =>
        Jvm ?? throw new InvalidOperationException(
            "TooltipState is not bound. Render it with Tooltip before controlling it.");
}
