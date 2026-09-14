using Android.Graphics;
using Android.Runtime;
using AndroidX.Compose;
using BoundFont = AndroidX.Compose.UI.Text.Font;
using Font = AndroidX.Compose.Font;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Verifies bound resource fonts, input ownership, real font matching, and typography compatibility.</summary>
[TestClass]
public class ResourceFontTests
{
    [TestMethod]
    public void Resource_ValidatesRequiredInputs()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Font.Resource(0));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Font.Resource(-1));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            Font.Resource(Resource.Font.karla_regular, loadingStrategy: (FontLoadingStrategy)99));
#pragma warning disable CS8625 // Exercise the runtime contract for callers without nullable annotations.
        Assert.ThrowsExactly<ArgumentNullException>(() => FontFamily.FromFonts(null));
#pragma warning restore CS8625
        Assert.ThrowsExactly<ArgumentException>(() => FontFamily.FromFonts());
        Assert.ThrowsExactly<ArgumentException>(() => FontFamily.FromFonts(new BoundFont.IFont[1]));
        var disposed = Font.Resource(Resource.Font.karla_regular);
        disposed.Dispose();
        Assert.ThrowsExactly<ObjectDisposedException>(() => FontFamily.FromFonts(disposed));
    }

    [TestMethod]
    [DataRow(FontLoadingStrategy.Blocking, 0)]
    [DataRow(FontLoadingStrategy.OptionalLocal, 1)]
    [DataRow(FontLoadingStrategy.Async, 2)]
    public void Resource_PreservesMetadata(FontLoadingStrategy strategy, int expectedStrategy)
    {
        using var font = Font.Resource(Resource.Font.karla_bold, FontWeight.Bold, FontStyle.Italic, strategy);
        var resource = font.JavaCast<BoundFont.ResourceFont>();
        Assert.AreEqual(Resource.Font.karla_bold, resource.ResId);
        Assert.AreEqual(700, resource.Weight.Weight);
        Assert.AreEqual(1, resource.Style);
        Assert.AreEqual(expectedStrategy, resource.LoadingStrategy);
        Assert.AreNotEqual(IntPtr.Zero, FontWeight.Bold.Handle);
        Assert.AreNotEqual(IntPtr.Zero, FontStyle.Italic.Handle);
    }

    [TestMethod]
    public void Family_SnapshotsInputsAndDoesNotDisposeSharedPeers()
    {
        using var regular = Font.Resource(Resource.Font.karla_regular);
        using var bold = Font.Resource(Resource.Font.karla_bold, FontWeight.Bold);
        BoundFont.IFont[] fonts = [regular, bold];
        using var family = FontFamily.FromFonts(fonts);
        Assert.AreSame(regular, fonts[0]);
        Assert.AreSame(bold, fonts[1]);
        fonts[0] = bold;

        var list = family.JavaCast<BoundFont.FontListFontFamily>();
        Assert.AreEqual(2, list.Size);
        Assert.AreEqual(Resource.Font.karla_regular, list.Get(0).JavaCast<BoundFont.ResourceFont>().ResId);
        Assert.AreEqual(Resource.Font.karla_bold, list.Get(1).JavaCast<BoundFont.ResourceFont>().ResId);
        family.Dispose();
        Assert.AreNotEqual(IntPtr.Zero, regular.Handle);
        Assert.AreNotEqual(IntPtr.Zero, bold.Handle);
        Assert.AreNotEqual(IntPtr.Zero, FontFamily.Default.Handle);
        Assert.AreNotEqual(IntPtr.Zero, FontFamily.SansSerif.Handle);
        Assert.AreNotEqual(IntPtr.Zero, FontFamily.Serif.Handle);
        Assert.AreNotEqual(IntPtr.Zero, FontFamily.Monospace.Handle);
        Assert.AreNotEqual(IntPtr.Zero, FontFamily.Cursive.Handle);
    }

    [TestMethod]
    public void Resolver_MatchesRealRegularBoldAndItalicAfterDescriptorDisposalAndGc()
    {
        RequireFontRasterSupport();
        using var family = CreateFamilyAndDisposeDescriptors();
        for (int i = 0; i < 8; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Java.Lang.JavaSystem.Gc();
            AssertMatchesResource(family, Resource.Font.karla_regular, FontWeight.Normal, 0);
            AssertMatchesResource(family, Resource.Font.karla_bold, FontWeight.Bold, 0);
            AssertMatchesResource(family, Resource.Font.karla_italic, FontWeight.Normal, 1);
        }
    }

    [TestMethod]
    [DataRow(FontLoadingStrategy.OptionalLocal)]
    [DataRow(FontLoadingStrategy.Async)]
    public async Task Resolver_LoadsSupportedNonBlockingStrategies(FontLoadingStrategy strategy)
    {
        RequireFontRasterSupport();
        using var font = Font.Resource(Resource.Font.montserrat_regular, loadingStrategy: strategy);
        using var family = FontFamily.FromFonts(font);
        var context = global::Android.App.Application.Context;
        using var resolver = BoundFont.FontFamilyResolver_androidKt.CreateFontFamilyResolver(context);
        var state = resolver.Resolve_DPcqOEQ(family.JavaCast<BoundFont.FontFamily>(), BoundWeight(FontWeight.Normal), 0, 0);
        var expected = ExpectedTypeface(Resource.Font.montserrat_regular);
        var pixels = Raster(expected);
        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (DateTime.UtcNow < deadline)
        {
            if (state.Value is Typeface typeface && pixels.SequenceEqual(Raster(typeface)))
                return;
            await Task.Delay(50);
        }
        Assert.Fail($"Resource font did not resolve with {strategy} loading.");
    }

    [TestMethod]
    public void Typography_AndTextStyleRetainFamilyAcrossRepeatedPeerCollection()
    {
        for (int i = 0; i < 128; i++)
        {
            using var font = Font.Resource(Resource.Font.karla_regular);
            using var family = FontFamily.FromFonts(font);
            var style = new global::AndroidX.Compose.TextStyle { FontFamily = family, FontSize = 22 };
            using var typography = MaterialTheme.BuildTypography(bodyLarge: style);
            var boundStyle = typography.BodyLarge;
            Assert.IsTrue(boundStyle.FontFamily?.Equals(family) == true);
            family.Dispose();
            font.Dispose();
            if (i % 16 == 0)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                Java.Lang.JavaSystem.Gc();
            }
            Assert.IsTrue(boundStyle.FontFamily is BoundFont.FontListFontFamily);
            Assert.AreNotEqual(IntPtr.Zero, FontWeight.Normal.Handle);
            Assert.AreNotEqual(IntPtr.Zero, FontStyle.Normal.Handle);
        }
    }

    internal static FontFamily CreateFamilyAndDisposeDescriptors()
    {
        using var normal = Font.Resource(Resource.Font.karla_regular);
        using var bold = Font.Resource(Resource.Font.karla_bold, FontWeight.Bold);
        using var italic = Font.Resource(Resource.Font.karla_italic, style: FontStyle.Italic);
        return FontFamily.FromFonts(normal, bold, italic);
    }

    static BoundFont.FontWeight BoundWeight(FontWeight weight) => weight.JavaCast<BoundFont.FontWeight>();

    internal static void RequireFontRasterSupport()
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(26))
            Assert.Inconclusive("Resource font raster comparisons require Android 26 or later.");
    }

    static void AssertMatchesResource(FontFamily family, int resourceId, FontWeight weight, int style)
    {
        var context = global::Android.App.Application.Context;
        using var resolver = BoundFont.FontFamilyResolver_androidKt.CreateFontFamilyResolver(context);
        var state = resolver.Resolve_DPcqOEQ(family.JavaCast<BoundFont.FontFamily>(), BoundWeight(weight), style, 0);
        var actual = state.Value as Typeface
            ?? throw new InvalidOperationException("Font resolver did not return an Android Typeface.");
        var expected = ExpectedTypeface(resourceId);
        CollectionAssert.AreEqual(Raster(expected), Raster(actual), "Compose selected a different font file or synthesized style.");
        Assert.IsFalse(Raster(actual).SequenceEqual(Raster(Typeface.Default
            ?? throw new InvalidOperationException("Default Typeface unavailable."))), "Resource font silently fell back to the system font.");
    }

    static Typeface ExpectedTypeface(int resourceId)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(26))
            throw new PlatformNotSupportedException("Resource font raster comparisons require Android 26.");
        return global::Android.App.Application.Context.Resources?.GetFont(resourceId)
            ?? throw new InvalidOperationException("Expected resource font did not load.");
    }

    static int[] Raster(Typeface typeface)
    {
        using var bitmap = Bitmap.CreateBitmap(600, 64, Bitmap.Config.Argb8888
            ?? throw new InvalidOperationException("ARGB bitmap configuration unavailable."));
        using var canvas = new global::Android.Graphics.Canvas(bitmap);
        using var paint = new Paint(PaintFlags.AntiAlias) { TextSize = 32 };
        paint.SetTypeface(typeface);
        canvas.DrawText("Hamburgefontsiv AVW 012345", 4, 45, paint);
        var pixels = new int[600 * 64];
        bitmap.GetPixels(pixels, 0, 600, 0, 0, 600, 64);
        return pixels;
    }
}
