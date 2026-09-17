using Android.Content;
using Android.Graphics;
using Android.Media;

namespace AndroidX.Compose.Samples.Jetchat;

internal sealed class VideoThumbnailView : ImageView
{
    readonly Context _context;
    string _videoUri;
    CancellationTokenSource _cancellation = new();
    Bitmap? _bitmap;
    internal string CurrentVideoUri => _videoUri;

    internal VideoThumbnailView(Context context, string videoUri) : base(context)
    {
        _context = context;
        _videoUri = videoUri;
        SetBackgroundColor(Android.Graphics.Color.Rgb(20, 20, 30));
        SetScaleType(ScaleType.CenterCrop);
        ImportantForAccessibility = Android.Views.ImportantForAccessibility.No;
        _ = LoadAsync(_videoUri, _cancellation.Token);
    }

    internal void SetVideoUri(string videoUri)
    {
        if (_videoUri == videoUri)
            return;
        _cancellation.Cancel();
        _cancellation.Dispose();
        _cancellation = new CancellationTokenSource();
        _videoUri = videoUri;
        SetImageDrawable(null);
        _bitmap?.Dispose();
        _bitmap = null;
        _ = LoadAsync(_videoUri, _cancellation.Token);
    }

    protected override void OnDetachedFromWindow()
    {
        _cancellation.Cancel();
        _cancellation.Dispose();
        SetImageDrawable(null);
        _bitmap?.Dispose();
        _bitmap = null;
        base.OnDetachedFromWindow();
    }

    async Task LoadAsync(string videoUri, CancellationToken cancellationToken)
    {
        Bitmap? loaded = null;
        try
        {
            loaded = await Task.Run(
                () => ExtractFrame(videoUri, cancellationToken),
                cancellationToken);
            if (cancellationToken.IsCancellationRequested || loaded is null)
            {
                loaded?.Dispose();
                return;
            }

            Post(() =>
            {
                if (cancellationToken.IsCancellationRequested)
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

    Bitmap? ExtractFrame(string videoUri, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var retriever = new MediaMetadataRetriever();
        var uri = Android.Net.Uri.Parse(videoUri)
            ?? throw new InvalidOperationException("Video URI could not be parsed.");
        retriever.SetDataSource(_context, uri);
        return retriever.GetFrameAtTime(
            1_000_000,
            Option.ClosestSync);
    }
}
