using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.Drawing;

/// <summary>Canvas, DrawScope, Path, Brush, and cached/content drawing modifiers.</summary>
public static class DrawingPrimitivesDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id: "drawing-primitives",
        CategoryId: "drawing",
        Title: "Drawing primitives",
        Description: "Canvas path geometry plus drawWithContent and drawWithCache modifiers.",
        Build: _ => Build());

    static Column Build()
    {
        var magenta = new Color(0xFF, 0xFF, 0x00, 0xCC);
        var cyan = new Color(0xFF, 0x00, 0xCC, 0xFF);
        var navy = new Color(0xFF, 0x12, 0x24, 0x44);
        var gradient = Brush.LinearGradient(magenta, cyan);
        var stroke = DrawingStyle.Stroke(5f, StrokeCap.Round, StrokeJoin.Round);
        var wave = new Path()
            .MoveTo(12f, 130f)
            .CubicTo(60f, 30f, 120f, 210f, 190f, 90f)
            .CubicTo(225f, 30f, 270f, 155f, 320f, 70f);

        var canvas = new Canvas(scope =>
        {
            scope.DrawRoundRect(
                gradient,
                new CornerRadius(24f),
                size: scope.Size);
            scope.DrawPath(wave, Color.White, style: stroke);
            scope.DrawCircle(
                Color.White,
                radius: 18f,
                center: new Offset(scope.Size.Width - 36f, 36f));
        })
        {
            Modifier = Modifier.FillMaxWidth().Height(new Dp(180)),
        };

        var contentOverlay = new Box
        {
            Modifier.FillMaxWidth()
                .Height(new Dp(72))
                .Background(navy)
                .DrawWithContent(scope =>
                {
                    scope.DrawContent();
                    scope.DrawLine(
                        cyan,
                        new Offset(12f, scope.Size.Height - 12f),
                        new Offset(scope.Size.Width - 12f, 12f),
                        strokeWidth: 6f,
                        cap: StrokeCap.Round);
                }),
            new Text("drawWithContent") { Color = Color.White },
        };

        var cached = new Box
        {
            Modifier.FillMaxWidth()
                .Height(new Dp(72))
                .DrawWithCache(cache =>
                {
                    var radius = MathF.Min(cache.Size.Width, cache.Size.Height) / 2f;
                    cache.OnDrawBehind(scope =>
                        scope.DrawCircle(gradient, radius: radius, center: scope.Center));
                }),
            new Text("drawWithCache") { Color = Color.White },
        };

        return new Column(verticalArrangement: Arrangement.SpacedBy(12))
        {
            Modifier.FillMaxWidth(),
            new Text("Canvas + cubic Path + gradient Brush"),
            canvas,
            contentOverlay,
            cached,
        };
    }
}
