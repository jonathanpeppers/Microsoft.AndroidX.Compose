namespace AndroidX.Compose.Samples.Jetchat;

internal static class VideoPlayer
{
    internal static ComposableNode Build(string videoUri, Action onDismiss) =>
        new Composed(c =>
        {
            var session = c.Remember(() => new VideoPlaybackSession(videoUri), key1: videoUri);
            return new Box
            {
                Modifier.FillMaxSize().Background(Color.Black),
                new DisposableEffect(videoUri, () =>
                {
                    VideoPlaybackCoordinator.Activate(session);
                    return () =>
                    {
                        VideoPlaybackCoordinator.Deactivate(session);
                        session.Release();
                    };
                }),
                new AndroidView(session.CreateView)
                {
                    Modifier = Modifier.FillMaxSize(),
                },
                new BackHandler(onDismiss),
                BuildCloseButton(onDismiss),
            };
        });

    static IconButton BuildCloseButton(Action onDismiss)
    {
        var button = new IconButton(onDismiss)
        {
            Modifier = Modifier
                .Align(Alignment.TopEnd)
                .Padding(16)
                .Size(48)
                .Background(Color.Black.WithAlpha(166), Shape.Circle()),
        };
        button.Add(new Icon(Resource.Drawable.ic_close, "Close video")
        {
            Tint = Color.White,
        });
        return button;
    }
}
