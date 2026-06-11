using Android.Graphics.Drawables;
using AndroidX.Compose;
using AndroidX.Compose.Runtime;
using AndroidX.Compose.UI.Graphics;
using AndroidX.Compose.UI.Graphics.Painter;
using AndroidX.Compose.UI.Platform;
using AndroidX.Core.Graphics.Drawable;
using Microsoft.AndroidX.Compose.Maui.Platform;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using ComposeImage = AndroidX.Compose.Image;
using ComposePainter = AndroidX.Compose.UI.Graphics.Painter.Painter;
using MauiIImage = Microsoft.Maui.IImage;

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
/// <para>Source resolution is hybrid:</para>
/// <list type="bullet">
///   <item>
///     <description>
///       An <see cref="IFileImageSource"/> whose file name resolves to a
///       packaged Android drawable (the common case for files under
///       <c>Resources/Images/</c>) goes through Compose's
///       <c>painterResource(int)</c> directly. This preserves vector
///       drawables and per-density buckets, and skips the
///       <c>Drawable</c> → <c>Bitmap</c> rasterization the general
///       path performs.
///     </description>
///   </item>
///   <item>
///     <description>
///       Every other shape — <see cref="IUriImageSource"/>,
///       <see cref="IStreamImageSource"/>, <see cref="IFontImageSource"/>,
///       plus files that aren't packaged as drawable resources — routes
///       through MAUI's <see cref="ImageSourcePartLoader"/> +
///       <see cref="IImageSourceServiceProvider"/> pipeline. The
///       <see cref="Drawable"/> the pipeline produces is wrapped as a
///       Compose <c>BitmapPainter</c> and rendered via the
///       <c>Image(Painter)</c> ctor on <see cref="ComposeImage"/>.
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

    // Fast-path slot: file source resolved to an Android drawable id.
    // Compose's painterResource(id) handles density buckets and vector
    // drawables natively; no Drawable->Bitmap round trip needed.
    readonly MutableState<int?> _drawableResourceId = new((int?)null);

    // General-path slot: drawable produced by MAUI's
    // IImageSourceService<TSource> pipeline, wrapped as a Compose
    // BitmapPainter. The two slots are independent so a slow URI load
    // can complete without disturbing a prior fast-path render, and
    // vice versa.
    readonly MutableState<ComposePainter?> _painter = new((ComposePainter?)null);

    readonly MutableState<ContentScale> _contentScale = new(ContentScale.Fit);

    // Lazy: handlers that only ever see file sources don't allocate the
    // loader or its setter at all.
    ImageSourcePartLoader? _loader;
    ImageSourcePartLoader Loader =>
        _loader ??= new ImageSourcePartLoader(new ComposeImageSetter(this));

    /// <summary>Construct a handler with the default mappers.</summary>
    public ImageHandler() : base(Mapper, CommandMapper) { }

    /// <summary>Construct a handler with custom mappers.</summary>
    public ImageHandler(IPropertyMapper? mapper, CommandMapper? commandMapper = null)
        : base(mapper ?? Mapper, commandMapper ?? CommandMapper) { }

    /// <inheritdoc/>
    public override ComposableNode BuildNode(IComposer composer)
    {
        SubscribeToViewProperties();

        // Painter wins over the resource id so a freshly-loaded
        // BitmapPainter immediately replaces any stale fast-path
        // drawable. Both null => empty placeholder.
        ComposableNode node;
        if (_painter.Value is { } painter)
            node = new ComposeImage(painter) { ContentScale = _contentScale.Value };
        else if (_drawableResourceId.Value is int id)
            node = new ComposeImage(id) { ContentScale = _contentScale.Value };
        else
            node = new Box();

        // Apply cross-cutting view properties (Opacity / Translation /
        // Scale / Rotation / IsVisible / Clip / Shadow). The placeholder
        // Box gets the modifier too, so an Image with Opacity=0 stays
        // invisible even before the source loads.
        node.PrependModifier(Modifier.Companion.ApplyViewProperties(VirtualView!));
        return node;
    }

    /// <inheritdoc/>
    protected override void DisconnectHandler(ComposeView platformView)
    {
        // Reset() cancels any pending CancellationToken inside the
        // loader so a stale continuation can't write into our state
        // slots after disconnect.
        _loader?.Reset();
        _drawableResourceId.Value = null;
        _painter.Value = null;
        base.DisconnectHandler(platformView);
    }

    /// <summary>
    /// Map <see cref="IImageSourcePart.Source"/> to either a resolved
    /// Android drawable resource id (fast path) or a Compose
    /// <c>BitmapPainter</c> built from the result of MAUI's
    /// <see cref="IImageSourceService{TSource}"/> pipeline (general
    /// path).
    /// </summary>
    /// <remarks>
    /// Declared <c>async void</c> deliberately. <see cref="PropertyMapper"/>
    /// stores mapper delegates as <see cref="Action{T1, T2}"/> and
    /// invokes them synchronously without awaiting, so the
    /// fire-and-forget shape matches what MAUI's stock handlers do via
    /// the internal <c>TaskExtensions.FireAndForget</c> helper.
    /// </remarks>
    public static async void MapSource(ImageHandler handler, MauiIImage image)
    {
        var src = image.Source;

        // Empty / cleared source -> drop both slots and cancel any
        // in-flight load so the next paint goes blank.
        if (src is null || src.IsEmpty)
        {
            handler._loader?.Reset();
            handler._drawableResourceId.Value = null;
            handler._painter.Value = null;
            return;
        }

        // Fast path: a FileImageSource that resolves to a packaged
        // drawable. Context.GetDrawableId mirrors MAUI's
        // FileImageSourceService — lower-cases the file name and asks
        // Resources.GetIdentifier("name", "drawable", PackageName).
        if (src is IFileImageSource file &&
            handler.Context.GetDrawableId(file.File ?? string.Empty) is var id && id > 0)
        {
            handler._loader?.Reset();
            handler._painter.Value = null;
            handler._drawableResourceId.Value = id;
            return;
        }

        // General path: hand the source off to MAUI's pipeline.
        // FileImageSourceService (non-drawable file paths),
        // UriImageSourceService, StreamImageSourceService and
        // FontImageSourceService all materialize the source into a
        // Drawable. The setter writes the resulting BitmapPainter
        // into _painter (or null on cancel/error).
        //
        // The inner UpdateSourceAsync already catches Exception and
        // routes failure to setImage(null), so this catch is
        // defence-in-depth — primarily covering ObjectDisposedException
        // from a disconnected handler.
        handler._drawableResourceId.Value = null;
        try
        {
            await handler.Loader.UpdateImageSourceAsync().ConfigureAwait(false);
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[ImageHandler] image source load failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Map <see cref="MauiIImage.Aspect"/> to a Compose
    /// <see cref="ContentScale"/>. The
    /// <see cref="ComposeCompanionAttribute"/>-generated getters on
    /// <see cref="ContentScale"/> cache the singleton peer per slot,
    /// so reference equality across compositions is stable.
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

    // Invoked from ComposeImageSetter when MAUI's pipeline finishes a
    // load (or cancels/errors with `null`).
    void OnDrawableLoaded(Drawable? drawable)
    {
        _painter.Value = drawable is null ? null : DrawableToPainter(drawable);
    }

    // Wrap an Android Drawable in a Compose BitmapPainter.
    //
    // AndroidX Core's `Drawable.toBitmap` extension does the right thing
    // for every drawable shape we care about:
    //   * BitmapDrawable — zero-copy return of its existing bitmap when
    //     width/height match the intrinsic size.
    //   * Vector / layer-list / font / shape — allocate a Bitmap of the
    //     intrinsic size and rasterize once.
    // Intrinsic dimensions can be `-1` (e.g. ColorDrawable), so clamp to
    // 1x1 before forwarding — Bitmap.createBitmap(0, 0, ...) throws.
    static ComposePainter DrawableToPainter(Drawable d)
    {
        // d.IntrinsicWidth/Height are JNI getters, so read each once.
        int intrinsicW = d.IntrinsicWidth;
        int intrinsicH = d.IntrinsicHeight;
        var w = intrinsicW > 0 ? intrinsicW : 1;
        var h = intrinsicH > 0 ? intrinsicH : 1;
        var bitmap = DrawableKt.ToBitmap(d, w, h, config: null);

        var imageBitmap = AndroidImageBitmap_androidKt.AsImageBitmap(bitmap);
        // IntSize packs width in the upper 32 bits and height in the
        // lower 32 (matches Compose's `packInts` lowering for the
        // @JvmInline value class). IntOffset.Zero == 0L for srcOffset.
        // FilterQuality.Low (== 1) is Compose's BitmapPainter default
        // and avoids needless filtering for 1:1 source/destination
        // samples. ToBitmap returns the existing BitmapDrawable.Bitmap
        // when its dimensions match (which they do — we asked for the
        // intrinsic size) or allocates a fresh bitmap of (w, h), so
        // reuse our local w/h instead of paying two more JNI getters
        // for bitmap.Width/Height.
        var srcSize = ((long)w << 32) | (uint)h;
        return BitmapPainterKt.BitmapPainter(
            image:         imageBitmap,
            srcOffset:     0L,
            srcSize:       srcSize,
            filterQuality: 1);
    }

    /// <summary>
    /// Bridge implementing MAUI's <see cref="IImageSourcePartSetter"/>.
    /// Holds a weak reference back to the handler so the setter can
    /// outlive a stale loader continuation without rooting a
    /// disconnected handler.
    /// </summary>
    sealed class ComposeImageSetter : IImageSourcePartSetter
    {
        readonly WeakReference<ImageHandler> _handler;

        public ComposeImageSetter(ImageHandler handler) =>
            _handler = new WeakReference<ImageHandler>(handler);

        ImageHandler? Target => _handler.TryGetTarget(out var h) ? h : null;

        public IElementHandler? Handler => Target;

        // IImage already implements IImageSourcePart, so the cast is
        // free; the `as` keeps it defensive against a disconnect race
        // where VirtualView momentarily flips null.
        public IImageSourcePart? ImageSourcePart =>
            Target?.VirtualView as IImageSourcePart;

        public void SetImageSource(Drawable? platformImage) =>
            Target?.OnDrawableLoaded(platformImage);
    }
}
