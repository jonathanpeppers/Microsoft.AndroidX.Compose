using Android.Runtime;
using AndroidX.Compose;
using AndroidX.Compose.Material3;
using AndroidX.Compose.Runtime;
using Color = AndroidX.Compose.Color;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Checks native Surface defaults, drawing, live option transitions, and saved state.</summary>
[TestClass]
[DoNotParallelize]
public class SurfaceStylingTests
{
    /// <summary>Preserves the original constructor, catalog method groups, and compiled direct target.</summary>
    [TestMethod]
    public void OriginalClrSignaturesRemainAvailable()
    {
        Assert.IsNotNull(typeof(Surface).GetConstructor(Type.EmptyTypes));
        var implicitMethod = typeof(Composables).GetMethod(nameof(Composables.Surface),
            [typeof(Action), typeof(Modifier), typeof(Shape)]);
        Action<IComposer, Action<IComposer>, Modifier?, Shape?> explicitMethod = Composables.Surface;
        Action<IComposer, Action, Modifier?, Shape?, ulong, int> directMethod =
            Composables.Surface_PrimaryResource_Implicit;
        Assert.IsNotNull(implicitMethod);
        Assert.AreEqual(3, implicitMethod.GetParameters().Length);
        Assert.AreEqual(4, explicitMethod.Method.GetParameters().Length);
        Assert.AreEqual(6, directMethod.Method.GetParameters().Length);
    }

    /// <summary>Checks real optional argument omission through both generated catalog overloads.</summary>
    [TestMethod]
    [DataRow(1, false)] [DataRow(1, true)]
    [DataRow(2, false)] [DataRow(2, true)]
    [DataRow(3, false)] [DataRow(3, true)]
    public async Task LiteralOmissionUsesMaterialDefaults(int style, bool dark)
    {
        SurfaceStylingTestActivity.Prepare(style, dark, literalDefaults: true);
        var activity = await Start();
        try
        {
            var snapshot = await activity.ReadAtIdle();
            AssertState(activity, snapshot, style, 0, 0);
            await AssertPixels(activity, snapshot, style, dark);
        }
        finally { await Finish(activity); }
    }

    /// <summary>Changes omission at one live helper/tree position and checks committed native results.</summary>
    [TestMethod]
    [DataRow(0, false)] [DataRow(0, true)]
    [DataRow(1, false)] [DataRow(1, true)]
    [DataRow(2, false)] [DataRow(2, true)]
    [DataRow(4, false)] [DataRow(4, true)]
    public async Task StylingTransitionsRetainStateAndRestore(int style, bool dark)
    {
        SurfaceStylingTestActivity.Prepare(style, dark);
        var activity = await Start();
        try
        {
            var first = await activity.ReadAtIdle();
            AssertState(activity, first, style, 0, 0);
            await activity.OnUi(() =>
            {
                first.Counter.Value = 37;
                first.OutsideCounter.Value = 91;
            });
            var seeded = await activity.ReadAtIdle();
            Assert.AreEqual(37, seeded.CounterValue);
            Assert.AreEqual(91, seeded.OutsideCounterValue);
            int[] modes = [7, 0, 6, 0, 1, 0, 2, 5, 4, 0, 3, 0];
            foreach (int mode in modes)
            {
                int generation = 0;
                await activity.OnUi(() => generation = activity.Change(mode));
                var actual = await activity.ReadAtIdle();
                bool sameLambda = JNIEnv.IsSameObject(
                    ((Java.Lang.Object)first.Content).Handle, ((Java.Lang.Object)actual.Content).Handle);
                bool sameCounterPeer = JNIEnv.IsSameObject(
                    ((Java.Lang.Object)first.CounterPeer).Handle, ((Java.Lang.Object)actual.CounterPeer).Handle);
                string trace = $"style={style}, dark={dark}, generation={actual.Generation}, mode={actual.Mode}, "
                    + $"defaults={actual.Defaults}, nativeDefaults={actual.NativeDefaults}, changed={actual.Changed}, "
                    + $"nativeColor={actual.NativeColor:X16}, nativeContent={actual.NativeContentColor:X16}, "
                    + $"localContent={actual.ContentColor:X16}, absoluteElevation={actual.AbsoluteElevation}, "
                    + $"sameBody={ReferenceEquals(first.Sentinel, actual.Sentinel)}, "
                    + $"sameTail={ReferenceEquals(first.TailSentinel, actual.TailSentinel)}, "
                    + $"sameCounter={ReferenceEquals(first.Counter, actual.Counter)}, sameCounterPeer={sameCounterPeer}, "
                    + $"outsideValue={actual.OutsideCounterValue}, sameOutside={ReferenceEquals(first.OutsideCounter, actual.OutsideCounter)}, "
                    + $"sameLambda={sameLambda}";
                Console.WriteLine(trace);
                AssertState(activity, actual, style, mode, generation);
                Assert.AreEqual(37, actual.CounterValue, trace);
                Assert.AreEqual(91, actual.OutsideCounterValue, trace);
                Assert.AreSame(first.OutsideCounter, actual.OutsideCounter, trace);
                Assert.IsTrue(sameCounterPeer, trace);
                Assert.AreSame(first.Sentinel, actual.Sentinel, "Body remember moved when options changed.");
                Assert.AreSame(first.TailSentinel, actual.TailSentinel, "A conditional helper DiffSlot shifted the trailing remember.");
                Assert.AreSame(first.Counter, actual.Counter);
                Assert.IsTrue(sameLambda, "The tracked Surface content lambda changed native identity.");
                await AssertPixels(activity, actual, style, dark);
            }
            int[] themeModes = [0, 7];
            bool[] palettes = [!dark, dark];
            foreach (int mode in themeModes)
            {
                await activity.OnUi(() => activity.Change(mode));
                foreach (bool palette in palettes)
                {
                    int generation = 0;
                    await activity.OnUi(() => generation = activity.ChangePalette(palette));
                    var themed = await activity.ReadAtIdle();
                    AssertState(activity, themed, style, mode, generation);
                    Assert.AreEqual(37, themed.CounterValue);
                    Assert.AreEqual(91, themed.OutsideCounterValue);
                    Assert.AreSame(first.Sentinel, themed.Sentinel);
                    Assert.AreSame(first.Counter, themed.Counter);
                    Assert.IsTrue(JNIEnv.IsSameObject(
                        ((Java.Lang.Object)first.CounterPeer).Handle, ((Java.Lang.Object)themed.CounterPeer).Handle));
                    Assert.IsTrue(JNIEnv.IsSameObject(
                        ((Java.Lang.Object)first.Content).Handle, ((Java.Lang.Object)themed.Content).Handle));
                    await AssertPixels(activity, themed, style, palette);
                }
            }
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Java.Lang.JavaSystem.Gc();
            await activity.OnUi(() => activity.Change(1));
            var collected = await activity.ReadAtIdle();
            Assert.AreSame(first.Sentinel, collected.Sentinel);
            Assert.AreEqual(37, collected.CounterValue);

            var previous = activity;
            SurfaceStylingTestActivity.PrepareRecreation();
            await previous.OnUi(() =>
            {
                previous.ExpectEnd();
                previous.Recreate();
            });
            activity = await SurfaceStylingTestActivity.Started.Task.WaitAsync(TimeSpan.FromSeconds(10));
            await previous.Destroyed.Task.WaitAsync(TimeSpan.FromSeconds(10));
            var restored = await activity.ReadAtIdle();
            Assert.AreEqual(37, restored.CounterValue, "Surface child saveable value did not restore.");
            Assert.AreEqual(91, restored.OutsideCounterValue, "Outside Surface saveable value did not restore.");
            Assert.AreNotSame(first.Counter, restored.Counter);
            Assert.AreNotSame(first.Sentinel, restored.Sentinel);
            AssertState(activity, restored, style, 1, 0);
            await activity.OnUi(() =>
            {
                restored.Counter.Value = 38;
                activity.Change(0);
            });
            var afterRestore = await activity.ReadAtIdle();
            Assert.AreEqual(38, afterRestore.CounterValue);
            Assert.AreSame(restored.Sentinel, afterRestore.Sentinel);
            AssertState(activity, afterRestore, style, 0, 1);
        }
        finally { await Finish(activity); }
    }

    static void AssertState(SurfaceStylingTestActivity activity, SurfaceStylingSnapshot actual,
        int style, int mode, int generation)
    {
        var scheme = activity.Scheme ?? throw new InvalidOperationException("Surface theme unavailable.");
        bool zero = mode == 2 || (mode == 3 && style != 0);
        long expectedContent = mode is 1 or 6 ? Color.White.ToPacked()
            : mode is 4 or 7 ? scheme.Secondary : zero ? 0 : scheme.OnSurface;
        int expectedDefaults = style == 4 ? 3 : mode switch
        {
            1 => 2,
            2 => style == 0 ? 67 : 3,
            3 => style == 0 ? 127 : 3,
            4 => 122,
            5 => 78,
            6 => 119,
            7 => 123,
            _ => 127,
        };
        Assert.AreEqual(generation, actual.Generation, "An obsolete render was observed at native idle.");
        Assert.AreEqual(mode, actual.Mode);
        Assert.AreEqual(expectedDefaults, actual.Defaults, "Incorrect Kotlin default bitmask.");
        Assert.AreEqual(expectedDefaults & ~(int)(SurfaceDefault.Color | SurfaceDefault.ContentColor),
            actual.NativeDefaults, "Surface color defaults were not normalized at the bound call.");
        Assert.AreEqual(mode is 1 or 4 or 7 ? SurfaceStylingTestActivity.CustomColor.ToPacked()
            : zero ? 0L : scheme.Surface, actual.NativeColor);
        Assert.AreEqual(expectedContent, actual.NativeContentColor);
        Assert.AreEqual(0, actual.Changed, "Resolved theme colors require a conservative native changed mask.");
        Assert.AreEqual(expectedContent, actual.ContentColor, "Content color did not inherit the expected Material role.");
        Assert.AreEqual(mode is 1 or 5 ? 10f : 2f, actual.AbsoluteElevation,
            "Nested Surface tonal elevations must accumulate, including explicit zero.");
    }

    static async Task AssertPixels(SurfaceStylingTestActivity activity, SurfaceStylingSnapshot snapshot,
        int style, bool dark)
    {
        var instrumentation = TestInstrumentation.Current
            ?? throw new InvalidOperationException("Surface instrumentation unavailable.");
        var automation = instrumentation.UiAutomation
            ?? throw new InvalidOperationException("Native UI automation unavailable.");
        await Task.Run(() => automation.WaitForIdle(100, 5000));
        using var bitmap = automation.TakeScreenshot()
            ?? throw new InvalidOperationException("Native Surface screenshot unavailable.");
        (int X, int Y) background = default, border = default, shadow = default;
        await activity.OnUi(() =>
        {
            background = activity.Pixel(100, 80);
            border = activity.Pixel(1, 50);
            shadow = activity.Pixel(201, 90);
        });
        var scheme = activity.Scheme ?? throw new InvalidOperationException("Surface theme unavailable.");
        bool zero = snapshot.Mode == 2 || (snapshot.Mode == 3 && style != 0);
        long expected = snapshot.Mode is 1 or 4 or 7 ? SurfaceStylingTestActivity.CustomColor.ToPacked()
            : zero ? SurfaceStylingTestActivity.Backdrop.ToPacked()
            : ColorSchemeKt.SurfaceColorAtElevation(scheme, snapshot.AbsoluteElevation);
        Assert.AreEqual(Argb(expected), bitmap.GetPixel(background.X, background.Y),
            "Native Surface fill did not match its supplied color/elevation.");
        if (snapshot.Mode == 1)
            Assert.AreEqual(Argb(SurfaceStylingTestActivity.BorderColor.ToPacked()),
                bitmap.GetPixel(border.X, border.Y), "The supplied border was not drawn.");
        else
            Assert.AreEqual(Argb(expected), bitmap.GetPixel(border.X, border.Y),
                "Border pixels remained after removing the border.");
        if (snapshot.Mode is 1 or 5)
            Assert.AreNotEqual(Argb(SurfaceStylingTestActivity.Backdrop.ToPacked()),
                bitmap.GetPixel(shadow.X, shadow.Y), "Nonzero shadow elevation did not draw outside the Surface.");
        else
            Assert.AreEqual(Argb(SurfaceStylingTestActivity.Backdrop.ToPacked()),
                bitmap.GetPixel(shadow.X, shadow.Y), "Removed shadow still darkened the backdrop.");

        if (snapshot.Mode is 0 or 1 or 5)
        {
            string directory = activity.GetExternalFilesDir("surface-styling")?.AbsolutePath
                ?? throw new InvalidOperationException("Surface screenshot directory unavailable.");
            string path = System.IO.Path.Combine(directory, $"style-{style}-dark-{dark}-mode-{snapshot.Mode}.png");
            using var file = File.Create(path);
            Assert.IsTrue(bitmap.Compress(global::Android.Graphics.Bitmap.CompressFormat.Png
                ?? throw new InvalidOperationException("PNG format unavailable."), 100, file));
        }
    }

    static int Argb(long packed) => unchecked((int)(packed >> 32));

    static async Task<SurfaceStylingTestActivity> Start()
    {
        var instrumentation = TestInstrumentation.Current
            ?? throw new InvalidOperationException("Surface instrumentation unavailable.");
        var launched = await Task.Run(() =>
        {
            using var intent = new global::Android.Content.Intent(Application.Context, typeof(SurfaceStylingTestActivity));
            intent.AddFlags(global::Android.Content.ActivityFlags.NewTask);
            return instrumentation.StartActivitySync(intent);
        }).WaitAsync(TimeSpan.FromSeconds(10));
        var activity = await SurfaceStylingTestActivity.Started.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.IsNotNull(launched);
        Assert.IsTrue(JNIEnv.IsSameObject(launched.Handle, activity.Handle));
        return activity;
    }

    static async Task Finish(SurfaceStylingTestActivity activity)
    {
        await activity.OnUi(() =>
        {
            activity.ExpectEnd();
            activity.Finish();
        });
        await activity.Destroyed.Task.WaitAsync(TimeSpan.FromSeconds(10));
    }
}
