using AndroidX.Compose.Runtime;
using Android.Runtime;

namespace AndroidX.Compose;

/// <summary>
/// Material 3 <c>TooltipBox</c> with a plain tooltip popup. The popup
/// position provider comes from <c>TooltipDefaults.rememberPlainTooltipPositionProvider</c>;
/// <c>TooltipState</c> from <c>rememberTooltipState</c>. Both are
/// resolved via JNI inside <see cref="ComposeBridges"/>.
///
/// Use named-property syntax: <see cref="Tip"/> is the popup body shown
/// on long-press / hover, <see cref="Anchor"/> is the always-visible
/// content the popup attaches to.
/// </summary>
public sealed class Tooltip : ComposableNode
{
    readonly bool _isPersistent;
    readonly TooltipState? _state;

    /// <summary>Creates a tooltip with internally remembered state.</summary>
    /// <param name="isPersistent">
    /// <c>true</c> to keep the tooltip visible until Compose dismisses it;
    /// otherwise use the default timed behavior.
    /// </param>
    public Tooltip(bool isPersistent = false) => _isPersistent = isPersistent;

    /// <summary>Creates a tooltip controlled by caller-supplied state.</summary>
    /// <param name="state">
    /// State remembered by the caller and shared with event handlers that call
    /// <see cref="TooltipState.ShowAsync(CancellationToken)"/> or
    /// <see cref="TooltipState.Dismiss"/>.
    /// </param>
    public Tooltip(TooltipState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        _state = state;
        _isPersistent = state.IsPersistent;
    }

    /// <summary>Required: the popup body shown on long-press / hover.</summary>
    public required ComposableNode Tip { get; set; }

    /// <summary>Required: the always-visible anchor the tooltip attaches to.</summary>
    public required ComposableNode Anchor { get; set; }

    /// <summary>
    /// Whether native long-press and hover gestures show this tooltip. Defaults
    /// to true. Disable when the anchor owns a competing long-press gesture.
    /// </summary>
    public bool EnableUserInput { get; set; } = true;

    public override void Render(IComposer composer)
    {
        if (Tip is null || Anchor is null)
            throw new InvalidOperationException(
                "Tooltip requires both Tip (popup body) and Anchor (visible content).");

        var positionProvider = ComposeBridges.RememberPlainTooltipPositionProvider(composer);
        var stateHandle      = ComposeBridges.RememberTooltipState(_isPersistent, composer);
        if (_state is not null)
        {
            var state = Java.Lang.Object.GetObject<AndroidX.Compose.Material3.ITooltipState>(
                stateHandle,
                JniHandleOwnership.DoNotTransfer)
                ?? throw new InvalidOperationException(
                    "rememberTooltipState did not return a TooltipState peer.");
            _state.Jvm = state;
            var holder = _state;
            composer.DisposableEffect(holder, () => () =>
            {
                if (ReferenceEquals(holder.Jvm, state))
                    holder.Jvm = null;
            });
        }

        var tooltip = ComposableLambdas.Wrap3(composer, c => Tip.Render(c));
        var anchor  = ComposableLambdas.Wrap2(composer, c => Anchor.Render(c));

        var modifier = BuildModifier();
        int defaults = (int)TooltipBoxDefault.All;
        if (modifier is not null) defaults &= ~(int)TooltipBoxDefault.Modifier;
        // The existing generated bridge supplies false for the unrepresented bool slot.
        if (!EnableUserInput) defaults &= ~(int)TooltipBoxDefault.EnableUserInput;

        ComposeBridges.TooltipBox(
            positionProvider: positionProvider,
            tooltip:          tooltip,
            state:            stateHandle,
            modifier:         modifier,
            content:          anchor,
            defaults:         defaults,
            composer:         composer);
    }
}
