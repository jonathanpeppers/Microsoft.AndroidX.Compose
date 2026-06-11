using Android.Graphics.Drawables;
using AndroidX.Compose;
using AndroidX.Compose.UI.Graphics;
using AndroidX.Compose.UI.Graphics.Painter;
using AndroidX.Core.Graphics.Drawable;
using Microsoft.Maui.Platform;
using ComposePainter = AndroidX.Compose.UI.Graphics.Painter.Painter;

namespace Microsoft.AndroidX.Compose.Maui.Loaders;

/// <summary>
/// Hybrid image-source loader shared by every Compose-backed handler
/// that consumes a MAUI <see cref="IImageSourcePart"/>
/// (<see cref="Handlers.ImageHandler"/>,
/// <see cref="Handlers.ImageButtonHandler"/>). Resolves
/// <see cref="IImageSource"/> values to either an Android drawable
/// resource id (fast path, preserves vector + density buckets) or a
/// Compose <see cref="ComposePainter"/> built from a rasterised
/// <see cref="Drawable"/> (general path — file/uri/stream/font sources
/// that don't resolve to a packaged drawable).
/// </summary>
/// <remarks>
/// <para>Owners hold an <see cref="ImageSourceLoader"/> instance per
/// virtual view. Mappers call <see cref="LoadAsync"/>; the loader
/// updates its internal <see cref="MutableState{T}"/> slots and the
/// owning composition recomposes off the next slot read inside
/// <c>BuildNode</c>.</para>
///
/// <para>The owner exposes a delegate
/// (<see cref="ImageSourcePartProvider"/>) that hands back the
/// in-flight <see cref="IImageSourcePart"/> so a stale continuation
/// from a disconnected handler doesn't write into our slots after the
/// virtual view has gone away.</para>
/// </remarks>
public sealed class ImageSourceLoader
{
    /// <summary>
    /// Delegate type returning the live <see cref="IImageSourcePart"/>
    /// — typically the handler's <see cref="IElementHandler.VirtualView"/>
    /// cast to <see cref="IImageSourcePart"/>. Returning <c>null</c>
    /// short-circuits the loader's setter callback so a disconnected
    /// handler can't observe a late drawable.
    /// </summary>
    public delegate IImageSourcePart? ImageSourcePartProvider();

    /// <summary>
    /// Drawable-resource fast path — Compose's
    /// <c>painterResource(id)</c> handles vector drawables + per-density
    /// buckets without rasterizing.
    /// </summary>
    public MutableState<int?> DrawableResourceId { get; } = new((int?)null);

    /// <summary>
    /// General-path slot — drawable produced by MAUI's
    /// <see cref="IImageSourceServiceProvider"/> pipeline, wrapped as a
    /// Compose <c>BitmapPainter</c>. Painter wins over
    /// <see cref="DrawableResourceId"/> at render time so a
    /// freshly-loaded URI replaces a stale fast-path drawable.
    /// </summary>
    public MutableState<ComposePainter?> Painter { get; } = new((ComposePainter?)null);

    readonly IElementHandler                _handler;
    readonly ImageSourcePartProvider        _sourceProvider;
    ImageSourcePartLoader?                  _loader;

    /// <summary>
    /// Construct a loader bound to a Compose-backed
    /// <see cref="IElementHandler"/> and a delegate that returns the
    /// in-flight <see cref="IImageSourcePart"/>.
    /// </summary>
    public ImageSourceLoader(IElementHandler handler, ImageSourcePartProvider sourceProvider)
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(sourceProvider);
        _handler        = handler;
        _sourceProvider = sourceProvider;
    }

    // Lazy: handlers that only ever see file sources don't allocate
    // the loader or its setter at all.
    ImageSourcePartLoader Loader =>
        _loader ??= new ImageSourcePartLoader(new ComposeImageSetter(this));

    /// <summary>
    /// Resolve <paramref name="src"/> to either a drawable id (fast
    /// path) or a <see cref="ComposePainter"/> via MAUI's pipeline
    /// (general path), updating the loader's
    /// <see cref="DrawableResourceId"/> / <see cref="Painter"/> slots
    /// so the owning composition recomposes.
    /// </summary>
    public async Task LoadAsync(IImageSource? src)
    {
        if (src is null || src.IsEmpty)
        {
            _loader?.Reset();
            DrawableResourceId.Value = null;
            Painter.Value            = null;
            return;
        }

        // Fast path: file source resolves to a packaged drawable id.
        // Context.GetDrawableId mirrors MAUI's FileImageSourceService —
        // lower-cases the file name and asks
        // Resources.GetIdentifier("name", "drawable", PackageName).
        var context = _handler.MauiContext?.Context;
        if (src is IFileImageSource file && context is not null &&
            context.GetDrawableId(file.File ?? string.Empty) is var id && id > 0)
        {
            _loader?.Reset();
            Painter.Value            = null;
            DrawableResourceId.Value = id;
            return;
        }

        DrawableResourceId.Value = null;
        try
        {
            await Loader.UpdateImageSourceAsync().ConfigureAwait(false);
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[ImageSourceLoader] image source load failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Cancel any in-flight load and clear both slots — call from the
    /// owning handler's <c>DisconnectHandler</c> so a late
    /// continuation doesn't write into a disposed handler.
    /// </summary>
    public void Reset()
    {
        _loader?.Reset();
        DrawableResourceId.Value = null;
        Painter.Value            = null;
    }

    void OnDrawableLoaded(Drawable? drawable)
    {
        Painter.Value = drawable is null ? null : DrawableToPainter(drawable);
    }

    // Wrap an Android Drawable in a Compose BitmapPainter. Same logic
    // as the original hand-written ImageHandler shipped — see the
    // shared comments there for JNI / packed-IntSize details.
    static ComposePainter DrawableToPainter(Drawable d)
    {
        int intrinsicW = d.IntrinsicWidth;
        int intrinsicH = d.IntrinsicHeight;
        var w = intrinsicW > 0 ? intrinsicW : 1;
        var h = intrinsicH > 0 ? intrinsicH : 1;
        var bitmap = DrawableKt.ToBitmap(d, w, h, config: null);

        var imageBitmap = AndroidImageBitmap_androidKt.AsImageBitmap(bitmap);
        var srcSize = ((long)w << 32) | (uint)h;
        return BitmapPainterKt.BitmapPainter(
            image:         imageBitmap,
            srcOffset:     0L,
            srcSize:       srcSize,
            filterQuality: 1);
    }

    // Bridge implementing MAUI's IImageSourcePartSetter. Holds a weak
    // reference back to the loader so the setter outlives stale
    // continuations without rooting a disconnected handler.
    sealed class ComposeImageSetter : IImageSourcePartSetter
    {
        readonly WeakReference<ImageSourceLoader> _loader;

        public ComposeImageSetter(ImageSourceLoader loader) =>
            _loader = new WeakReference<ImageSourceLoader>(loader);

        ImageSourceLoader? Target => _loader.TryGetTarget(out var l) ? l : null;

        public IElementHandler? Handler => Target?._handler;

        public IImageSourcePart? ImageSourcePart =>
            Target?._sourceProvider?.Invoke();

        public void SetImageSource(Drawable? platformImage) =>
            Target?.OnDrawableLoaded(platformImage);
    }
}
