using AndroidX.Compose;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Verifies the semantic surface and JNI packing of Compose value types.</summary>
[TestClass]
public class ComposeValueTypeTests
{
    [TestMethod]
    public void Sp_ToStringUsesReadableUnit()
    {
        Assert.AreEqual("16.sp", new Sp(16).ToString());
        Assert.AreEqual("Unspecified", default(Sp).ToString());
        Assert.AreEqual("InvalidSp(0x000000023F800000)", new Sp(0x000000023F800000L).ToString());
    }

    [TestMethod]
    public void TextOverflow_UsesNamedClosedValues()
    {
        Assert.AreEqual("Clip", TextOverflow.Clip.ToString());
        Assert.AreEqual("Ellipsis", TextOverflow.Ellipsis.ToString());
        Assert.AreNotEqual(TextOverflow.Clip, TextOverflow.Ellipsis);
        Assert.AreEqual(TextOverflow.Clip, default(TextOverflow));
        Assert.AreEqual(1, TextOverflow.Pack(default(TextOverflow)));
        Assert.IsNull(typeof(TextOverflow).GetConstructor([typeof(int)]));
    }

    [TestMethod]
    public void TransformOrigin_ExposesFractionsAndPacksForJni()
    {
        var origin = new TransformOrigin(0.25f, 0.75f);
        long x = unchecked((uint)BitConverter.SingleToInt32Bits(0.25f));
        long y = unchecked((uint)BitConverter.SingleToInt32Bits(0.75f));

        Assert.AreEqual(0.25f, origin.PivotFractionX);
        Assert.AreEqual(0.75f, origin.PivotFractionY);
        Assert.AreEqual((x << 32) | y, TransformOrigin.Pack(origin));
        Assert.AreEqual(new TransformOrigin(0.5f, 0.5f), TransformOrigin.Center);
    }

    [TestMethod]
    public void LayoutDimensions_ExposeDpAndPreserveInfinity()
    {
        var constraints = new BoxConstraints(1f, float.PositiveInfinity, 2f, 3f);
        var progress = new CircularProgressIndicator { StrokeWidthDp = 4 };
        var horizontal = new HorizontalDivider { ThicknessDp = 2 };
        var vertical = new VerticalDivider { ThicknessDp = 3 };

        Assert.AreEqual(new Dp(1), constraints.MinWidth);
        Assert.IsTrue(float.IsPositiveInfinity(constraints.MaxWidth.Value));
        Assert.AreEqual(new Dp(2), constraints.MinHeight);
        Assert.AreEqual(new Dp(3), constraints.MaxHeight);
        Assert.AreEqual(new Dp(4), progress.StrokeWidthDp);
        Assert.AreEqual(new Dp(2), horizontal.ThicknessDp);
        Assert.AreEqual(new Dp(3), vertical.ThicknessDp);
        Assert.AreEqual(
            typeof(Dp),
            typeof(MeasureScope).GetMethod(nameof(MeasureScope.RoundToPx))?.GetParameters()[0].ParameterType);
    }
}
