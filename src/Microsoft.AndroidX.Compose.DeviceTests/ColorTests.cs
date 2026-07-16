using ComposeBrush = AndroidX.Compose.Brush;
using ComposeColor = AndroidX.Compose.Color;
using SolidColor = AndroidX.Compose.UI.Graphics.SolidColor;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Verifies typed Compose colors preserve their packed JNI representation.</summary>
[TestClass]
public class ColorTests
{
    [TestMethod]
    public void FromPacked_ToPacked_PreservesAllBits()
    {
        const long packed = unchecked((long)0x89ABCDEF_01234567UL);

        Assert.AreEqual(packed, ComposeColor.FromPacked(packed).ToPacked());
    }

    [TestMethod]
    public void SolidColor_RoundTripsThroughBindingBoundary()
    {
        var color = ComposeColor.FromArgb(0xFF, 0x12, 0x34, 0x56);
        using var brush = ComposeBrush.SolidColor(color);
        var solidColor = (SolidColor)brush;

        Assert.AreEqual(color.ToPacked(), solidColor.Value);
    }
}
