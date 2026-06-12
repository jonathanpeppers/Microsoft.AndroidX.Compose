using AndroidX.Compose;
using AndroidX.Compose.Material3;
using AndroidX.Compose.Runtime;
using Microsoft.AndroidX.Compose.Maui.Platform;
using Microsoft.Maui.Handlers;
using ComposeCheckbox = AndroidX.Compose.Checkbox;

namespace Microsoft.AndroidX.Compose.Maui.Handlers;

/// <summary>
/// MAUI <see cref="Microsoft.Maui.Controls.CheckBox"/> handler that
/// renders through Jetpack Compose's Material 3 <c>Checkbox</c>
/// composable. Replaces MAUI's stock <c>AppCompatCheckBox</c>-based
/// handler when the consumer calls
/// <see cref="Hosting.AppHostBuilderExtensions.UseAndroidXCompose"/>.
/// </summary>
/// <remarks>
/// <para>Folds into the page's single composition via
/// <see cref="ComposeElementHandler{TVirtualView}"/> /
/// <see cref="IComposeHandler"/>. Two-way binding mirrors
/// <see cref="EntryHandler"/>: Compose's <c>onCheckedChange</c> writes
/// the new value back to <see cref="ICheckBox.IsChecked"/>, which
/// re-enters <see cref="MapIsChecked"/> — that's a no-op against the
/// already-updated <see cref="MutableState{T}"/> so the loop
/// short-circuits without a guard flag.</para>
///
/// <para><see cref="Microsoft.Maui.Graphics.IElement.Color"/> is
/// surfaced as Compose's <see cref="CheckboxColors"/> <c>checkedColor</c>
/// slot via <see cref="ComposeExtensions.CheckboxColors"/>. MAUI's
/// stock checkbox tints the box and the checkmark together; we map
/// the same colour onto the box fill + border so the rendered chrome
/// stays internally consistent. Alpha-only colour overrides are
/// dropped as <c>0L</c>.</para>
/// </remarks>
public partial class CheckBoxHandler : ComposeElementHandler<ICheckBox>
{
    /// <summary>
    /// Property mapper that forwards MAUI <see cref="ICheckBox"/>
    /// property changes to the Compose-backed <see cref="ComposeView"/>.
    /// </summary>
    public static IPropertyMapper<ICheckBox, CheckBoxHandler> Mapper =
        new PropertyMapper<ICheckBox, CheckBoxHandler>(ViewHandler.ViewMapper)
        {
            [nameof(ICheckBox.IsChecked)] = MapIsChecked,
            [nameof(ICheckBox.Foreground)] = MapForeground,
        };

    /// <summary>Command mapper (inherits view-level commands; no extras).</summary>
    public static CommandMapper<ICheckBox, CheckBoxHandler> CommandMapper =
        new(ViewCommandMapper);

    readonly MutableState<bool>  _checked = new(false);
    readonly MutableState<long?> _color   = new((long?)null);

    /// <summary>Construct a handler with the default mappers.</summary>
    public CheckBoxHandler() : base(Mapper, CommandMapper) { }

    /// <summary>Construct a handler with custom mappers.</summary>
    public CheckBoxHandler(IPropertyMapper? mapper, CommandMapper? commandMapper = null)
        : base(mapper ?? Mapper, commandMapper ?? CommandMapper) { }

    /// <inheritdoc/>
    public override ComposableNode BuildNode(IComposer composer)
    {
        var virtualView = VirtualView
            ?? throw new InvalidOperationException("VirtualView not set on CheckBoxHandler.");

        var color = _color.Value;
        var box   = new ComposeCheckbox(@checked: _checked.Value,
                                        onCheckedChange: OnCheckedChanged);
        if (color is not null)
            box.Colors = composer.CheckboxColors(checkedColor: color);
        box.PrependModifier(Modifier.Companion.ApplyGestures(virtualView, MauiContext).ApplySemantics(virtualView));
        return box;
    }

    void OnCheckedChanged(bool newValue)
    {
        // Compose state must be flipped synchronously so the rendered
        // box reflects the user's tap; lagging here drops the tick.
        // Writing back to `VirtualView.IsChecked` re-enters MapIsChecked
        // with the same bool — `MutableState<bool>` short-circuits on
        // equality so there's no recompose-loop.
        _checked.Value = newValue;
        if (VirtualView is { } v)
            v.IsChecked = newValue;
    }

    /// <summary>Map <see cref="ICheckBox.IsChecked"/> to the Compose <c>checked</c> slot.</summary>
    public static void MapIsChecked(CheckBoxHandler handler, ICheckBox checkBox) =>
        handler._checked.Value = checkBox.IsChecked;

    /// <summary>
    /// Map <see cref="ICheckBox.Foreground"/> to the Compose
    /// <see cref="CheckboxColors"/> <c>checkedColor</c> slot. ICheckBox
    /// surfaces its tint through <c>Foreground</c> (the
    /// <see cref="Microsoft.Maui.Controls.CheckBox.Color"/> XAML
    /// property maps to it). Only solid paints participate; gradients
    /// / images / nulls fall back to the M3 theme default.
    /// </summary>
    public static void MapForeground(CheckBoxHandler handler, ICheckBox checkBox) =>
        handler._color.Value = checkBox.Foreground is SolidPaint solid
            ? ColorMapping.ToPackedLong(solid.Color)
            : null;
}
