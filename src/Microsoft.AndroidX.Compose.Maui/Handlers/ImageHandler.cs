using System.Diagnostics;
using AndroidX.Compose;
using AndroidX.Compose.UI.Platform;
using Microsoft.Maui.Handlers;
using ComposeImage = AndroidX.Compose.Image;
using MauiIImage = Microsoft.Maui.IImage;

namespace Microsoft.AndroidX.Compose.Maui.Handlers;

/// <summary>
/// MAUI <see cref="Microsoft.Maui.Controls.Image"/> handler that renders
/// through Jetpack Compose's <c>Image</c> composable
/// (<see cref="ComposeImage"/>). Replaces MAUI's stock
/// <c>AppCompatImageView</c>-based handler when the consumer calls
/// <see cref="Hosting.AppHostBuilderExtensions.UseAndroidXCompose"/>.
/// </summary>
/// <remarks>
/// <para>Phase 2 first slice only supports <c>FileImageSource</c> values
/// that resolve to a packaged Android drawable. MAUI's
/// <c>MauiImage</c> build action (the default for files under
/// <c>Resources/Images/</c>) generates per-density drawables in the
/// APK, so the common case — a project-level image like
/// <c>dotnet_bot.png</c> — works out of the box.</para>
///
/// <para>Other <see cref="IImageSource"/> shapes (URI, stream, font,
/// gradient brush) need MAUI's <c>IImageSourceService&lt;TSource&gt;</c>
/// pipeline to materialize into a <c>Drawable</c>/<c>Bitmap</c> — that's
/// queued for a later slice. The handler logs a
/// <see cref="Debug"/> message and leaves the surface blank when it
/// can't resolve the source.</para>
/// </remarks>
public partial class ImageHandler : ViewHandler<MauiIImage, ComposeView>
{
    /// <summary>
    /// Property mapper that forwards MAUI <see cref="MauiIImage"/> property
    /// changes to the Compose-backed <see cref="ComposeView"/>.
    /// </summary>
    public static IPropertyMapper<MauiIImage, ImageHandler> Mapper =
        new PropertyMapper<MauiIImage, ImageHandler>(ViewHandler.ViewMapper)
        {
            [nameof(IImageSourcePart.Source)] = MapSource,
            [nameof(MauiIImage.Aspect)]           = MapAspect,
        };

    /// <summary>Command mapper (inherits view-level commands; no extras).</summary>
    public static CommandMapper<MauiIImage, ImageHandler> CommandMapper =
        new(ViewCommandMapper);

    // ContentScale wrapped as an int index so MutableState picks the
    // primitive path (the wrapper type is a Java.Lang.Object subclass,
    // and Compose snapshots compare by reference — picking the actual
    // ContentScale instance inside SetContent avoids recompose churn).
    const int ContentScaleFit        = 0;
    const int ContentScaleCrop       = 1;
    const int ContentScaleFillBounds = 2;

    readonly MutableState<int?> _resourceId  = new((int?)null);
    readonly MutableState<int>  _contentScale = new(ContentScaleFit);

    /// <summary>Construct a handler with the default mappers.</summary>
    public ImageHandler() : base(Mapper, CommandMapper) { }

    /// <summary>Construct a handler with custom mappers.</summary>
    public ImageHandler(IPropertyMapper? mapper, CommandMapper? commandMapper = null)
        : base(mapper ?? Mapper, commandMapper ?? CommandMapper) { }

    /// <inheritdoc/>
    protected override ComposeView CreatePlatformView()
    {
        var view = new ComposeView(Context);
        view.SetContent(_ =>
        {
            if (_resourceId.Value is not int id)
                return new Box(); // Empty placeholder while no source resolved.
            var scale = _contentScale.Value switch
            {
                ContentScaleCrop       => ContentScale.Crop,
                ContentScaleFillBounds => ContentScale.FillBounds,
                _                      => ContentScale.Fit,
            };
            return new ComposeImage(id)
            {
                ContentScale = scale,
            };
        });
        return view;
    }

    /// <inheritdoc/>
    protected override void DisconnectHandler(ComposeView platformView)
    {
        platformView.DisposeComposition();
        base.DisconnectHandler(platformView);
    }

    /// <summary>
    /// Map <see cref="IImageSourcePart.Source"/> to a resolved Android
    /// drawable resource id. Supports <see cref="IFileImageSource"/>
    /// only; everything else logs and falls through so the surface
    /// renders blank rather than crashing.
    /// </summary>
    public static void MapSource(ImageHandler handler, MauiIImage image)
    {
        handler._resourceId.Value = handler.ResolveDrawableId(image.Source);
    }

    /// <summary>Map <see cref="MauiIImage.Aspect"/> to a Compose <c>ContentScale</c> index.</summary>
    public static void MapAspect(ImageHandler handler, MauiIImage image) =>
        handler._contentScale.Value = image.Aspect switch
        {
            Aspect.AspectFill => ContentScaleCrop,
            Aspect.Fill       => ContentScaleFillBounds,
            // AspectFit + Center both map to Fit — Compose's `Fit`
            // already preserves aspect ratio and centers the result.
            _                 => ContentScaleFit,
        };

    int? ResolveDrawableId(IImageSource? source)
    {
        if (source is null || source.IsEmpty) return null;
        if (source is not IFileImageSource fileSource)
        {
            Debug.WriteLine(
                $"[ImageHandler] {source.GetType().Name} not supported in Phase 2 slice 1; " +
                "only FileImageSource is wired. Tracking issue in docs/maui-backend.md.");
            return null;
        }
        var ctx  = Context;
        var name = ResourceNameFromFile(fileSource.File);
        if (name is null) return null;
        var id = ctx.Resources?.GetIdentifier(name, "drawable", ctx.PackageName);
        if (id is null or 0)
        {
            Debug.WriteLine(
                $"[ImageHandler] Could not resolve drawable '{name}' (from '{fileSource.File}') " +
                $"in package '{ctx.PackageName}'. Ensure the image is under Resources/Images/.");
            return null;
        }
        return id;
    }

    /// <summary>
    /// Convert a MAUI <see cref="IFileImageSource.File"/> path
    /// (e.g. <c>dotnet_bot.png</c>) to the Android resource name
    /// (<c>dotnet_bot</c>). The MAUI build pipeline lower-cases the
    /// file name when emitting drawables; we mirror that here.
    /// </summary>
    static string? ResourceNameFromFile(string? file)
    {
        if (string.IsNullOrEmpty(file)) return null;
        var name = System.IO.Path.GetFileNameWithoutExtension(file);
        return string.IsNullOrEmpty(name) ? null : name.ToLowerInvariant();
    }
}
