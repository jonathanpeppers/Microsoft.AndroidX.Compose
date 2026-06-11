using AndroidX.Compose;
using AndroidX.Compose.Runtime;
using AndroidX.Compose.UI.Platform;
using Microsoft.AndroidX.Compose.Maui.Platform;
using Microsoft.Maui.Handlers;
using ComposeColor          = AndroidX.Compose.Color;
using ComposeDatePicker     = AndroidX.Compose.DatePicker;
using ComposeDatePickerDialog = AndroidX.Compose.DatePickerDialog;
using ComposeFontWeight     = AndroidX.Compose.FontWeight;
using ComposeOutlinedButton = AndroidX.Compose.OutlinedButton;
using ComposeText           = AndroidX.Compose.Text;
using MauiDatePicker        = Microsoft.Maui.Controls.DatePicker;

namespace Microsoft.AndroidX.Compose.Maui.Handlers;

/// <summary>
/// MAUI <see cref="MauiDatePicker"/> handler that renders through Jetpack
/// Compose's Material 3 <see cref="ComposeDatePickerDialog"/>. Replaces
/// MAUI's stock <c>DatePickerDialog</c> Java native handler when the
/// consumer calls
/// <see cref="Hosting.AppHostBuilderExtensions.UseAndroidXCompose"/>.
/// </summary>
/// <remarks>
/// <para>MAUI's <c>DatePicker</c> is dialog-style (not inline), so the
/// composable surface mirrors the platform contract: an
/// <see cref="ComposeOutlinedButton"/> trigger labelled with the
/// formatted date pops up a Compose
/// <see cref="ComposeDatePickerDialog"/> wrapping a
/// <see cref="ComposeDatePicker"/>. <c>OK</c> writes the picked
/// <see cref="DateTime"/> back to <see cref="IDatePicker.Date"/> so
/// MAUI's standard property pipeline (data binding, behaviors,
/// validation) fires normally.</para>
///
/// <para><see cref="DateTime"/> is a CLR struct so
/// <see cref="MutableState{T}"/> doesn't model it directly — we round-trip
/// through <c>long</c> ticks (<see cref="DateTime.Ticks"/>) and
/// reconstitute live in <see cref="BuildNode(IComposer)"/>, the same
/// trick <see cref="LayoutHandler"/> uses for <c>Thickness</c>.</para>
///
/// <para>Compose's <see cref="DatePickerState"/> wrapper exposes a
/// <c>Jvm</c> field that's only populated <em>during</em> the first
/// <see cref="ComposeDatePicker"/> render — writing
/// <see cref="DatePickerState.SelectedDateMillis"/> before that point is
/// silently dropped. We seed the state from MAUI's <c>Date</c> through
/// a <see cref="LaunchedEffect"/> sibling that runs after composition,
/// keyed on the trigger so reopening the dialog with a new external
/// <c>Date</c> re-syncs.</para>
/// </remarks>
public partial class DatePickerHandler : ComposeElementHandler<IDatePicker>
{
    /// <summary>
    /// Property mapper that forwards MAUI <see cref="IDatePicker"/> property
    /// changes to the Compose-backed <see cref="ComposeView"/>.
    /// </summary>
    public static IPropertyMapper<IDatePicker, DatePickerHandler> Mapper =
        new PropertyMapper<IDatePicker, DatePickerHandler>(ViewHandler.ViewMapper)
        {
            [nameof(IDatePicker.Date)]                = MapDate,
            // MinimumDate / MaximumDate are intentionally NOT mapped here —
            // wiring them through to Compose's DatePickerState requires
            // lifting `RememberDatePickerState` to a Phase 4b parameterised
            // wrapper that surfaces `yearRange` (and ideally an
            // `ISelectableDates` adapter). Tracked as a Slice 5 follow-up.
            [nameof(IDatePicker.Format)]              = MapFormat,
            [nameof(ITextStyle.TextColor)]            = MapTextColor,
            [nameof(ITextStyle.Font)]                 = MapFont,
            [nameof(IView.HorizontalLayoutAlignment)] = MapHorizontalLayoutAlignment,
        };

    /// <summary>Command mapper (inherits view-level commands; no extras).</summary>
    public static CommandMapper<IDatePicker, DatePickerHandler> CommandMapper =
        new(ViewCommandMapper);

    readonly MutableState<long?>  _ticks      = new((long?)null);
    readonly MutableState<string> _format     = new("d");
    readonly MutableState<long?>  _textColor  = new((long?)null);
    readonly MutableState<int?>   _fontSize   = new((int?)null);
    readonly MutableState<bool>   _bold       = new(false);
    readonly MutableState<bool>   _open       = new(false);
    readonly MutableState<bool>   _fillWidth  = new(false);

    /// <summary>Construct a handler with the default mappers.</summary>
    public DatePickerHandler() : base(Mapper, CommandMapper) { }

    /// <summary>Construct a handler with custom mappers.</summary>
    public DatePickerHandler(IPropertyMapper? mapper, CommandMapper? commandMapper = null)
        : base(mapper ?? Mapper, commandMapper ?? CommandMapper) { }

    /// <inheritdoc/>
    public override ComposableNode BuildNode(IComposer composer)
    {
        // Composer-aware: the DatePickerState wrapper has to live across
        // recompositions or the dialog forgets the current selection,
        // which means it goes through `composer.Remember(...)` rather
        // than a handler field. Same `Composed` envelope as the gallery
        // demo's `c => { … }` pattern.
        return new Composed(c =>
        {
            var state = c.Remember(() => new DatePickerState());

            var ticks   = _ticks.Value;
            var format  = string.IsNullOrEmpty(_format.Value) ? "d" : _format.Value;
            var packed  = _textColor.Value;
            var size    = _fontSize.Value;
            var bold    = _bold.Value;
            var fill    = _fillWidth.Value;
            var isOpen  = _open.Value;

            var label = ticks is long t
                ? new DateTime(t).ToString(format)
                : "Pick a date";

            var trigger = new ComposeOutlinedButton(onClick: () => _open.Value = true)
            {
                BuildLabel(label, packed, size, bold),
            };
            // Combines the layout-fill (when set) with the cross-cutting view
            // properties (Opacity, Translation, Scale, Rotation, IsVisible,
            // Clip, Shadow). The dialog is a separate window, so ViewProperties
            // only applies to the trigger button.
            var outer = (fill ? Modifier.FillMaxWidth() : Modifier.Companion)
                .ApplyViewProperties(VirtualView!);
            trigger.PrependModifier(outer);

            var dialog = isOpen
                ? new ComposeDatePickerDialog(onDismissRequest: () => _open.Value = false)
                {
                    ConfirmButton = new TextButton(onClick: () =>
                    {
                        // state.Jvm is bound during the dialog's
                        // composition (which is in the past, since the
                        // user just clicked OK), so SelectedDateMillis
                        // is live at this point.
                        var ms = state.SelectedDateMillis;
                        if (ms is long m)
                        {
                            // DatePicker emits midnight UTC; convert to
                            // local-date DateTime so MAUI's Date getter
                            // round-trips cleanly.
                            var utc = DateTimeOffset.FromUnixTimeMilliseconds(m).UtcDateTime;
                            var picked = new DateTime(utc.Year, utc.Month, utc.Day,
                                                      0, 0, 0, DateTimeKind.Local);
                            _ticks.Value = picked.Ticks;
                            if (VirtualView is { } dp)
                                dp.Date = picked;
                        }
                        _open.Value = false;
                    })
                    { new ComposeText("OK") },
                    DismissButton = new TextButton(onClick: () => _open.Value = false)
                    {
                        new ComposeText("Cancel"),
                    },
                    Body = new ComposeDatePicker(state),
                }
                : (ComposableNode?)null;

            // Seed the JVM state from MAUI's current Date once
            // composition has wired it up. Keyed on _ticks so external
            // changes to MAUI's Date re-sync; user-driven picks update
            // SelectedDateMillis directly without re-keying so this
            // doesn't fight the user. Setting SelectedDateMillis before
            // the wrapper is bound is a silent no-op, so this runs
            // unconditionally — once the first dialog opens and the
            // DatePicker mounts, the next external Date push will
            // re-fire the LE and the seeding sticks.
            var seed = new LaunchedEffect((object?)ticks, ct =>
            {
                if (ticks is long localTicks)
                {
                    var dt = new DateTime(localTicks, DateTimeKind.Local).Date;
                    var ms = new DateTimeOffset(dt.Year, dt.Month, dt.Day,
                                                0, 0, 0, TimeSpan.Zero)
                        .ToUnixTimeMilliseconds();
                    state.SelectedDateMillis = ms;
                }
                return Task.CompletedTask;
            });

            return new Column
            {
                trigger,
                dialog,
                seed,
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

    /// <summary>Map <see cref="IDatePicker.Date"/> to the Compose ticks slot.</summary>
    public static void MapDate(DatePickerHandler handler, IDatePicker dp) =>
        handler._ticks.Value = dp.Date is DateTime d ? d.Ticks : null;

    /// <summary>Map <see cref="IDatePicker.Format"/> to the formatted-label slot.</summary>
    public static void MapFormat(DatePickerHandler handler, IDatePicker dp) =>
        handler._format.Value = string.IsNullOrEmpty(dp.Format) ? "d" : dp.Format!;

    /// <summary>Map <see cref="ITextStyle.TextColor"/> to the Compose <c>TextStyle.Color</c> slot.</summary>
    public static void MapTextColor(DatePickerHandler handler, IDatePicker dp) =>
        handler._textColor.Value = ColorMapping.ToPackedLong(dp.TextColor);

    /// <summary>Map <see cref="ITextStyle.Font"/> (size + bold) to the trigger label.</summary>
    public static void MapFont(DatePickerHandler handler, IDatePicker dp)
    {
        var font = dp.Font;
        handler._fontSize.Value = font.Size > 0 ? (int)font.Size : null;
        handler._bold.Value     = (font.Weight & Microsoft.Maui.FontWeight.Bold)
            == Microsoft.Maui.FontWeight.Bold;
    }

    /// <summary>
    /// Map <see cref="IView.HorizontalLayoutAlignment"/> to
    /// <c>Modifier.fillMaxWidth()</c> when the picker asks to fill its
    /// slot — same parity rule as <see cref="EntryHandler"/>.
    /// </summary>
    public static void MapHorizontalLayoutAlignment(DatePickerHandler handler, IDatePicker dp) =>
        handler._fillWidth.Value = dp.HorizontalLayoutAlignment
            == Microsoft.Maui.Primitives.LayoutAlignment.Fill;
}
