using AndroidX.Compose;
using AndroidX.Compose.Runtime;
using AndroidX.Compose.UI.Platform;
using Microsoft.AndroidX.Compose.Maui.Loaders;
using Microsoft.AndroidX.Compose.Maui.Platform;
using Microsoft.Maui.Handlers;
using ComposeColor      = AndroidX.Compose.Color;
using ComposeIconButton = AndroidX.Compose.IconButton;
using ComposeImage      = AndroidX.Compose.Image;
using MauiIImageButton  = Microsoft.Maui.IImageButton;

namespace Microsoft.AndroidX.Compose.Maui.Handlers;

/// <summary>
/// MAUI <see cref="Microsoft.Maui.Controls.ImageButton"/> handler that
/// renders an <c>IconButton</c> wrapping a Compose <c>Image(Painter)</c>.
/// Folds into the page's single composition via
/// <see cref="ComposeElementHandler{TVirtualView}"/> /
/// <see cref="IComposeHandler"/>.
/// </summary>
/// <remarks>
/// <para>The image-source pipeline is shared with
/// <see cref="ImageHandler"/> via <see cref="ImageSourceLoader"/> —
/// drawable-id fast path for packaged resources, full
/// <see cref="ImageSourcePartLoader"/> general path for
/// URI / stream / font / non-drawable file sources.</para>
///
/// <para>On tap the handler fires the canonical MAUI
/// <c>Pressed → Clicked → Released</c> sequence on
/// <see cref="MauiIImageButton"/> (mirrors stock
/// <see cref="ButtonHandler"/>). Compose has no separate "pressed"
/// state for IconButton tap; the synthesised events keep MAUI
/// commands / behaviours that rely on the sequence functional.</para>
///
/// <para>Border + corner radius compose into a single
/// <c>Modifier.Border(width, color, RoundedCornerShape).Clip(...)</c>
/// chain so the rounded clip both shapes the button outline and clips
/// the inner image.</para>
/// </remarks>
public partial class ImageButtonHandler : ComposeElementHandler<MauiIImageButton>
{
    /// <summary>
    /// Property mapper that forwards MAUI <see cref="MauiIImageButton"/>
    /// changes to the Compose-backed <see cref="ComposeView"/>.
    /// </summary>
    public static IPropertyMapper<MauiIImageButton, ImageButtonHandler> Mapper =
        new PropertyMapper<MauiIImageButton, ImageButtonHandler>(ViewHandler.ViewMapper)
        {
            [nameof(IImageSourcePart.Source)]            = MapSource,
            [nameof(Microsoft.Maui.IImage.Aspect)]       = MapAspect,
            [nameof(IButtonStroke.StrokeColor)]          = MapStrokeColor,
            [nameof(IButtonStroke.StrokeThickness)]      = MapStrokeThickness,
            [nameof(IButtonStroke.CornerRadius)]         = MapCornerRadius,
            [nameof(IPadding.Padding)]                   = MapPadding,
        };

    /// <summary>Command mapper (inherits view-level commands; no extras).</summary>
    public static CommandMapper<MauiIImageButton, ImageButtonHandler> CommandMapper =
        new(ViewCommandMapper);

    readonly MutableState<ContentScale> _contentScale = new(ContentScale.Fit);
    readonly MutableState<long?>        _strokeColor  = new((long?)null);
    readonly MutableState<float>        _strokeWidth  = new(0f);
    // CornerRadius comes from IButtonStroke as int (corner radius in
    // device-independent pixels). 0 disables the rounded clip.
    readonly MutableState<int>          _cornerRadius = new(0);
    // Thickness is a struct, so use the version-counter pattern.
    readonly MutableState<int>          _paddingVersion = new(0);
    ImageSourceLoader? _loader;

    /// <summary>Construct a handler with the default mappers.</summary>
    public ImageButtonHandler() : base(Mapper, CommandMapper) { }

    /// <summary>Construct a handler with custom mappers.</summary>
    public ImageButtonHandler(IPropertyMapper? mapper, CommandMapper? commandMapper = null)
        : base(mapper ?? Mapper, commandMapper ?? CommandMapper) { }

    ImageSourceLoader Loader =>
        _loader ??= new ImageSourceLoader(
            this,
            () => VirtualView as IImageSourcePart);

    /// <inheritdoc/>
    public override ComposableNode BuildNode(IComposer composer)
    {
        var virtualView = VirtualView;
        ArgumentNullException.ThrowIfNull(virtualView);

        _ = _paddingVersion.Value;  // subscribe — Padding change bumps this
        var padding = virtualView is IPadding p ? p.Padding : Thickness.Zero;
        var stroke  = _strokeColor.Value;
        var width   = _strokeWidth.Value;
        var corner  = _cornerRadius.Value;

        // Inner image — Painter wins over the resource-id fast path so
        // a freshly-loaded URI immediately replaces a stale drawable.
        ComposableNode imageNode;
        if (_loader is { } loader)
        {
            if (loader.Painter.Value is { } painter)
                imageNode = new ComposeImage(painter) { ContentScale = _contentScale.Value };
            else if (loader.DrawableResourceId.Value is int id)
                imageNode = new ComposeImage(id) { ContentScale = _contentScale.Value };
            else
                imageNode = new Box();
        }
        else
        {
            imageNode = new Box();
        }

        Modifier? modifier = null;
        if (corner > 0)
            modifier = (modifier ?? Modifier.Companion).Clip(new Dp(corner));
        if (stroke.HasValue && width > 0f)
        {
            var color = new ComposeColor(stroke.Value);
            modifier = corner > 0
                ? (modifier ?? Modifier.Companion).Border(new Dp(width), color, new Dp(corner))
                : (modifier ?? Modifier.Companion).Border(new Dp(width), color);
        }
        if (padding != Thickness.Zero)
        {
            modifier = (modifier ?? Modifier.Companion).Padding(
                start:  new Dp((float)padding.Left),
                top:    new Dp((float)padding.Top),
                end:    new Dp((float)padding.Right),
                bottom: new Dp((float)padding.Bottom));
        }

        var button = new ComposeIconButton(OnClicked) { imageNode };
        modifier = (modifier ?? Modifier.Companion).ApplyGestures(virtualView, MauiContext);
        button.Modifier = modifier;
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
        // Mirror ButtonHandler — fire the MAUI Pressed/Clicked/Released
        // sequence so commands / behaviours that hook those events
        // continue to work.
        if (VirtualView is { } imgBtn)
        {
            imgBtn.Pressed();
            imgBtn.Clicked();
            imgBtn.Released();
        }
    }

    /// <summary>
    /// Map <see cref="IImageSourcePart.Source"/> through the shared
    /// <see cref="ImageSourceLoader"/>.
    /// </summary>
    /// <remarks>
    /// <c>async void</c> deliberately — see
    /// <see cref="ImageHandler.MapSource"/>.
    /// </remarks>
    public static async void MapSource(ImageButtonHandler handler, MauiIImageButton image) =>
        await handler.Loader.LoadAsync(image.Source).ConfigureAwait(false);

    /// <summary>Map <see cref="Microsoft.Maui.IImage.Aspect"/> to a Compose
    /// <see cref="ContentScale"/>.</summary>
    public static void MapAspect(ImageButtonHandler handler, MauiIImageButton image) =>
        handler._contentScale.Value = image.Aspect switch
        {
            Aspect.AspectFill => ContentScale.Crop,
            Aspect.Fill       => ContentScale.FillBounds,
            _                 => ContentScale.Fit,
        };

    /// <summary>Map <see cref="IButtonStroke.StrokeColor"/>.</summary>
    public static void MapStrokeColor(ImageButtonHandler handler, MauiIImageButton image) =>
        handler._strokeColor.Value = ColorMapping.ToPackedLong(image.StrokeColor);

    /// <summary>Map <see cref="IButtonStroke.StrokeThickness"/>.</summary>
    public static void MapStrokeThickness(ImageButtonHandler handler, MauiIImageButton image) =>
        handler._strokeWidth.Value = (float)image.StrokeThickness;

    /// <summary>Map <see cref="IButtonStroke.CornerRadius"/>.</summary>
    public static void MapCornerRadius(ImageButtonHandler handler, MauiIImageButton image) =>
        handler._cornerRadius.Value = image.CornerRadius;

    /// <summary>Bump the padding version slot.</summary>
    public static void MapPadding(ImageButtonHandler handler, MauiIImageButton _) =>
        handler._paddingVersion.Value++;
}
