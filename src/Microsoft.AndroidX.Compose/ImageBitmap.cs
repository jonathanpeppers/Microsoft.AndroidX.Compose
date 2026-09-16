using BoundImageBitmap = AndroidX.Compose.UI.Graphics.IImageBitmap;
using ImageBitmapFactory = AndroidX.Compose.UI.Graphics.ImageBitmapKt;
using NativeImageBitmap = AndroidX.Compose.UI.Graphics.AndroidImageBitmap_androidKt;

namespace AndroidX.Compose;

/// <summary>Owned Compose image bitmap accepted by DrawScope image operations.</summary>
public sealed class ImageBitmap : IDisposable
{
    BoundImageBitmap? _jvm;

    internal BoundImageBitmap Jvm => _jvm
        ?? throw new ObjectDisposedException(nameof(ImageBitmap));

    ImageBitmap(BoundImageBitmap jvm) => _jvm = jvm;

    /// <summary>Wraps an Android bitmap as a Compose image bitmap.</summary>
    public ImageBitmap(Android.Graphics.Bitmap bitmap)
    {
        ArgumentNullException.ThrowIfNull(bitmap);
        _jvm = NativeImageBitmap.AsImageBitmap(bitmap)
            ?? throw new InvalidOperationException("Compose ImageBitmap conversion returned null.");
    }

    /// <summary>Decodes an encoded image into a Compose image bitmap.</summary>
    public static ImageBitmap Decode(byte[] encoded)
    {
        ArgumentNullException.ThrowIfNull(encoded);
        var image = ImageBitmapFactory.DecodeToImageBitmap(encoded)
            ?? throw new InvalidOperationException("Compose ImageBitmap decoder returned null.");
        return new ImageBitmap(image);
    }

    /// <summary>Image width in pixels.</summary>
    public int Width => Jvm.Width;

    /// <summary>Image height in pixels.</summary>
    public int Height => Jvm.Height;

    /// <summary>Whether the image stores an alpha channel.</summary>
    public bool HasAlpha => Jvm.HasAlpha;

    /// <summary>Requests eager preparation for drawing.</summary>
    public void PrepareToDraw() => Jvm.PrepareToDraw();

    /// <summary>
    /// Returns the Android bitmap backing this Compose image. The returned
    /// peer is borrowed and must not be disposed independently.
    /// </summary>
    public Android.Graphics.Bitmap NativeBitmap =>
        NativeImageBitmap.AsAndroidBitmap(Jvm)
        ?? throw new InvalidOperationException("Compose ImageBitmap had no Android bitmap.");

    /// <summary>Releases the owned Compose image-bitmap peer.</summary>
    public void Dispose()
    {
        var peer = _jvm;
        _jvm = null;
        peer?.Dispose();
    }
}
