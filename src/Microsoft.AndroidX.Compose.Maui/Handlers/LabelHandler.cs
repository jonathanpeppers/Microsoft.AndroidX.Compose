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
            [nameof(IText.Text)]                      = MapText,
            [nameof(ITextStyle.TextColor)]            = MapTextColor,
            [nameof(ITextStyle.CharacterSpacing)]     = MapCharacterSpacing,
            [nameof(ITextStyle.Font)]                 = MapFont,
            [nameof(ILabel.HorizontalTextAlignment)]  = MapHorizontalTextAlignment,
            [nameof(ILabel.VerticalTextAlignment)]    = MapVerticalTextAlignment,
            [nameof(ILabel.LineHeight)]               = MapLineHeight,
            [nameof(ILabel.TextDecorations)]          = MapTextDecorations,
            [nameof(IPadding.Padding)]                = MapPadding,
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
    readonly MutableState<int>         _vTextAlign = new((int)TextAlignment.Start);
    readonly MutableState<bool>        _fillWidth = new(false);
    // CharacterSpacing in MAUI is "em"-ish (0..1 typically). Packed via the
    // Sp(1) * float overload because Sp has no (float) ctor.
    readonly MutableState<float?>      _letterSpacing = new((float?)null);
    // LineHeight in MAUI is a multiplier on the default line height; we
    // expose it as an int sp here for simplicity — null when not set.
    readonly MutableState<int?>        _lineHeight = new((int?)null);
    // TextDecorations enum stored as int (Flags: None=0, Underline=1,
    // Strikethrough=2). MutableState's generic boxed path doesn't recognise
    // [Flags] enums.
    readonly MutableState<int>         _decorations = new(0);
    // Padding live-read in BuildNode; this version slot just bumps to force
    // a recomposition when MAUI invokes the mapper.
    readonly MutableState<int>         _paddingVersion = new(0);

    /// <summary>Construct a handler with the default mappers.</summary>
    public LabelHandler() : base(Mapper, CommandMapper) { }

    /// <summary>Construct a handler with custom mappers.</summary>
    public LabelHandler(IPropertyMapper? mapper, CommandMapper? commandMapper = null)
        : base(mapper ?? Mapper, commandMapper ?? CommandMapper) { }

    /// <inheritdoc/>
    public override ComposableNode BuildNode(IComposer composer)
    {
        var virtualView = VirtualView
            ?? throw new InvalidOperationException("VirtualView not set on LabelHandler.");

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
        var vAlign = (TextAlignment)_vTextAlign.Value;
        var letterSpacing = _letterSpacing.Value;
        var lineHeight    = _lineHeight.Value;
        var decorations   = (Microsoft.Maui.TextDecorations)_decorations.Value;
        // Subscribe so padding mapper bumps re-run BuildNode.
        _ = _paddingVersion.Value;
        var padding = virtualView.Padding;

        // Resolve the text color. Three paths:
        //
        //  1. MAUI's `TextColor` is set → use it verbatim (the most
        //     common case; users who care set this explicitly).
        //  2. `TextColor` is null AND we know the app's active theme
        //     via the inherited <see cref="Theme"/> cache → pick
        //     `White` on dark / `Black` on light. This mirrors what
        //     MAUI's stock `LabelHandler` does (it reads
        //     `Resources.GetColorStateList` for the active
        //     configuration) and — critically — fixes #248: when our
        //     `LabelHandler` runs as a Compose leaf inside a stock
        //     host (e.g. `Shell`'s built-in `FlyoutItem` template),
        //     there's no enclosing `MaterialTheme` / `Surface` to set
        //     `LocalContentColor`. Without an explicit color,
        //     Compose's `Text` would fall through to
        //     `LocalContentColor.current` which defaults to
        //     `Color.Black` — black-on-dark in dark mode, invisible.
        //  3. No `ThemeManager` registered (consumer skipped
        //     `UseAndroidXCompose`) → fall through with `null` so
        //     `Text` keeps its pre-existing inherited-from-
        //     `LocalContentColor` behaviour.
        //
        // Reading `Theme.IsDark.Value` inside the composable scope
        // registers a snapshot read, so flipping the MAUI theme at
        // runtime recomposes the label against the new fallback.
        ComposeColor? color;
        if (packed.HasValue)
        {
            color = new ComposeColor(packed.Value);
        }
        else
        {
            color = Theme is null
                ? null
                : Theme.IsDark.Value ? ComposeColor.White : ComposeColor.Black;
        }

        var text = new ComposeText(_text.Value)
        {
            Color      = color,
            FontSize   = size.HasValue ? new Sp(size.Value) : null,
            FontWeight = bold ? ComposeFontWeight.Bold : null,
            Align      = align switch
            {
                TextAlignment.Center => ComposeTextAlign.Center,
                TextAlignment.End    => ComposeTextAlign.End,
                _                    => null,
            },
            LetterSpacing = letterSpacing.HasValue ? new Sp(1) * letterSpacing.Value : null,
            LineHeight    = lineHeight.HasValue ? new Sp(lineHeight.Value) : null,
            // Strikethrough takes precedence over Underline when both bits
            // are set; combining the two would need TextDecoration.Combine
            // which isn't bound yet.
            // TODO: expose TextDecoration.Combine to handle the
            // Underline | Strikethrough combination faithfully.
            Decoration = decorations switch
            {
                Microsoft.Maui.TextDecorations.Strikethrough => TextDecoration.LineThrough,
                Microsoft.Maui.TextDecorations.Underline     => TextDecoration.Underline,
                _ when (decorations & Microsoft.Maui.TextDecorations.Strikethrough) != 0
                    => TextDecoration.LineThrough,
                _ when (decorations & Microsoft.Maui.TextDecorations.Underline) != 0
                    => TextDecoration.Underline,
                _ => null,
            },
        };
        // Single PrependModifier call combining the layout-fill (if
        // applicable) with the cross-cutting view properties — calling
        // PrependModifier twice would replace, not merge, so this
        // builds the chain once. View properties are applied on the
        // outermost modifier (per ModifierBridge convention) so
        // background/shadow/opacity cover the entire label rectangle;
        // padding is innermost so it only shrinks the content area.
        var outer = (fill ? Modifier.FillMaxWidth() : Modifier.Companion)
            .ApplyViewProperties(virtualView)
            .ApplyGestures(virtualView, MauiContext)
            .ApplySemantics(virtualView)
            .ApplyVerticalTextAlignment(vAlign)
            .Padding(
                new Dp((float)padding.Left),
                new Dp((float)padding.Top),
                new Dp((float)padding.Right),
                new Dp((float)padding.Bottom));
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
    /// Map <see cref="ILabel.VerticalTextAlignment"/> to a
    /// <c>Modifier.wrapContentHeight(Alignment.Vertical)</c> on the
    /// outer modifier, so the text top/center/bottom-aligns inside the
    /// label's allocated height. Visible only when the label has an
    /// explicit <c>HeightRequest</c> (or fills its parent vertically) —
    /// matches the stock MAUI behaviour.
    /// </summary>
    public static void MapVerticalTextAlignment(LabelHandler handler, ILabel label) =>
        handler._vTextAlign.Value = (int)label.VerticalTextAlignment;

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

    /// <summary>
    /// Map <see cref="ITextStyle.CharacterSpacing"/> to Compose
    /// <c>letterSpacing</c>. MAUI's value is in "em-ish" units; we
    /// pack it via the <see cref="Sp"/> <c>* float</c> overload because
    /// <see cref="Sp"/> has no <c>(float)</c> constructor.
    /// </summary>
    public static void MapCharacterSpacing(LabelHandler handler, ILabel label) =>
        handler._letterSpacing.Value = label.CharacterSpacing != 0
            ? (float)label.CharacterSpacing
            : null;

    /// <summary>Map <see cref="ILabel.LineHeight"/> to Compose <c>lineHeight</c>.</summary>
    /// <remarks>
    /// MAUI's <c>LineHeight</c> is a multiplier on the platform default
    /// line height (-1 = use default). Compose expects an absolute sp.
    /// We approximate by multiplying against the current font size when
    /// known; otherwise leave the slot unset.
    /// </remarks>
    public static void MapLineHeight(LabelHandler handler, ILabel label)
    {
        var lh = label.LineHeight;
        if (lh <= 0)
        {
            handler._lineHeight.Value = null;
            return;
        }
        var size = label.Font.Size > 0 ? label.Font.Size : 14d;
        handler._lineHeight.Value = (int)Math.Round(size * lh);
    }

    /// <summary>
    /// Map <see cref="ILabel.TextDecorations"/> to Compose
    /// <c>TextDecoration</c>. Only single-flag values render correctly;
    /// the combined <c>Underline | Strikethrough</c> case falls back to
    /// <c>Strikethrough</c> (see <see cref="BuildNode"/>).
    /// </summary>
    public static void MapTextDecorations(LabelHandler handler, ILabel label) =>
        handler._decorations.Value = (int)label.TextDecorations;

    /// <summary>Map <see cref="IPadding.Padding"/>. Live-read in <see cref="BuildNode"/>.</summary>
    public static void MapPadding(LabelHandler handler, ILabel label) =>
        handler._paddingVersion.Value = handler._paddingVersion.Value + 1;

}
