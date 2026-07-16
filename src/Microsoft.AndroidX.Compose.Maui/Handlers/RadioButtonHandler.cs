using AndroidX.Compose;
using AndroidX.Compose.Material3;
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
            [nameof(IRadioButton.IsChecked)]       = MapIsChecked,
            [nameof(MauiRadioButton.Content)]      = MapContent,
            [nameof(ITextStyle.TextColor)]         = MapTextColor,
            [nameof(ITextStyle.Font)]              = MapFont,
            [nameof(ITextStyle.CharacterSpacing)]  = MapCharacterSpacing,
            [nameof(IButtonStroke.StrokeColor)]    = MapStrokeColor,
            // TODO: IButtonStroke.{StrokeThickness, CornerRadius} —
            // Material 3's RadioButton draws a fixed 20.dp circle whose
            // ring thickness and shape are baked into the composable's
            // internal Canvas (`drawCircle` with a hard-coded
            // `RadioStrokeWidth = 2.dp`); the public surface
            // (`RadioButtonColors`) only exposes the ring colour, not
            // its geometry. Honouring these would require replacing
            // the call to `RadioButtonKt.RadioButton` with a hand-rolled
            // `Canvas { drawCircle / drawArc }` composable, which loses
            // Material 3's ripple, state-layer, focus indicator, and
            // accessibility chrome. A `CornerRadius` override is also
            // semantically off — XAML callers reaching for it on a
            // `RadioButton` are typically trying to make a control that
            // is no longer recognisable as a radio button. Held back
            // deliberately; revisit if a public M3 API surfaces these
            // knobs.
        };

    /// <summary>Command mapper (inherits view-level commands; no extras).</summary>
    public static CommandMapper<IRadioButton, RadioButtonHandler> CommandMapper =
        new(ViewCommandMapper);

    readonly MutableState<bool>   _checked  = new(false);
    readonly MutableState<string> _label    = new(string.Empty);
    readonly MutableState<long?>  _color    = new((long?)null);
    readonly MutableState<int?>   _fontSize = new((int?)null);
    readonly MutableState<bool>   _bold     = new(false);
    readonly MutableState<float?> _letterSpacing = new((float?)null);
    readonly MutableState<long?>  _strokeColor   = new((long?)null);

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
        var spacing = _letterSpacing.Value;
        var label  = _label.Value;
        var stroke = _strokeColor.Value;

        var radio = new ComposeRadioButton(selected: _checked.Value, onClick: OnSelected);
        if (stroke is { } strokeColor)
            radio.Colors = composer.RadioButtonColors(
                selectedColor:   ComposeColor.FromPacked(strokeColor),
                unselectedColor: ComposeColor.FromPacked(strokeColor));
        var gestureModifier = Modifier.Companion.ApplyGestures(virtualView, MauiContext).ApplySemantics(virtualView);
        if (string.IsNullOrEmpty(label))
        {
            radio.PrependModifier(gestureModifier);
            return radio;
        }

        var text = new ComposeText(label);
        if (packed is { } textColor)
            text.Color = ComposeColor.FromPacked(textColor);
        if (size.HasValue)
            text.FontSize = new Sp(size.Value);
        if (bold)
            text.FontWeight = ComposeFontWeight.Bold;
        if (spacing.HasValue)
            text.LetterSpacing = new Sp(1) * spacing.Value;

        var row = new Row(horizontalArrangement: null,
                          verticalAlignment: Alignment.Vertical.CenterVertically)
        {
            radio,
            text,
        };
        // Fill the available width so the radio sits at the start of its
        // layout slot — without it the Row hugs `radio + label` and the
        // MAUI host centers each row independently, which makes a column
        // of radios with different label lengths visibly stagger
        // horizontally. Matches stock MAUI's left-anchored layout.
        row.Modifier = Modifier.Companion.FillMaxWidth().Then(gestureModifier);
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

    /// <summary>
    /// Map <see cref="ITextStyle.CharacterSpacing"/> to the Compose
    /// label's <c>letterSpacing</c> slot (sp). MAUI's value is em-ish;
    /// the multiplication operator on <see cref="Sp"/> packs a
    /// fractional Sp without losing the signal that an
    /// <c>int</c>-only constructor would round to zero.
    /// </summary>
    public static void MapCharacterSpacing(RadioButtonHandler handler, IRadioButton rb) =>
        handler._letterSpacing.Value = rb.CharacterSpacing != 0 ? (float)rb.CharacterSpacing : null;

    /// <summary>
    /// Map <see cref="IButtonStroke.StrokeColor"/> to the Compose
    /// <see cref="RadioButtonColors"/> ring slots — applied to both
    /// <c>selectedColor</c> and <c>unselectedColor</c> so the user-
    /// supplied colour is visible whether the radio is on or off
    /// (matches stock MAUI's <c>UpdateStrokeColor</c>, which tints
    /// the ring drawable in every check state). The disabled-state
    /// ring colour stays on the M3 theme default.
    /// </summary>
    public static void MapStrokeColor(RadioButtonHandler handler, IRadioButton rb) =>
        handler._strokeColor.Value = ColorMapping.ToPackedLong(rb.StrokeColor);
}
