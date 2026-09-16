using System.Runtime.CompilerServices;
using Android.Runtime;
using AndroidX.Compose;
using AndroidX.Compose.UI.Graphics;
using AndroidX.Compose.UI.Unit;
using ComposePath = AndroidX.Compose.Path;
using Region = Android.Graphics.Region;
using PathMeasure = AndroidX.Compose.PathMeasure;
using Size = AndroidX.Compose.Size;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Native outline geometry, density, RTL, and retained path-builder lifetime contracts.</summary>
[TestClass]
[DoNotParallelize]
public class ShapeTests
{
    /// <summary>Every new corner constructor and factory preserves physical corner order and units.</summary>
    [TestMethod]
    [DataRow(false, 1f, 120, 80)]
    [DataRow(true, 1f, 120, 80)]
    [DataRow(false, 2f, 200, 160)]
    [DataRow(true, 2f, 200, 160)]
    public void CornersMatchNativeGeometry(bool rtl, float density, int width, int height)
    {
        for (int kind = 0; kind < 3; kind++)
        {
            for (int variant = 0; variant < 4; variant++)
            {
                using var shape = CreateCornerShape(kind, variant);
                float[] corners = variant < 2 ? [10, 10, 10, 10] : [5, 10, 15, 20];
                float unit = variant % 2 == 0 ? density : Math.Min(width, height) / 100f;
                for (int i = 0; i < corners.Length; i++)
                    corners[i] *= unit;
                if (rtl && kind == 0)
                    corners = [corners[1], corners[0], corners[3], corners[2]];
                using var outline = Outline(shape, new Size(width, height), rtl, density);
                AssertCorners(outline, width, height, corners, rounded: kind == 2);
            }
        }
        using var dp = Shape.CutCorners(5.Dp(), 10.Dp(), 15.Dp(), 20.Dp());
        using var percent = Shape.CutCornersPercent(5, 10, 15, 20);
        using var dpOutline = Outline(dp, new Size(width, height), rtl, density);
        using var percentOutline = Outline(percent, new Size(width, height), rtl, density);
        float[] logical = rtl ? [10, 5, 20, 15] : [5, 10, 15, 20];
        AssertCorners(dpOutline, width, height, logical.Select(x => x * density).ToArray(), false);
        AssertCorners(percentOutline, width, height,
            logical.Select(x => x * Math.Min(width, height) / 100f).ToArray(), false);
    }

    /// <summary>Size and direction reach the builder exactly, and Kotlin closes the borrowed path.</summary>
    [TestMethod]
    public void GenericBuilderUsesNativePathAndClosesContour()
    {
        var probe = new ShapeCallbackProbe { RetainCopy = true };
        using var shape = new GenericShape(probe.Build);
        using var outline = Outline(shape, new Size(120, 80), false, 2);
        Assert.AreEqual(1, probe.Calls);
        Assert.AreEqual(new Size(120, 80), probe.LastSize);
        Assert.AreEqual(LayoutDirection.Ltr, probe.LastDirection);
        var generic = Assert.IsInstanceOfType<Outline.Generic>(outline);
        using var borrowedOutline = new ComposePath(generic.Path);
        using var measure = new PathMeasure(borrowedOutline);
        float expectedLength = 2 * MathF.Sqrt(120 * 120 + 40 * 40) + 80;
        Assert.AreEqual(expectedLength, measure.Length, 0.1f, "Kotlin did not close the builder's contour.");
        var expired = probe.Borrowed ?? throw new InvalidOperationException("Builder did not receive a path.");
        Assert.ThrowsExactly<ObjectDisposedException>(() => expired.LineTo(1, 1));
        using var copy = probe.Copy ?? throw new InvalidOperationException("Independent path copy missing.");
        Assert.IsFalse(copy.IsEmpty);
        using var copiedMeasure = new PathMeasure(copy);
        Assert.AreEqual(expectedLength - 80, copiedMeasure.Length, 0.1f, "The retained copy must remain independent.");
        probe.RetainCopy = false;
        using var second = Outline(shape, new Size(60, 100), true, 1);
        Assert.AreEqual(2, probe.Calls);
        Assert.AreEqual(new Size(60, 100), probe.LastSize);
        Assert.AreEqual(LayoutDirection.Rtl, probe.LastDirection);
        using var region = Region(second, 60, 100);
        Assert.IsTrue(region.Contains(55, 10));
        Assert.IsFalse(region.Contains(5, 10));
    }

    /// <summary>Borrowed disposal cannot delete Kotlin's path, and malformed callbacks fail explicitly.</summary>
    [TestMethod]
    public void BorrowedPathDisposalAndNullBuilderAreSafe()
    {
#pragma warning disable CS8625 // Exercise the public runtime null guard.
        Assert.ThrowsExactly<ArgumentNullException>(() => new GenericShape(null));
#pragma warning restore CS8625
        using var shape = new GenericShape((path, _, _) =>
        {
            path.AddRect(new global::AndroidX.Compose.Rect(0, 0, 30, 40));
            path.Dispose();
            path.Dispose();
            Assert.ThrowsExactly<ObjectDisposedException>(() => path.Close());
        });
        using var outline = Outline(shape, new Size(30, 40), false, 1);
        using var region = Region(outline, 30, 40);
        Assert.IsTrue(region.Contains(15, 20), "Disposing a borrowed view destroyed Kotlin's retained path.");
    }

    /// <summary>Kotlin alone retains the delegate after wrapper disposal; release removes the managed root.</summary>
    [TestMethod]
    public void KotlinRetainedBuilderSurvivesGcAndIsReleased()
    {
        using var roots = new Java.Util.ArrayList();
        var weak = RetainOnlyInKotlin(roots);
        for (int cycle = 0; cycle < 3; cycle++)
        {
            Collect();
            InvokeRetained(roots, weak, cycle + 1);
        }
        roots.Clear();
        for (int attempt = 0; attempt < 20 && IsAlive(weak); attempt++)
        {
            Collect();
            Thread.Sleep(25);
        }
        Assert.IsFalse(IsAlive(weak), "The callback capture leaked after the last Kotlin shape reference was released.");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static WeakReference<ShapeCallbackProbe> RetainOnlyInKotlin(Java.Util.ArrayList roots)
    {
        var probe = new ShapeCallbackProbe();
        using var shape = new GenericShape(probe.Build);
        roots.Add(shape);
        return new WeakReference<ShapeCallbackProbe>(probe);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void InvokeRetained(Java.Util.ArrayList roots, WeakReference<ShapeCallbackProbe> weak, int calls)
    {
        using var peer = roots.Get(0) ?? throw new InvalidOperationException("Native shape root missing.");
        var shape = peer.JavaCast<IShape>();
        using var density = DensityKt.Density(1, 1);
        using var outline = shape.CreateOutline(new Size(80, 40).Packed,
            LayoutDirection.Ltr ?? throw new InvalidOperationException("LTR missing."), density);
        Assert.IsTrue(weak.TryGetTarget(out var probe), "The Kotlin-held callback lost its managed capture.");
        Assert.AreEqual(calls, probe.Calls);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool IsAlive(WeakReference<ShapeCallbackProbe> weak) => weak.TryGetTarget(out _);

    internal static void Collect()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        Java.Lang.JavaSystem.Gc();
        Java.Lang.JavaSystem.RunFinalization();
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    internal static Shape CreateCornerShape(int kind, int variant) => (kind, variant) switch
    {
        (0, 0) => new CutCornerShape(10.Dp()),
        (0, 1) => new CutCornerShape(10),
        (0, 2) => new CutCornerShape(5.Dp(), 10.Dp(), 15.Dp(), 20.Dp()),
        (0, 3) => new CutCornerShape(5, 10, 15, 20),
        (1, 0) => new AbsoluteCutCornerShape(10.Dp()),
        (1, 1) => new AbsoluteCutCornerShape(10),
        (1, 2) => new AbsoluteCutCornerShape(5.Dp(), 10.Dp(), 15.Dp(), 20.Dp()),
        (1, 3) => new AbsoluteCutCornerShape(5, 10, 15, 20),
        (2, 0) => new AbsoluteRoundedCornerShape(10.Dp()),
        (2, 1) => new AbsoluteRoundedCornerShape(10),
        (2, 2) => new AbsoluteRoundedCornerShape(5.Dp(), 10.Dp(), 15.Dp(), 20.Dp()),
        (2, 3) => new AbsoluteRoundedCornerShape(5, 10, 15, 20),
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    internal static Outline Outline(Shape shape, Size size, bool rtl, float scale)
    {
        var native = shape.JavaCast<IShape>();
        using var density = DensityKt.Density(scale, 1);
        var direction = (rtl ? LayoutDirection.Rtl : LayoutDirection.Ltr)
            ?? throw new InvalidOperationException("Native layout direction missing.");
        return native.CreateOutline(size.Packed, direction, density);
    }

    static Region Region(Outline outline, int width, int height)
    {
        using var path = new ComposePath();
        OutlineKt.AddOutline(path.Jvm, outline);
        var native = AndroidPath_androidKt.AsAndroidPath(path.Jvm);
        using var clip = new Region(0, 0, width, height);
        var region = new Region();
        region.SetPath(native, clip);
        GC.KeepAlive(native);
        return region;
    }

    static void AssertCorners(Outline outline, int width, int height, float[] radii, bool rounded)
    {
        using var region = Region(outline, width, height);
        Assert.IsTrue(region.Contains(width / 2, height / 2));
        for (int corner = 0; corner < 4; corner++)
        {
            float radius = radii[corner];
            int checkedPixels = 0;
            for (int y = 0; y <= Math.Ceiling(radius) + 3; y++)
            for (int x = 0; x <= Math.Ceiling(radius) + 3; x++)
            {
                float dx = x + 0.5f, dy = y + 0.5f;
                float distance = rounded
                    ? radius - MathF.Sqrt(MathF.Pow(Math.Max(0, radius - dx), 2) + MathF.Pow(Math.Max(0, radius - dy), 2))
                    : (dx + dy - radius) / MathF.Sqrt(2);
                if (MathF.Abs(distance) < 1.5f)
                    continue; // Exclude only the native integer rasterizer's boundary band.
                int px = corner is 1 or 2 ? width - x - 1 : x;
                int py = corner is 2 or 3 ? height - y - 1 : y;
                Assert.AreEqual(distance >= 0, region.Contains(px, py),
                    $"corner={corner}, radius={radius}, rounded={rounded}, pixel=({px},{py})");
                checkedPixels++;
            }
            Assert.IsTrue(checkedPixels > 10);
        }
    }
}
