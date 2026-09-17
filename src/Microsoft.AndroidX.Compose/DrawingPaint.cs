using AndroidX.Compose.UI.Graphics;

namespace AndroidX.Compose;

internal static class DrawingPaint
{
    const int SrcOverBlendMode = 3;

    internal static IPaint CreatePointPaint(
        float strokeWidth,
        StrokeCap cap,
        PathEffect? pathEffect,
        ColorFilter? colorFilter,
        float alpha)
    {
        if (strokeWidth < 0f)
            throw new ArgumentOutOfRangeException(nameof(strokeWidth));
        var paint = AndroidPaint_androidKt.Paint()
            ?? throw new InvalidOperationException("Compose Paint factory returned null.");
        paint.Style = 1;
        paint.StrokeWidth = strokeWidth;
        paint.StrokeCap = (int)cap;
        paint.PathEffect = pathEffect?.Jvm;
        paint.ColorFilter = colorFilter;
        paint.Alpha = alpha;
        paint.BlendMode = SrcOverBlendMode;
        return paint;
    }

    internal static float[] Flatten(IReadOnlyList<Offset> points)
    {
        var values = new float[checked(points.Count * 2)];
        for (var i = 0; i < points.Count; i++)
        {
            values[i * 2] = points[i].X;
            values[i * 2 + 1] = points[i].Y;
        }
        return values;
    }

    internal static void ValidateImageRegion(IntSize sourceSize, IntSize destinationSize)
    {
        if (sourceSize.Width < 0 || sourceSize.Height < 0)
            throw new ArgumentOutOfRangeException(
                nameof(sourceSize), "Source image dimensions cannot be negative.");
        if (destinationSize.Width < 0 || destinationSize.Height < 0)
            throw new ArgumentOutOfRangeException(
                nameof(destinationSize), "Destination image dimensions cannot be negative.");
    }

}
