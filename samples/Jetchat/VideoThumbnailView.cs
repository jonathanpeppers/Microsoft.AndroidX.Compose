using Android.Content;
using Android.Graphics;
using Android.Media;

namespace AndroidX.Compose.Samples.Jetchat;

internal class VideoThumbnailView : ImageView
{
    readonly Context _context;
    string _videoUri;
    CancellationTokenSource? _cancellation;
    Bitmap? _bitmap;
    bool _attached;
    bool _disposed;
    internal string CurrentVideoUri => _videoUri;
    internal int LoadGeneration { get; private set; }
    internal bool HasActiveLoad => _cancellation is { IsCancellationRequested: false };

    internal VideoThumbnailView(Context context, string videoUri) : base(context)
    {
        _context = context;
        _videoUri = videoUri;
        SetBackgroundColor(Android.Graphics.Color.Rgb(20, 20, 30));
        SetScaleType(ScaleType.CenterCrop);
        ImportantForAccessibility = Android.Views.ImportantForAccessibility.No;
    }

    internal void SetVideoUri(string videoUri)
    {
        if (_videoUri == videoUri)
            return;
        CancelLoad();
        _videoUri = videoUri;
        SetImageDrawable(null);
        _bitmap?.Dispose();
        _bitmap = null;
        if (_attached)
            StartLoad();
    }

    protected override void OnAttachedToWindow()
    {
        base.OnAttachedToWindow();
        _attached = true;
        if (_bitmap is null)
            StartLoad();
    }

    protected override void OnDetachedFromWindow()
    {
        _attached = false;
        CancelLoad();
        base.OnDetachedFromWindow();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !_disposed)
        {
            _disposed = true;
            CancelLoad();
            SetImageDrawable(null);
            _bitmap?.Dispose();
            _bitmap = null;
        }
        base.Dispose(disposing);
    }

    void StartLoad()
    {
        CancelLoad();
        _cancellation = new CancellationTokenSource();
        LoadGeneration++;
        _ = LoadAsync(_videoUri, _cancellation.Token);
    }

    void CancelLoad()
    {
        var cancellation = _cancellation;
        _cancellation = null;
        if (cancellation is null)
            return;
        cancellation.Cancel();
        cancellation.Dispose();
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
        using var uri = Android.Net.Uri.Parse(videoUri)
            ?? throw new InvalidOperationException("Video URI could not be parsed.");
        retriever.SetDataSource(_context, uri);
        return retriever.GetFrameAtTime(
            1_000_000,
            Option.ClosestSync);
    }
}
