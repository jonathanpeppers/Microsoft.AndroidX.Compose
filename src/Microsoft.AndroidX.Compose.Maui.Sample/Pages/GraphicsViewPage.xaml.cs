namespace Microsoft.AndroidX.Compose.Maui.Sample.Pages;

/// <summary>
/// GraphicsView demo — drives an <see cref="IDrawable"/> through the
/// stock <c>GraphicsViewHandler</c> hosted by Compose
/// <see cref="AndroidX.Compose.AndroidView"/>. Tapping the canvas
/// bumps a seed and re-shuffles the drawing, which exercises both
/// the fallback's repaint path and tap routing.
/// </summary>
public partial class GraphicsViewPage : ContentPage
{
    readonly SparkDrawable _drawable = new();

    /// <summary>Build the page and seed the drawable.</summary>
    public GraphicsViewPage()
    {
        InitializeComponent();
        GraphicsCanvas.Drawable = _drawable;
    }

    void OnTap(object? sender, TouchEventArgs e)
    {
        _drawable.Seed++;
        SeedLabel.Text = $"Seed: {_drawable.Seed}  ·  Tap to re-shuffle";
        GraphicsCanvas.Invalidate();
    }

    /// <summary>
    /// Synthetic drawable — random sparkline with a gradient-filled
    /// circle on the right edge. Kept inside the page so the demo is
    /// self-contained.
    /// </summary>
    sealed class SparkDrawable : IDrawable
    {
        public int Seed { get; set; }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            var random = new Random(Seed);

            // Background fill.
            canvas.FillColor = Color.FromArgb("#FAFAFA");
            canvas.FillRectangle(dirtyRect);

            // Border.
            canvas.StrokeColor = Color.FromArgb("#1A000000");
            canvas.StrokeSize = 1f;
            canvas.DrawRectangle(dirtyRect);

            // Spark line — 12 random points across the width.
            const int Points = 12;
            float stepX = dirtyRect.Width / (Points - 1);
            var path = new PathF();
            for (int i = 0; i < Points; i++)
            {
                float x = i * stepX;
                float y = 20f + (float)random.NextDouble() * (dirtyRect.Height - 40f);
                if (i == 0) path.MoveTo(x, y);
                else        path.LineTo(x, y);
            }
            canvas.StrokeColor = Color.FromArgb("#512BD4");
            canvas.StrokeSize = 3f;
            canvas.DrawPath(path);

            // Right-edge circle.
            float cx = dirtyRect.Right - 32f;
            float cy = dirtyRect.Bottom - 32f;
            canvas.FillColor = Color.FromArgb("#E91E63");
            canvas.FillCircle(cx, cy, 22f);
            canvas.FontColor = Colors.White;
            canvas.FontSize = 14f;
            canvas.DrawString(Seed.ToString(), cx - 10f, cy - 8f, 20f, 16f, HorizontalAlignment.Center, VerticalAlignment.Center);
        }
    }
}
