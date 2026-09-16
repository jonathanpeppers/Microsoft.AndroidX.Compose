using Android.Content;
using Android.Graphics;
using Android.Media;

namespace AndroidX.Compose.Samples.Jetchat;

internal sealed class VideoThumbnailView : ImageView
{
    readonly Context _context;
    readonly string _videoUri;
    readonly CancellationTokenSource _cancellation = new();
    Bitmap? _bitmap;

    internal VideoThumbnailView(Context context, string videoUri) : base(context)
    {
        _context = context;
        _videoUri = videoUri;
        SetBackgroundColor(Android.Graphics.Color.Rgb(20, 20, 30));
        SetScaleType(ScaleType.CenterCrop);
        ImportantForAccessibility = Android.Views.ImportantForAccessibility.No;
        _ = LoadAsync();
    }

    protected override void OnDetachedFromWindow()
    {
        _cancellation.Cancel();
        SetImageDrawable(null);
        _bitmap?.Dispose();
        _bitmap = null;
        base.OnDetachedFromWindow();
    }

    async Task LoadAsync()
    {
        Bitmap? loaded = null;
        try
        {
            loaded = await Task.Run(ExtractFrame, _cancellation.Token);
            if (_cancellation.IsCancellationRequested || loaded is null)
            {
                loaded?.Dispose();
                return;
            }

            Post(() =>
            {
                if (_cancellation.IsCancellationRequested)
                {
                    loaded.Dispose();
                    return;
                }
                _bitmap?.Dispose();
                _bitmap = loaded;
                SetImageBitmap(loaded);
            });
        }
        catch (OperationCanceledException)
        {
            loaded?.Dispose();
        }
        catch (Exception ex)
        {
            loaded?.Dispose();
            Android.Util.Log.Warn("JetchatVideo", $"Thumbnail unavailable: {ex.Message}");
        }
    }

    Bitmap? ExtractFrame()
    {
        _cancellation.Token.ThrowIfCancellationRequested();
        using var retriever = new MediaMetadataRetriever();
        var uri = Android.Net.Uri.Parse(_videoUri)
            ?? throw new InvalidOperationException("Video URI could not be parsed.");
        retriever.SetDataSource(_context, uri);
        return retriever.GetFrameAtTime(
            1_000_000,
            Option.ClosestSync);
    }
}
