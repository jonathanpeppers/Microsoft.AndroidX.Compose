using AndroidX.Compose;
using AndroidX.Compose.Runtime;
using Microsoft.Maui.Handlers;
using ComposeIconButton  = AndroidX.Compose.IconButton;
using ComposeRow         = AndroidX.Compose.Row;
using ComposeText        = AndroidX.Compose.Text;

namespace Microsoft.AndroidX.Compose.Maui.Handlers;

/// <summary>
/// MAUI <see cref="Microsoft.Maui.Controls.Stepper"/> handler that
/// renders through Jetpack Compose. Material 3 has no first-class
/// <c>Stepper</c>, so the handler synthesizes one from a
/// <see cref="ComposeRow"/> containing two <see cref="ComposeIconButton"/>s
/// (<c>−</c> / <c>+</c>). Replaces MAUI's stock
/// <c>android.widget.LinearLayout</c>-based handler when the consumer
/// calls <see cref="Hosting.AppHostBuilderExtensions.UseAndroidXCompose"/>.
/// </summary>
/// <remarks>
/// <para>Folds into the page's single composition via
/// <see cref="ComposeElementHandler{TVirtualView}"/> /
/// <see cref="IComposeHandler"/>. The two-way value pipeline mirrors
/// <see cref="EntryHandler.OnValueChanged"/>: each button click
/// updates the local <see cref="MutableState{T}"/> first (so the
/// handler tracks the user's tap immediately) and then writes
/// to <see cref="IRange.Value"/>; MAUI's standard property pipeline
/// re-enters <see cref="MapValue"/> with the same double, which is a
/// no-op on <c>MutableState&lt;double&gt;</c> — no feedback loop.</para>
///
/// <para>Renders only <c>−</c> and <c>+</c> with no inline value Text,
/// matching MAUI's stock <c>Stepper</c> visual semantics. Consumers
/// expose the current value via a sibling <c>Label</c> bound to
/// <see cref="IRange.Value"/> (see <c>SlidersPage</c> for the canonical
/// pattern). An inline value would double-render whenever the page
/// also binds a Label, and looks misaligned because the two text
/// nodes have different font sizes / styles.</para>
///
/// <para>The decrement / increment buttons clamp at the configured
/// <see cref="IRange.Minimum"/> / <see cref="IRange.Maximum"/>; clicks
/// outside the range silently no-op. Glyphs render at the active
/// theme's <c>onSurface</c> content colour at every value (no
/// per-bound dimming): the M3 <c>IconButton</c> facade doesn't expose
/// the <c>enabled</c> slot, and a hand-rolled
/// <c>0x61000000</c> overlay reads as nearly black on a dark theme
/// — see the SlidersPage screenshot from #262 review.
/// The Material icon set bound by
/// <c>Xamarin.AndroidX.Compose.Material.Icons.Core</c> doesn't include
/// <c>Remove</c>, so the buttons use literal "−" / "+" text labels —
/// they render at the same baseline as a Material icon and look
/// identical inside the M3 <c>IconButton</c> tap target.</para>
/// </remarks>
public partial class StepperHandler : ComposeElementHandler<IStepper>
{
    /// <summary>
    /// Property mapper that forwards MAUI <see cref="IStepper"/> property
    /// changes to the Compose-backed <see cref="ComposeView"/>.
    /// </summary>
    public static IPropertyMapper<IStepper, StepperHandler> Mapper =
        new PropertyMapper<IStepper, StepperHandler>(ViewHandler.ViewMapper)
        {
            [nameof(IRange.Value)]      = MapValue,
            [nameof(IRange.Minimum)]    = MapMinimum,
            [nameof(IRange.Maximum)]    = MapMaximum,
            [nameof(IStepper.Interval)] = MapInterval,
        };

    /// <summary>Command mapper (inherits view-level commands; no extras).</summary>
    public static CommandMapper<IStepper, StepperHandler> CommandMapper =
        new(ViewCommandMapper);

    readonly MutableState<double> _value    = new(0d);
    readonly MutableState<double> _min      = new(0d);
    readonly MutableState<double> _max      = new(100d);
    readonly MutableState<double> _interval = new(1d);

    /// <summary>Construct a handler with the default mappers.</summary>
    public StepperHandler() : base(Mapper, CommandMapper) { }

    /// <summary>Construct a handler with custom mappers.</summary>
    public StepperHandler(IPropertyMapper? mapper, CommandMapper? commandMapper = null)
        : base(mapper ?? Mapper, commandMapper ?? CommandMapper) { }

    /// <inheritdoc/>
    public override ComposableNode BuildNode(IComposer composer)
    {
        var value    = _value.Value;
        var min      = _min.Value;
        var max      = _max.Value;
        var interval = _interval.Value;

        return new ComposeRow(
            horizontalArrangement: null,
            verticalAlignment:     Alignment.Vertical.CenterVertically)
        {
            new ComposeIconButton(onClick: Decrement)
            {
                new ComposeText("−"),
            },
            new ComposeIconButton(onClick: Increment)
            {
                new ComposeText("+"),
            },
        };

        void Decrement() => Apply(value - interval);
        void Increment() => Apply(value + interval);

        void Apply(double next)
        {
            var clamped = Math.Clamp(next, min, max);
            if (clamped == value) return;
            // Update Compose state synchronously so subsequent renders
            // see the new value before MAUI's bound bindings have a
            // chance to round-trip. MAUI's two-way pipeline re-enters
            // MapValue with the same double — a no-op on
            // MutableState<double>, so no feedback loop.
            _value.Value = clamped;
            if (VirtualView is { } stepper)
                stepper.Value = clamped;
        }
    }

    /// <summary>Map <see cref="IRange.Value"/> to the local mutable state slot.</summary>
    public static void MapValue(StepperHandler handler, IStepper stepper) =>
        handler._value.Value = stepper.Value;

    /// <summary>Map <see cref="IRange.Minimum"/> to the local minimum bound.</summary>
    public static void MapMinimum(StepperHandler handler, IStepper stepper) =>
        handler._min.Value = stepper.Minimum;

    /// <summary>Map <see cref="IRange.Maximum"/> to the local maximum bound.</summary>
    public static void MapMaximum(StepperHandler handler, IStepper stepper) =>
        handler._max.Value = stepper.Maximum;

    /// <summary>Map <see cref="IStepper.Interval"/> to the increment / decrement step size.</summary>
    public static void MapInterval(StepperHandler handler, IStepper stepper) =>
        handler._interval.Value = stepper.Interval;
}
