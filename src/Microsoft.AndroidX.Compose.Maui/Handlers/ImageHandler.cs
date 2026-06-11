using AndroidX.Compose;
using AndroidX.Compose.Runtime;
using AndroidX.Compose.UI.Platform;
using Microsoft.AndroidX.Compose.Maui.Loaders;
using Microsoft.AndroidX.Compose.Maui.Platform;
using Microsoft.Maui.Handlers;
using ComposeImage = AndroidX.Compose.Image;
using MauiIImage   = Microsoft.Maui.IImage;

namespace Microsoft.AndroidX.Compose.Maui.Handlers;

/// <summary>
/// MAUI <see cref="Microsoft.Maui.Controls.Image"/> handler that renders
/// through Jetpack Compose's <c>Image</c> composable
/// (<see cref="ComposeImage"/>). Replaces MAUI's stock
/// <c>AppCompatImageView</c>-based handler when the consumer calls
/// <see cref="Hosting.AppHostBuilderExtensions.UseAndroidXCompose"/>.
/// Folds into the page's single composition via
/// <see cref="ComposeElementHandler{TVirtualView}"/> /
/// <see cref="IComposeHandler"/>.
/// </summary>
/// <remarks>
/// <para>Source resolution is hybrid via
/// <see cref="ImageSourceLoader"/> — the same helper backing
/// <see cref="ImageButtonHandler"/>:</para>
/// <list type="bullet">
///   <item>
///     <description>
///       File sources whose name resolves to a packaged Android
///       drawable go through Compose's <c>painterResource(int)</c>
///       directly. Vector drawables + per-density buckets are
///       preserved.
///     </description>
///   </item>
///   <item>
///     <description>
///       Every other shape — URI / stream / font sources, plus files
///       that aren't packaged as drawables — routes through MAUI's
///       <see cref="ImageSourcePartLoader"/> and is wrapped as a
///       Compose <c>BitmapPainter</c>.
///     </description>
///   </item>
/// </list>
/// </remarks>
public partial class ImageHandler : ComposeElementHandler<MauiIImage>
{
    /// <summary>
    /// Property mapper that forwards MAUI <see cref="MauiIImage"/> property
    /// changes to the Compose-backed <see cref="ComposeView"/>.
    /// </summary>
    public static IPropertyMapper<MauiIImage, ImageHandler> Mapper =
        new PropertyMapper<MauiIImage, ImageHandler>(ViewHandler.ViewMapper)
        {
            [nameof(IImageSourcePart.Source)] = MapSource,
            [nameof(MauiIImage.Aspect)]       = MapAspect,
        };

    /// <summary>Command mapper (inherits view-level commands; no extras).</summary>
    public static CommandMapper<MauiIImage, ImageHandler> CommandMapper =
        new(ViewCommandMapper);

    readonly MutableState<ContentScale> _contentScale = new(ContentScale.Fit);
    ImageSourceLoader? _loader;

    /// <summary>Construct a handler with the default mappers.</summary>
    public ImageHandler() : base(Mapper, CommandMapper) { }

    /// <summary>Construct a handler with custom mappers.</summary>
    public ImageHandler(IPropertyMapper? mapper, CommandMapper? commandMapper = null)
        : base(mapper ?? Mapper, commandMapper ?? CommandMapper) { }

    // Lazy — handlers without a Source set never allocate the loader.
    ImageSourceLoader Loader =>
        _loader ??= new ImageSourceLoader(
            this,
            () => VirtualView as IImageSourcePart);

    // Without a sizing modifier, Compose's `Image` measures itself
    // against the painter's intrinsic size and `ContentScale` has no
    // visible effect (the layout box exactly matches the scaled
    // painter, so Fit / Crop / FillBounds all produce the same
    // result). `Modifier.fillMaxSize()` makes the Image honour the
    // layout box MAUI sized for the platform view — which is what
    // `HeightRequest` / `WidthRequest` on the `<Image>` virtual view
    // feed into. The `Modifier` chain itself is an immutable POCO
    // (just an op array); we cache one instance and share it across
    // every handler / Render pass.
    static readonly Modifier s_fillMaxSize = Modifier.FillMaxSize();

    /// <inheritdoc/>
    public override ComposableNode BuildNode(IComposer composer)
    {
        SubscribeToViewProperties();

        // Apply cross-cutting view properties (Opacity / Translation /
        // Scale / Rotation / IsVisible / Clip / Shadow) on top of the
        // FillMaxSize sizing modifier so the Image honours the layout
        // box MAUI sized for it. The placeholder Box gets the modifier
        // too so an Image with Opacity=0 stays invisible even before
        // the source loads.
        var modifier = s_fillMaxSize.ApplyViewProperties(VirtualView!);
        var cs = _contentScale.Value;

        // Painter wins over the resource id so a freshly-loaded
        // BitmapPainter immediately replaces any stale fast-path
        // drawable. Both null => empty placeholder.
        if (_loader is { } loader)
        {
            if (loader.Painter.Value is { } painter)
                return new ComposeImage(painter) { ContentScale = cs, Modifier = modifier };
            if (loader.DrawableResourceId.Value is int id)
                return new ComposeImage(id) { ContentScale = cs, Modifier = modifier };
        }
        var placeholder = new Box();
        placeholder.PrependModifier(modifier);
        return placeholder;
    }

    /// <inheritdoc/>
    protected override void DisconnectHandler(ComposeView platformView)
    {
        _loader?.Reset();
        base.DisconnectHandler(platformView);
    }

    /// <summary>
    /// Map <see cref="IImageSourcePart.Source"/> through the shared
    /// <see cref="ImageSourceLoader"/>.
    /// </summary>
    /// <remarks>
    /// Declared <c>async void</c> deliberately — <see cref="PropertyMapper"/>
    /// stores mapper delegates as <see cref="Action{T1, T2}"/> and
    /// invokes them synchronously without awaiting (mirrors stock
    /// MAUI handlers' <c>FireAndForget</c> trick).
    /// </remarks>
    public static async void MapSource(ImageHandler handler, MauiIImage image) =>
        await handler.Loader.LoadAsync(image.Source).ConfigureAwait(false);

    /// <summary>
    /// Map <see cref="MauiIImage.Aspect"/> to a Compose
    /// <see cref="ContentScale"/>.
    /// </summary>
    public static void MapAspect(ImageHandler handler, MauiIImage image) =>
        handler._contentScale.Value = image.Aspect switch
        {
            Aspect.AspectFill => ContentScale.Crop,
            Aspect.Fill       => ContentScale.FillBounds,
            // AspectFit + Center both fall through to Fit — Compose's
            // Fit already preserves aspect ratio and centers the
            // result inside the layout slot.
            _                 => ContentScale.Fit,
        };
}
