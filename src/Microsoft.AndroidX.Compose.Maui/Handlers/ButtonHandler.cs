using AndroidX.Compose;
using AndroidX.Compose.Material3;
using AndroidX.Compose.Runtime;
using AndroidX.Compose.UI.Platform;
using Microsoft.AndroidX.Compose.Maui.Loaders;
using Microsoft.AndroidX.Compose.Maui.Platform;
using Microsoft.Maui.Handlers;
using ComposeButton  = AndroidX.Compose.Button;
using ComposeColor   = AndroidX.Compose.Color;
using ComposeImage   = AndroidX.Compose.Image;
using ComposeFontWeight = AndroidX.Compose.FontWeight;
using ComposeText    = AndroidX.Compose.Text;

namespace Microsoft.AndroidX.Compose.Maui.Handlers;

/// <summary>
/// MAUI <see cref="Microsoft.Maui.Controls.Button"/> handler that renders
/// through Jetpack Compose's Material 3 <c>Button</c> composable. Replaces
/// MAUI's stock <c>MaterialButton</c>-based handler when the consumer calls
/// <see cref="Hosting.AppHostBuilderExtensions.UseAndroidXCompose"/>.
/// </summary>
/// <remarks>
/// Folds into the page's single composition via
/// <see cref="ComposeElementHandler{TVirtualView}"/> /
/// <see cref="IComposeHandler"/>. The Compose <c>onClick</c> lambda
/// forwards to <see cref="IButton.Clicked"/> so MAUI's standard
/// <c>Clicked</c> event and bound <c>Command</c> fire as expected.
/// </remarks>
public partial class ButtonHandler : ComposeElementHandler<IButton>
{
    /// <summary>
    /// Property mapper that forwards MAUI <see cref="IButton"/> property
    /// changes to the Compose-backed <see cref="ComposeView"/>.
    /// </summary>
    /// <remarks>
    /// <para>Typed against this concrete handler (not <c>IButtonHandler</c>)
    /// because <see cref="PropertyMapper{TVirtualView, TViewHandler}"/> casts
    /// the handler arg of every mapper callback to <c>TViewHandler</c>, and
    /// this class doesn't implement the stock MAUI <c>IButtonHandler</c>
    /// interface.</para>
    ///
    /// <para><c>IView.Background</c> is mapped to Compose's
    /// <see cref="ButtonColors"/> <c>containerColor</c> slot instead of
    /// letting <see cref="ViewHandler.ViewMapper"/> paint a
    /// <see cref="SolidPaint"/> on the outer <see cref="ComposeView"/>:
    /// Material 3's <c>Button</c> paints its own pill, so a stock outer
    /// background produces a wide rectangle behind the pill. Only solid
    /// paints are supported; gradients / images fall back to the M3
    /// default container colour.</para>
    /// </remarks>
    public static IPropertyMapper<IButton, ButtonHandler> Mapper =
        new PropertyMapper<IButton, ButtonHandler>(ViewHandler.ViewMapper)
        {
            [nameof(IText.Text)]                      = MapText,
            [nameof(ITextStyle.TextColor)]            = MapTextColor,
            [nameof(ITextStyle.CharacterSpacing)]     = MapCharacterSpacing,
            [nameof(ITextStyle.Font)]                 = MapFont,
            [nameof(IButtonStroke.CornerRadius)]      = MapCornerRadius,
            [nameof(IButtonStroke.StrokeColor)]       = MapStrokeColor,
            [nameof(IButtonStroke.StrokeThickness)]   = MapStrokeThickness,
            [nameof(IImageSourcePart.Source)]         = MapImageSource,
            [nameof(IPadding.Padding)]                = MapPadding,
            [nameof(IView.Background)]                = MapBackground,
            [nameof(IView.HorizontalLayoutAlignment)] = MapHorizontalLayoutAlignment,
        };

    /// <summary>Command mapper (inherits view-level commands; no extras).</summary>
    public static CommandMapper<IButton, ButtonHandler> CommandMapper =
        new(ViewCommandMapper);

    readonly MutableState<string> _text           = new(string.Empty);
    readonly MutableState<long?>  _containerColor = new((long?)null);
    readonly MutableState<long?>  _contentColor   = new((long?)null);
    readonly MutableState<bool>   _fillWidth      = new(false);
    readonly MutableState<float?> _letterSpacing  = new((float?)null);
    readonly MutableState<int?>   _fontSize       = new((int?)null);
    readonly MutableState<bool>   _bold           = new(false);
    // CornerRadius in MAUI IButtonStroke is an int (DIPs).
    readonly MutableState<int>    _cornerRadius   = new(-1);
    readonly MutableState<long?>  _strokeColor    = new((long?)null);
    readonly MutableState<float>  _strokeThickness = new(0f);
    // Padding lives as a struct (Thickness); bump on change and live-read.
    readonly MutableState<int>    _paddingVersion = new(0);
    ImageSourceLoader? _loader;

    /// <summary>Construct a handler with the default mappers.</summary>
    public ButtonHandler() : base(Mapper, CommandMapper) { }

    /// <summary>Construct a handler with custom mappers.</summary>
    public ButtonHandler(IPropertyMapper? mapper, CommandMapper? commandMapper = null)
        : base(mapper ?? Mapper, commandMapper ?? CommandMapper) { }

    // Lazy — buttons without an ImageSource never allocate the loader.
    ImageSourceLoader Loader =>
        _loader ??= new ImageSourceLoader(
            this,
            () => (VirtualView as Microsoft.Maui.IImage) as IImageSourcePart);

    /// <inheritdoc/>
    public override ComposableNode BuildNode(IComposer composer)
    {
        var virtualView = VirtualView
            ?? throw new InvalidOperationException("VirtualView not set on ButtonHandler.");

        SubscribeToViewProperties();

        // Subscribe so padding map bumps re-run BuildNode.
        _ = _paddingVersion.Value;

        var container       = _containerColor.Value;
        var content         = _contentColor.Value;
        var letterSpacing   = _letterSpacing.Value;
        var fontSize        = _fontSize.Value;
        var bold            = _bold.Value;
        var cornerRadius    = _cornerRadius.Value;
        var strokeColor     = _strokeColor.Value;
        var strokeThickness = _strokeThickness.Value;
        var padding         = (virtualView as IPadding)?.Padding ?? Thickness.Zero;
        var hasCustomText   = letterSpacing.HasValue || fontSize.HasValue || bold;

        var button = new ComposeButton(onClick: OnClicked);
        // Optional leading image — only added when ImageSource resolved.
        if (_loader is { } loader)
        {
            if (loader.Painter.Value is { } painter)
                button.Add(new ComposeImage(painter));
            else if (loader.DrawableResourceId.Value is int id)
                button.Add(new ComposeImage(id));
        }
        var textNode = new ComposeText(_text.Value)
        {
            LetterSpacing = letterSpacing.HasValue ? new Sp(1) * letterSpacing.Value : null,
            FontSize      = fontSize.HasValue ? new Sp(fontSize.Value) : null,
            FontWeight    = bold ? ComposeFontWeight.Bold : null,
        };
        button.Add(textNode);

        if (container is not null || content is not null)
            button.Colors = composer.ButtonColors(
                containerColor: container,
                contentColor:   content);
        if (cornerRadius >= 0)
            button.Shape = new RoundedCornerShape(new Dp(cornerRadius));
        if (padding != Thickness.Zero)
            button.ContentPadding = new PaddingValues(
                start:  new Dp((float)padding.Left),
                top:    new Dp((float)padding.Top),
                end:    new Dp((float)padding.Right),
                bottom: new Dp((float)padding.Bottom));
        // Optional stroke chain — Compose Button has no built-in border slot.
        // Wrap the outer Modifier with Modifier.Border when a stroke is set.
        var outer = (_fillWidth.Value ? Modifier.FillMaxWidth() : Modifier.Companion);
        if (strokeColor.HasValue && strokeThickness > 0f)
            outer = outer.Border(
                new Dp(strokeThickness),
                new ComposeColor(strokeColor.Value),
                button.Shape);
        outer = outer
            .ApplyViewProperties(virtualView)
            .ApplyGestures(virtualView, MauiContext)
            .ApplySemantics(virtualView);
        button.PrependModifier(outer);
        return button;
    }

    /// <inheritdoc/>
    protected override void DisconnectHandler(ComposeView platformView)
    {
        _loader?.Reset();
        base.DisconnectHandler(platformView);
    }

    void OnClicked()
    {
        // Stock MAUI ButtonHandler raises Pressed → Clicked → Released in
        // touch-down/-up order; Compose only surfaces a logical click here.
        // Fire all three so any caller subscribing to Pressed/Released
        // (e.g. behaviors, gesture recognizers) still observes them.
        var virtualView = VirtualView;
        if (virtualView is null) return;
        virtualView.Pressed();
        virtualView.Clicked();
        virtualView.Released();
    }

    /// <summary>
    /// Map <see cref="ITextStyle.TextColor"/> to the Compose <c>ButtonColors</c>
    /// <c>contentColor</c> slot. Necessary in addition to
    /// <see cref="MapBackground"/> because M3's
    /// <c>contentColorFor(arbitraryColor)</c> returns
    /// <c>Color.Unspecified</c> when the supplied container colour
    /// isn't one of the theme's tokens — so a Compose <c>Text</c>
    /// inside the button reads transparent and disappears against the
    /// MAUI-supplied background.
    /// </summary>
    public static void MapTextColor(ButtonHandler handler, IButton button)
    {
        if (button is ITextStyle textStyle)
            handler._contentColor.Value = ColorMapping.ToPackedLong(textStyle.TextColor);
    }

    /// <summary>Map <see cref="IText.Text"/> to the Compose text slot.</summary>
    public static void MapText(ButtonHandler handler, IButton button)
    {
        if (button is IText text)
            handler._text.Value = text.Text ?? string.Empty;
    }

    /// <summary>
    /// Map <see cref="IView.Background"/> to the Compose <c>ButtonColors</c>
    /// <c>containerColor</c> slot when the paint is a
    /// <see cref="SolidPaint"/>. Anything else (gradient, image,
    /// <see langword="null"/>) leaves the slot unset so M3's theme
    /// default applies.
    /// </summary>
    public static void MapBackground(ButtonHandler handler, IButton button) =>
        handler._containerColor.Value = button.Background is SolidPaint solid
            ? ColorMapping.ToPackedLong(solid.Color)
            : null;

    /// <summary>
    /// Map <see cref="IView.HorizontalLayoutAlignment"/> to
    /// <c>Modifier.fillMaxWidth()</c> when the button asks to fill its
    /// slot. Compose Material 3 <c>Button</c> hugs its content by
    /// default, so without this a button with <c>HorizontalOptions="Fill"</c>
    /// would render as a small pill on the left edge.
    /// </summary>
    /// <remarks>
    /// Suppressed when the parent is a
    /// <see cref="Microsoft.Maui.Controls.HorizontalStackLayout"/>:
    /// MAUI's stock <c>HorizontalStackLayoutManager</c> arranges
    /// children left-to-right at their measured width and ignores
    /// <c>HorizontalOptions=Fill</c> on the main axis (Fill there
    /// only stretches the cross-axis). Honouring it on the Compose
    /// side would make the first child's <c>FillMaxWidth</c> consume
    /// the entire row and squeeze every sibling to zero width — see
    /// the toggle row on <c>ProgressPage</c>.
    /// </remarks>
    public static void MapHorizontalLayoutAlignment(ButtonHandler handler, IButton button)
    {
        var fill = button.HorizontalLayoutAlignment
            == Microsoft.Maui.Primitives.LayoutAlignment.Fill;
        if (fill && button is IView view
                 && view.Parent is Microsoft.Maui.Controls.HorizontalStackLayout)
        {
            fill = false;
        }
        handler._fillWidth.Value = fill;
    }

    /// <summary>
    /// Map <see cref="ITextStyle.CharacterSpacing"/> to the Compose
    /// <c>Text.LetterSpacing</c> slot on the button's inner label.
    /// </summary>
    public static void MapCharacterSpacing(ButtonHandler handler, IButton button)
    {
        if (button is ITextStyle ts)
            handler._letterSpacing.Value = ts.CharacterSpacing != 0
                ? (float)ts.CharacterSpacing
                : null;
    }

    /// <summary>
    /// Map <see cref="ITextStyle.Font"/> (size + bold) to the Compose
    /// <c>Text.FontSize</c> and <c>Text.FontWeight</c> slots.
    /// Custom font families and italic land in a later phase.
    /// </summary>
    public static void MapFont(ButtonHandler handler, IButton button)
    {
        if (button is not ITextStyle ts) return;
        handler._fontSize.Value = ts.Font.Size > 0 ? (int)ts.Font.Size : null;
        handler._bold.Value     = (ts.Font.Weight & Microsoft.Maui.FontWeight.Bold)
            == Microsoft.Maui.FontWeight.Bold;
    }

    /// <summary>
    /// Map <see cref="IButtonStroke.CornerRadius"/> to the Compose
    /// <c>Button.Shape</c> slot via <see cref="RoundedCornerShape"/>.
    /// </summary>
    public static void MapCornerRadius(ButtonHandler handler, IButton button)
    {
        if (button is IButtonStroke stroke)
            handler._cornerRadius.Value = stroke.CornerRadius;
    }

    /// <summary>
    /// Map <see cref="IButtonStroke.StrokeColor"/> to the outer
    /// <c>Modifier.Border</c> chain. Compose's Material 3 <c>Button</c>
    /// has no built-in border slot, so we draw the stroke around the
    /// button frame instead.
    /// </summary>
    public static void MapStrokeColor(ButtonHandler handler, IButton button)
    {
        if (button is IButtonStroke stroke)
            handler._strokeColor.Value = ColorMapping.ToPackedLong(stroke.StrokeColor);
    }

    /// <summary>
    /// Map <see cref="IButtonStroke.StrokeThickness"/> to the outer
    /// <c>Modifier.Border</c> chain. See <see cref="MapStrokeColor"/>.
    /// </summary>
    public static void MapStrokeThickness(ButtonHandler handler, IButton button)
    {
        if (button is IButtonStroke stroke)
            handler._strokeThickness.Value = (float)stroke.StrokeThickness;
    }

    /// <summary>
    /// Map <see cref="IPadding.Padding"/> to the Compose
    /// <c>Button.ContentPadding</c> slot. Live-read in <see cref="BuildNode"/>.
    /// </summary>
    public static void MapPadding(ButtonHandler handler, IButton button) =>
        handler._paddingVersion.Value = handler._paddingVersion.Value + 1;

    /// <summary>
    /// Map <see cref="IImage.Source"/> through the shared
    /// <see cref="ImageSourceLoader"/>; the resolved drawable or painter
    /// is rendered inline as a leading icon inside the button row.
    /// </summary>
    public static async void MapImageSource(ButtonHandler handler, IButton button) =>
        await handler.Loader.LoadAsync((button as Microsoft.Maui.IImage)?.Source).ConfigureAwait(false);
}
