using Android.Content;
using Android.Views;
using AndroidX.Media3.Common;
using AndroidX.Media3.ExoPlayer;
using AndroidX.Media3.UI;

namespace AndroidX.Compose.Samples.Jetchat;

internal sealed class VideoPlaybackSession
{
    readonly string _videoUri;
    readonly VideoErrorMessageProvider _errorMessageProvider = new();
    IExoPlayer? _player;
    PlayerView? _view;
    bool _released;

    internal VideoPlaybackSession(string videoUri)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(videoUri);
        _videoUri = videoUri;
    }

    internal View CreateView(Context context)
    {
        if (_released)
            throw new InvalidOperationException("Cannot attach a released video player.");
        if (_view is not null)
            return _view;

        using var builder = new ExoPlayerBuilder(context);
        var player = builder.Build()
            ?? throw new InvalidOperationException("Media3 could not create an ExoPlayer.");
        using var itemBuilder = new MediaItem.Builder();
        using var item = itemBuilder.SetUri(_videoUri)?.Build()
            ?? throw new InvalidOperationException("Media3 could not create the selected media item.");

        player.SetMediaItem(item);
        player.PlayWhenReady = true;
        player.Prepare();

        var view = new PlayerView(context)
        {
            ControllerAutoShow = true,
            Player = player,
            UseController = true,
        };
        view.SetErrorMessageProvider(_errorMessageProvider);
        view.SetShowBuffering(PlayerView.ShowBufferingAlways);
        view.ContentDescription = "Video player";

        _player = player;
        _view = view;
        return view;
    }

    internal void Pause() => _player?.Pause();

    internal void Release()
    {
        if (_released)
            return;
        _released = true;

        if (_view is not null)
        {
            _view.Player = null;
            _view = null;
        }
        if (_player is not null)
        {
            _player.Release();
            _player.Dispose();
            _player = null;
        }
        _errorMessageProvider.Dispose();
    }
}
