using AndroidX.Compose.UI.Text;
using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.Drawing;

/// <summary>Nested DrawScope transforms, clipping, effects, images, text, and outlines.</summary>
public static class AdvancedDrawingDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id: "drawing-advanced",
        CategoryId: "drawing",
        Title: "Advanced drawing",
        Description: "Scoped transforms and clips plus points, images, text, outlines, and path effects.",
        Build: composer => Build(composer));

    static Column Build(AndroidX.Compose.Runtime.IComposer composer)
    {
        var textLayout = composer.Remember(
            () => new MutableState<TextLayoutResult?>(null));
        var guide = new Path()
            .MoveTo(10f, 42f)
            .CubicTo(60f, 4f, 110f, 80f, 170f, 28f);
        var diamond = new Path()
            .MoveTo(28f, 0f)
            .LineTo(56f, 28f)
            .LineTo(28f, 56f)
            .LineTo(0f, 28f)
            .Close();
        Offset[] points =
        [
            new Offset(8f, 34f),
            new Offset(42f, 8f),
            new Offset(76f, 34f),
            new Offset(110f, 8f),
            new Offset(144f, 34f),
        ];

        var transformed = new Canvas(scope =>
        {
            scope.ClipRect(new Rect(0f, 0f, scope.Size.Width, scope.Size.Height), clipped =>
                clipped.WithTransform(
                    transform =>
                    {
                        transform.Inset(12f, 12f, 12f, 12f);
                        transform.Rotate(-7f);
                    },
                    drawing =>
                    {
                        drawing.DrawRect(new Color(0xFF, 0xE0, 0xF2, 0xFF));
                        drawing.Scale(0.82f, scaled =>
                            scaled.DrawOutline(diamond, Color.Magenta));
                    }));
        })
        {
            Modifier = Modifier.FillMaxWidth().Height(new Dp(96)),
        };

        var advanced = new Canvas(scope =>
        {
            using var dash = PathEffect.Dash([10f, 6f]);
            using var corners = PathEffect.Corner(8f);
            using var chained = PathEffect.Chain(corners, dash);
            scope.DrawPath(
                guide,
                Color.Cyan,
                style: DrawingStyle.Stroke(5f, chained, StrokeCap.Round));
            scope.DrawPoints(
                points,
                PointMode.Polygon,
                Color.Magenta,
                strokeWidth: 4f,
                cap: StrokeCap.Round);

            using var native = Android.Graphics.Bitmap.CreateBitmap(
                    24, 24,
                    Android.Graphics.Bitmap.Config.Argb8888
                        ?? throw new InvalidOperationException("ARGB bitmap config unavailable."))
                ?? throw new InvalidOperationException("Android bitmap factory returned null.");
            native.EraseColor(Android.Graphics.Color.Yellow);
            using var image = new ImageBitmap(native);
            scope.DrawImage(
                image,
                IntOffset.Zero,
                new IntSize(24, 24),
                new IntOffset(178, 8),
                new IntSize(48, 48),
                filterQuality: FilterQuality.Low);

            var layout = textLayout.Value;
            if (layout is not null)
                scope.DrawText(layout, Color.White, new Offset(10f, 56f));
        })
        {
            Modifier = Modifier.FillMaxWidth()
                .Height(new Dp(112))
                .Background(new Color(0xFF, 0x12, 0x24, 0x44)),
        };

        return new Column(verticalArrangement: Arrangement.SpacedBy(10))
        {
            Modifier.FillMaxWidth(),
            new Text("Nested clip + inset + rotate + scale"),
            transformed,
            new BasicTextField("Measured text source", _ => { }, readOnly: true)
            {
                OnTextLayout = layout => textLayout.Value = layout,
                TextStyle = new TextStyle { Color = Color.White },
            },
            advanced,
        };
    }
}
