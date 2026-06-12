using AndroidX.Compose;
using AndroidX.Compose.Runtime;
using AndroidX.Compose.UI.Platform;
using Microsoft.AndroidX.Compose.Maui.Platform;
using Microsoft.Maui.Handlers;
using ComposeColor            = AndroidX.Compose.Color;
using ComposeFontWeight       = AndroidX.Compose.FontWeight;
using ComposeOutlinedButton   = AndroidX.Compose.OutlinedButton;
using ComposeText             = AndroidX.Compose.Text;
using ComposeTimePicker       = AndroidX.Compose.TimePicker;
using ComposeTimePickerDialog = AndroidX.Compose.TimePickerDialog;
using MauiTimePicker          = Microsoft.Maui.Controls.TimePicker;

namespace Microsoft.AndroidX.Compose.Maui.Handlers;

/// <summary>
/// MAUI <see cref="MauiTimePicker"/> handler that renders through Jetpack
/// Compose's Material 3 <see cref="ComposeTimePickerDialog"/>. Replaces
/// MAUI's stock <c>TimePickerDialog</c> Java native handler when the
/// consumer calls
/// <see cref="Hosting.AppHostBuilderExtensions.UseAndroidXCompose"/>.
/// </summary>
/// <remarks>
/// <para>MAUI's <c>TimePicker</c> is dialog-style (not inline). The
/// composable surface mirrors the platform contract: an
/// <see cref="ComposeOutlinedButton"/> trigger labelled with the
/// formatted time pops up a Compose
/// <see cref="ComposeTimePickerDialog"/> wrapping a
/// <see cref="ComposeTimePicker"/>. <c>OK</c> writes a
/// <see cref="TimeSpan"/> back to <see cref="ITimePicker.Time"/>.</para>
///
/// <para><see cref="TimeSpan"/> is a CLR struct so
/// <see cref="MutableState{T}"/> doesn't model it directly — round-trip
/// through <c>long</c> ticks. Compose's
/// <see cref="TimePickerState"/> wrapper takes
/// <c>initialHour</c>/<c>initialMinute</c>/<c>is24Hour</c> via its
/// constructor (Phase 4b), so we re-create the wrapper whenever the
/// stored ticks change — that's how the dialog observes external
/// <c>Time</c> writes between opens. Reads of
/// <see cref="TimePickerState.Hour"/> / <see cref="TimePickerState.Minute"/>
/// fall through to <c>InitialHour</c>/<c>InitialMinute</c> until the
/// JVM peer is bound, so the very first <c>OK</c> press still reads a
/// sensible value.</para>
/// </remarks>
public partial class TimePickerHandler : ComposeElementHandler<ITimePicker>
{
    /// <summary>
    /// Property mapper that forwards MAUI <see cref="ITimePicker"/> property
    /// changes to the Compose-backed <see cref="ComposeView"/>.
    /// </summary>
    public static IPropertyMapper<ITimePicker, TimePickerHandler> Mapper =
        new PropertyMapper<ITimePicker, TimePickerHandler>(ViewHandler.ViewMapper)
        {
            [nameof(ITimePicker.Time)]                = MapTime,
            [nameof(ITimePicker.Format)]              = MapFormat,
            [nameof(ITextStyle.TextColor)]            = MapTextColor,
            [nameof(ITextStyle.Font)]                 = MapFont,
            [nameof(IView.HorizontalLayoutAlignment)] = MapHorizontalLayoutAlignment,
        };

    /// <summary>Command mapper (inherits view-level commands; no extras).</summary>
    public static CommandMapper<ITimePicker, TimePickerHandler> CommandMapper =
        new(ViewCommandMapper);

    readonly MutableState<long?>  _ticks      = new((long?)null);
    readonly MutableState<string> _format     = new("t");
    readonly MutableState<long?>  _textColor  = new((long?)null);
    readonly MutableState<int?>   _fontSize   = new((int?)null);
    readonly MutableState<bool>   _bold       = new(false);
    readonly MutableState<bool>   _open       = new(false);
    readonly MutableState<bool>   _fillWidth  = new(false);

    /// <summary>Construct a handler with the default mappers.</summary>
    public TimePickerHandler() : base(Mapper, CommandMapper) { }

    /// <summary>Construct a handler with custom mappers.</summary>
    public TimePickerHandler(IPropertyMapper? mapper, CommandMapper? commandMapper = null)
        : base(mapper ?? Mapper, commandMapper ?? CommandMapper) { }

    /// <inheritdoc/>
    public override ComposableNode BuildNode(IComposer composer)
    {
        return new Composed(c =>
        {
            var ticks   = _ticks.Value;
            var format  = string.IsNullOrEmpty(_format.Value) ? "t" : _format.Value;
            var packed  = _textColor.Value;
            var size    = _fontSize.Value;
            var bold    = _bold.Value;
            var fill    = _fillWidth.Value;
            var isOpen  = _open.Value;

            var ts = ticks is long t ? new TimeSpan(t) : TimeSpan.Zero;
            // Compose only ships a 24-hour clock face for non-24-hour
            // mode at <see cref="ComposeTimePickerDialog"/> time — MAUI
            // honors the user's locale via the AM/PM marker in
            // `Format`. "H" or "HH" → 24h.
            var is24Hour = format.Contains('H');
            var label = ticks is long
                ? new DateTime(2000, 1, 1).Add(ts).ToString(format)
                : "Pick a time";

            // Re-key the wrapper on the stored ticks so external
            // ITimePicker.Time writes show up in the dialog the next
            // time it opens. User-driven picks rotate the wrapped
            // JVM state directly, so re-keying only happens on
            // external pushes.
            var state = c.Remember(
                () => new TimePickerState(
                    initialHour:   ts.Hours,
                    initialMinute: ts.Minutes,
                    is24Hour:      is24Hour),
                ticks,
                is24Hour);

            var trigger = new ComposeOutlinedButton(onClick: () => _open.Value = true)
            {
                BuildLabel(label, packed, size, bold),
            };
            // Combines the layout-fill (when set) with the cross-cutting view
            // properties (Opacity, Translation, Scale, Rotation, IsVisible,
            // Clip, Shadow). The dialog is a separate window, so ViewProperties
            // only applies to the trigger button.
            var outer = (fill ? Modifier.FillMaxWidth() : Modifier.Companion)
                .ApplyViewProperties(VirtualView!)
                .ApplyGestures(VirtualView!, MauiContext);
            trigger.PrependModifier(outer);

            var dialog = isOpen
                ? new ComposeTimePickerDialog(onDismissRequest: () => _open.Value = false)
                {
                    Title         = new ComposeText("Pick a time"),
                    ConfirmButton = new TextButton(onClick: () =>
                    {
                        var picked = new TimeSpan(state.Hour, state.Minute, 0);
                        _ticks.Value = picked.Ticks;
                        if (VirtualView is { } tp)
                            tp.Time = picked;
                        _open.Value = false;
                    })
                    { new ComposeText("OK") },
                    DismissButton = new TextButton(onClick: () => _open.Value = false)
                    {
                        new ComposeText("Cancel"),
                    },
                    Body = new ComposeTimePicker(state),
                }
                : (ComposableNode?)null;

            return new Column
            {
                trigger,
                dialog,
            };
        });
    }

    static ComposableNode BuildLabel(string text, long? packed, int? size, bool bold)
    {
        var node = new ComposeText(text);
        if (packed.HasValue)
            node.Color = new ComposeColor(packed.Value);
        if (size.HasValue)
            node.FontSize = new Sp(size.Value);
        if (bold)
            node.FontWeight = ComposeFontWeight.Bold;
        return node;
    }

    /// <summary>Map <see cref="ITimePicker.Time"/> to the Compose ticks slot.</summary>
    public static void MapTime(TimePickerHandler handler, ITimePicker tp) =>
        handler._ticks.Value = tp.Time is TimeSpan t ? t.Ticks : null;

    /// <summary>Map <see cref="ITimePicker.Format"/> to the formatted-label slot.</summary>
    public static void MapFormat(TimePickerHandler handler, ITimePicker tp) =>
        handler._format.Value = string.IsNullOrEmpty(tp.Format) ? "t" : tp.Format!;

    /// <summary>Map <see cref="ITextStyle.TextColor"/> to the trigger label colour slot.</summary>
    public static void MapTextColor(TimePickerHandler handler, ITimePicker tp) =>
        handler._textColor.Value = ColorMapping.ToPackedLong(tp.TextColor);

    /// <summary>Map <see cref="ITextStyle.Font"/> (size + bold) to the trigger label.</summary>
    public static void MapFont(TimePickerHandler handler, ITimePicker tp)
    {
        var font = tp.Font;
        handler._fontSize.Value = font.Size > 0 ? (int)font.Size : null;
        handler._bold.Value     = (font.Weight & Microsoft.Maui.FontWeight.Bold)
            == Microsoft.Maui.FontWeight.Bold;
    }

    /// <summary>
    /// Map <see cref="IView.HorizontalLayoutAlignment"/> to
    /// <c>Modifier.fillMaxWidth()</c> when the picker asks to fill its
    /// slot — same parity rule as <see cref="EntryHandler"/>.
    /// </summary>
    public static void MapHorizontalLayoutAlignment(TimePickerHandler handler, ITimePicker tp) =>
        handler._fillWidth.Value = tp.HorizontalLayoutAlignment
            == Microsoft.Maui.Primitives.LayoutAlignment.Fill;
}
