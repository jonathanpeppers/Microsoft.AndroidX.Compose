namespace AndroidX.Compose.Samples.Jetchat;

internal static class VideoThumbnail
{
    internal static ComposableNode Build(
        string videoUri,
        Action onClick,
        string description,
        Modifier modifier) =>
        new Box
        {
            modifier
                .Background(Color.FromArgb(255, 20, 20, 30), new RoundedCornerShape(12.Dp()))
                .Clip(new RoundedCornerShape(12.Dp()))
                .Clickable(onClick)
                .Semantics(description),
            new AndroidView(
                context => new VideoThumbnailView(context, videoUri),
                view => ((VideoThumbnailView)view).SetVideoUri(videoUri))
            {
                Modifier = Modifier.FillMaxSize(),
            },
            new Icon(Resource.Drawable.ic_play_arrow, description)
            {
                Modifier = Modifier
                    .Align(Alignment.Center)
                    .Size(56)
                    .Clickable(onClick)
                    .Padding(10)
                    .Background(Color.Black.WithAlpha(166), Shape.Circle()),
                Tint = Color.White,
            },
        };
}
