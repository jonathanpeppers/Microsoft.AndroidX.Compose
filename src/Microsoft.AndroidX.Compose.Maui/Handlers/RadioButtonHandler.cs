using AndroidX.Compose;
using AndroidX.Compose.Runtime;
using Microsoft.AndroidX.Compose.Maui.Platform;
using Microsoft.Maui.Handlers;
using ComposeColor      = AndroidX.Compose.Color;
using ComposeFontWeight = AndroidX.Compose.FontWeight;
using ComposeRadioButton = AndroidX.Compose.RadioButton;
using ComposeText       = AndroidX.Compose.Text;
using MauiRadioButton   = Microsoft.Maui.Controls.RadioButton;

namespace Microsoft.AndroidX.Compose.Maui.Handlers;

/// <summary>
/// MAUI <see cref="Microsoft.Maui.Controls.RadioButton"/> handler that
/// renders through Jetpack Compose's Material 3 <c>RadioButton</c>
/// composable — paired in a Compose <see cref="Row"/> with a
/// <see cref="ComposeText"/> label so the rendered chrome matches
/// MAUI's stock <c>AppCompatRadioButton</c>. Replaces MAUI's stock
/// handler when the consumer calls
/// <see cref="Hosting.AppHostBuilderExtensions.UseAndroidXCompose"/>.
/// </summary>
/// <remarks>
/// <para>Folds into the page's single composition via
/// <see cref="ComposeElementHandler{TVirtualView}"/> /
/// <see cref="IComposeHandler"/>. Two-way binding mirrors
/// <see cref="EntryHandler"/>: the Compose <c>onClick</c> writes
/// <see cref="IRadioButton.IsChecked"/><c> = true</c> on the tapped
/// virtual view, which re-enters <see cref="MapIsChecked"/> — equality
/// on <see cref="MutableState{T}"/> short-circuits the loop without a
/// guard flag.</para>
///
/// <para>Group semantics
/// (<see cref="Microsoft.Maui.Controls.RadioButtonGroup"/> /
/// <see cref="Microsoft.Maui.Controls.RadioButton.GroupName"/>) are
/// already enforced by MAUI's <c>RadioButtonGroupController</c> — when
/// one radio in a group flips to <c>true</c>, the controller raises
/// <c>CheckedChanged(false)</c> on the previous selection through the
/// virtual view's normal property pipeline. The handler doesn't
/// reimplement any of that; it simply surfaces
/// <see cref="IRadioButton.IsChecked"/> faithfully.</para>
///
/// <para><see cref="MauiRadioButton.Content"/> is read off the
/// concrete control type (it isn't on <see cref="IRadioButton"/>) and
/// rendered as a <see cref="ComposeText"/> next to the radio circle.
/// String content uses the value verbatim; anything else lowers
/// through <c>ToString()</c> matching MAUI's stock platform handler.
/// View-typed <c>Content</c> isn't supported here — pass a string or
/// build a custom Compose-rendered control.</para>
/// </remarks>
public partial class RadioButtonHandler : ComposeElementHandler<IRadioButton>
{
    /// <summary>
    /// Property mapper that forwards MAUI <see cref="IRadioButton"/>
    /// property changes to the Compose-backed <see cref="ComposeView"/>.
    /// </summary>
    public static IPropertyMapper<IRadioButton, RadioButtonHandler> Mapper =
        new PropertyMapper<IRadioButton, RadioButtonHandler>(ViewHandler.ViewMapper)
        {
            [nameof(IRadioButton.IsChecked)]      = MapIsChecked,
            [nameof(MauiRadioButton.Content)]     = MapContent,
            [nameof(ITextStyle.TextColor)]        = MapTextColor,
            [nameof(ITextStyle.Font)]             = MapFont,
        };

    /// <summary>Command mapper (inherits view-level commands; no extras).</summary>
    public static CommandMapper<IRadioButton, RadioButtonHandler> CommandMapper =
        new(ViewCommandMapper);

    readonly MutableState<bool>   _checked  = new(false);
    readonly MutableState<string> _label    = new(string.Empty);
    readonly MutableState<long?>  _color    = new((long?)null);
    readonly MutableState<int?>   _fontSize = new((int?)null);
    readonly MutableState<bool>   _bold     = new(false);

    /// <summary>Construct a handler with the default mappers.</summary>
    public RadioButtonHandler() : base(Mapper, CommandMapper) { }

    /// <summary>Construct a handler with custom mappers.</summary>
    public RadioButtonHandler(IPropertyMapper? mapper, CommandMapper? commandMapper = null)
        : base(mapper ?? Mapper, commandMapper ?? CommandMapper) { }

    /// <inheritdoc/>
    public override ComposableNode BuildNode(IComposer composer)
    {
        var virtualView = VirtualView
            ?? throw new InvalidOperationException("VirtualView not set on RadioButtonHandler.");

        var packed = _color.Value;
        var size   = _fontSize.Value;
        var bold   = _bold.Value;
        var label  = _label.Value;

        var radio = new ComposeRadioButton(selected: _checked.Value, onClick: OnSelected);
        var gestureModifier = Modifier.Companion.ApplyGestures(virtualView, MauiContext).ApplySemantics(virtualView);
        if (string.IsNullOrEmpty(label))
        {
            radio.PrependModifier(gestureModifier);
            return radio;
        }

        var text = new ComposeText(label);
        if (packed.HasValue)
            text.Color = new ComposeColor(packed.Value);
        if (size.HasValue)
            text.FontSize = new Sp(size.Value);
        if (bold)
            text.FontWeight = ComposeFontWeight.Bold;

        var row = new Row(horizontalArrangement: null,
                          verticalAlignment: Alignment.Vertical.CenterVertically)
        {
            radio,
            text,
        };
        row.Modifier = gestureModifier;
        return row;
    }

    void OnSelected()
    {
        // The Compose `onClick` only fires for unselected → selected;
        // a tap on an already-checked radio is a no-op (matches Kotlin
        // `RadioButton`'s own behaviour). Pin Compose state synchronously
        // so the rendered ring reflects the user's tap immediately —
        // matches `CheckBoxHandler.OnCheckedChanged` and
        // `SwitchHandler.OnCheckedChanged`. Then surface IsChecked = true
        // so MAUI's `RadioButtonGroupController` raises
        // CheckedChanged(false) on the previous selection automatically;
        // we don't reimplement group semantics here.
        _checked.Value = true;
        if (VirtualView is { } v)
            v.IsChecked = true;
    }

    /// <summary>Map <see cref="IRadioButton.IsChecked"/> to the Compose <c>selected</c> slot.</summary>
    public static void MapIsChecked(RadioButtonHandler handler, IRadioButton rb) =>
        handler._checked.Value = rb.IsChecked;

    /// <summary>
    /// Map <see cref="MauiRadioButton.Content"/> to the Compose label
    /// slot. Read off the concrete control type because
    /// <see cref="IRadioButton"/> exposes content only as
    /// <see cref="IContentView.PresentedContent"/>, which is null when
    /// the binding holds a string. <see langword="null"/> /
    /// <see cref="View"/>-typed content lowers to the empty string —
    /// the radio circle is rendered without a sibling label.
    /// </summary>
    public static void MapContent(RadioButtonHandler handler, IRadioButton rb)
    {
        if (rb is MauiRadioButton concrete && concrete.Content is { } content && content is not Microsoft.Maui.Controls.View)
            handler._label.Value = content.ToString() ?? string.Empty;
        else
            handler._label.Value = string.Empty;
    }

    /// <summary>Map <see cref="ITextStyle.TextColor"/> to the Compose label colour.</summary>
    public static void MapTextColor(RadioButtonHandler handler, IRadioButton rb) =>
        handler._color.Value = ColorMapping.ToPackedLong(rb.TextColor);

    /// <summary>Map <see cref="ITextStyle.Font"/> (size + bold) to the Compose label slots.</summary>
    public static void MapFont(RadioButtonHandler handler, IRadioButton rb)
    {
        var font = rb.Font;
        handler._fontSize.Value = font.Size > 0 ? (int)font.Size : null;
        handler._bold.Value     = (font.Weight & Microsoft.Maui.FontWeight.Bold)
            == Microsoft.Maui.FontWeight.Bold;
    }
}
