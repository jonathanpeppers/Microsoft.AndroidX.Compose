using AndroidX.Compose;
using AndroidX.Compose.Runtime;
using Microsoft.AndroidX.Compose.Maui.Platform;
using Microsoft.Maui.Handlers;
using ComposeColor = AndroidX.Compose.Color;
using ComposeText = AndroidX.Compose.Text;
using ComposeTextAlign = AndroidX.Compose.TextAlign;
using ComposeFontWeight = AndroidX.Compose.FontWeight;

namespace Microsoft.AndroidX.Compose.Maui.Handlers;

/// <summary>
/// MAUI <see cref="Microsoft.Maui.Controls.Label"/> handler that renders
/// through Jetpack Compose's <c>Text</c> composable. Replaces MAUI's stock
/// <c>AppCompatTextView</c>-based handler when the consumer calls
/// <see cref="Hosting.AppHostBuilderExtensions.UseAndroidXCompose"/>.
/// </summary>
/// <remarks>
/// Folds into the page's single composition via
/// <see cref="ComposeElementHandler{TVirtualView}"/> /
/// <see cref="IComposeHandler"/>. The label reads
/// <see cref="MutableState{T}"/> slots (text, color, font size/weight,
/// horizontal text alignment, fill-width flag) so MAUI property
/// changes propagate through the standard mapper pipeline and
/// trigger recomposition on the next frame without rebuilding the
/// platform view.
/// </remarks>
public partial class LabelHandler : ComposeElementHandler<ILabel>
{
    /// <summary>
    /// Property mapper that forwards MAUI <see cref="ILabel"/> property
    /// changes to the Compose-backed <see cref="ComposeView"/>.
    /// </summary>
    /// <remarks>
    /// Typed against this concrete handler (not <c>ILabelHandler</c>) because
    /// <see cref="PropertyMapper{TVirtualView, TViewHandler}"/> casts the
    /// handler arg of every mapper callback to <c>TViewHandler</c>, and this
    /// class doesn't implement the stock MAUI <c>ILabelHandler</c> interface.
    /// </remarks>
    public static IPropertyMapper<ILabel, LabelHandler> Mapper =
        new PropertyMapper<ILabel, LabelHandler>(ViewHandler.ViewMapper)
        {
            [nameof(IText.Text)]                  = MapText,
            [nameof(ITextStyle.TextColor)]        = MapTextColor,
            [nameof(ITextStyle.Font)]             = MapFont,
            [nameof(ILabel.HorizontalTextAlignment)] = MapHorizontalTextAlignment,
            [nameof(IView.HorizontalLayoutAlignment)] = MapHorizontalLayoutAlignment,
        };

    /// <summary>Command mapper (inherits view-level commands; no extras).</summary>
    public static CommandMapper<ILabel, LabelHandler> CommandMapper =
        new(ViewCommandMapper);

    readonly MutableState<string>      _text      = new(string.Empty);
    readonly MutableState<long?>       _color     = new((long?)null);
    readonly MutableState<int?>        _fontSize  = new((int?)null);
    readonly MutableState<bool>        _bold      = new(false);
    // Stored as the underlying int so MutableState picks the primitive
    // (IMutableIntState) path; the generic boxed path doesn't recognise
    // user-defined enums and would throw NotSupportedException at ctor time.
    readonly MutableState<int>         _hTextAlign = new((int)TextAlignment.Start);
    readonly MutableState<bool>        _fillWidth = new(false);

    /// <summary>Construct a handler with the default mappers.</summary>
    public LabelHandler() : base(Mapper, CommandMapper) { }

    /// <summary>Construct a handler with custom mappers.</summary>
    public LabelHandler(IPropertyMapper? mapper, CommandMapper? commandMapper = null)
        : base(mapper ?? Mapper, commandMapper ?? CommandMapper) { }

    /// <inheritdoc/>
    public override ComposableNode BuildNode(IComposer composer)
    {
        var virtualView = VirtualView;
        ArgumentNullException.ThrowIfNull(virtualView);

        // Subscribe to the shared view-properties version slot so any
        // ApplyViewProperties-relevant property change (Opacity,
        // Translation, Scale, Rotation, IsVisible, Clip, Shadow)
        // forces a recomposition through the IComposeHandler bumper.
        SubscribeToViewProperties();

        var packed = _color.Value;
        var size   = _fontSize.Value;
        var bold   = _bold.Value;
        var fill   = _fillWidth.Value;
        var align  = (TextAlignment)_hTextAlign.Value;
        var text = new ComposeText(_text.Value)
        {
            Color      = packed.HasValue ? new ComposeColor(packed.Value) : null,
            FontSize   = size.HasValue ? new Sp(size.Value) : null,
            FontWeight = bold ? ComposeFontWeight.Bold : null,
            Align      = align switch
            {
                TextAlignment.Center => ComposeTextAlign.Center,
                TextAlignment.End    => ComposeTextAlign.End,
                _                    => null,
            },
        };
        // Single PrependModifier call combining the layout-fill (if
        // applicable) with the cross-cutting view properties — calling
        // PrependModifier twice would replace, not merge, so this
        // builds the chain once.
        var outer = (fill ? Modifier.FillMaxWidth() : Modifier.Companion)
            .ApplyViewProperties(virtualView)
            .ApplyGestures(virtualView, MauiContext);
        text.PrependModifier(outer);
        return text;
    }

    /// <summary>Map <see cref="IText.Text"/> to the Compose text slot.</summary>
    public static void MapText(LabelHandler handler, ILabel label) =>
        handler._text.Value = label.Text ?? string.Empty;

    /// <summary>Map <see cref="ITextStyle.TextColor"/> to the Compose color slot.</summary>
    public static void MapTextColor(LabelHandler handler, ILabel label) =>
        handler._color.Value = ColorMapping.ToPackedLong(label.TextColor);

    /// <summary>Map <see cref="ITextStyle.Font"/> (size + weight) to Compose slots.</summary>
    /// <remarks>
    /// MAUI <c>Font</c> aggregates <c>Family</c>, <c>Size</c> (sp), and
    /// <c>FontAttributes</c>. Only size and bold are wired in Phase 1;
    /// custom font families and italic land in a later phase
    /// (see <c>.github/instructions/compose-maui.instructions.md</c>).
    /// </remarks>
    public static void MapFont(LabelHandler handler, ILabel label)
    {
        var font = label.Font;
        handler._fontSize.Value = font.Size > 0 ? (int)font.Size : null;
        handler._bold.Value     = (font.Weight & Microsoft.Maui.FontWeight.Bold) == Microsoft.Maui.FontWeight.Bold;
    }

    /// <summary>Map <see cref="ILabel.HorizontalTextAlignment"/> to Compose <c>textAlign</c>.</summary>
    public static void MapHorizontalTextAlignment(LabelHandler handler, ILabel label) =>
        handler._hTextAlign.Value = (int)label.HorizontalTextAlignment;

    /// <summary>
    /// Map <see cref="IView.HorizontalLayoutAlignment"/> to
    /// <c>Modifier.fillMaxWidth()</c> when the label asks to fill or
    /// center within its slot. Compose's <c>Text</c> only honours
    /// <c>textAlign</c> when its measured width spans the available
    /// space, so this is needed for the <c>Headline</c>/<c>SubHeadline</c>
    /// styles (which set <c>HorizontalOptions="Center"</c>) to render
    /// centered like stock MAUI.
    /// </summary>
    public static void MapHorizontalLayoutAlignment(LabelHandler handler, ILabel label) =>
        handler._fillWidth.Value = label.HorizontalLayoutAlignment
            is Microsoft.Maui.Primitives.LayoutAlignment.Fill
            or Microsoft.Maui.Primitives.LayoutAlignment.Center;

}
