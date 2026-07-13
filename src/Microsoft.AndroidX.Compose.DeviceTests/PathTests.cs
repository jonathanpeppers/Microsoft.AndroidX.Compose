using AndroidX.Compose;
using ComposePath = AndroidX.Compose.Path;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Exercises the bound Compose path factory and mutable geometry methods.</summary>
[TestClass]
public class PathTests
{
    [TestMethod]
    public void PathBuilder_CreatesAndResetsGeometry()
    {
        using var path = new ComposePath()
            .MoveTo(0f, 0f)
            .LineTo(20f, 0f)
            .LineTo(10f, 20f)
            .Close();

        Assert.IsFalse(path.IsEmpty);
        path.Reset();
        Assert.IsTrue(path.IsEmpty);
    }

    [TestMethod]
    public void PathOperation_CombinesPaths()
    {
        using var first = new ComposePath()
            .MoveTo(0f, 0f)
            .LineTo(20f, 0f)
            .LineTo(20f, 20f)
            .Close();
        using var second = new ComposePath(first).Translate(new Offset(10f, 0f));
        using var result = new ComposePath();

        Assert.IsTrue(result.Op(first, second, PathOperation.Union));
        Assert.IsFalse(result.IsEmpty);
    }

    [TestMethod]
    public void PathFactories_AndMeasure_WorkTogether()
    {
        using var path = new ComposePath()
            .AddRect(new Rect(0f, 0f, 40f, 20f))
            .AddOval(new Rect(50f, 0f, 90f, 40f))
            .AddRoundRect(
                new Rect(0f, 50f, 90f, 100f),
                new CornerRadius(8f));
        using var measure = new PathMeasure(path);

        Assert.IsTrue(measure.Length > 0f);
        var position = measure.GetPosition(measure.Length / 2f);
        Assert.IsFalse(float.IsNaN(position.X));
        Assert.IsFalse(float.IsNaN(position.Y));
    }

    [TestMethod]
    public void AddRect_UsesComposeCounterClockwiseWinding()
    {
        using var path = new ComposePath()
            .AddRect(new Rect(0f, 0f, 40f, 20f));
        using var measure = new PathMeasure(path);

        var position = measure.GetPosition(1f);

        Assert.AreEqual(0f, position.X, 0.01f);
        Assert.IsTrue(position.Y > 0f);
    }
}
