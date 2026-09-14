using AndroidX.Compose;
using TextUnit = AndroidX.Compose.UI.Unit.TextUnit;
using TextUnitKt = AndroidX.Compose.UI.Unit.TextUnitKt;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Verifies the semantic surface and JNI packing of Compose value types.</summary>
[TestClass]
public class ComposeValueTypeTests
{
    [TestMethod]
    [DataRow(0.5f)]
    [DataRow(0.1f)]
    [DataRow(16.25f)]
    [DataRow(0f)]
    [DataRow(-0.5f)]
    [DataRow(-16.25f)]
    public void Sp_FractionalValuesPreservePackedFloat(float value)
    {
        long expected = 0x0000000100000000L | unchecked((uint)BitConverter.SingleToInt32Bits(value));
        var sp = new Sp(value);

        Assert.AreEqual(expected, TextUnitKt.GetSp(value));
        Assert.AreEqual(expected, sp.PackedValue);
        Assert.AreEqual(expected, Sp.Pack(sp));
        Assert.AreEqual(sp, value.Sp());
        Assert.AreEqual(sp, new Sp(expected));
        Assert.AreEqual(sp.GetHashCode(), new Sp(expected).GetHashCode());
        Assert.AreEqual(value, BitConverter.Int32BitsToSingle(unchecked((int)sp.PackedValue)));
    }

    [TestMethod]
    public void Sp_NumericEdgesMatchKotlinWithoutNormalization()
    {
        float[] values =
        [
            -0f, float.Epsilon, -float.Epsilon, float.MaxValue, float.MinValue,
            float.PositiveInfinity, float.NegativeInfinity, float.NaN,
            BitConverter.Int32BitsToSingle(0x7FC01234),
        ];
        foreach (float value in values)
        {
            long expected = 0x0000000100000000L | unchecked((uint)BitConverter.SingleToInt32Bits(value));
            Assert.AreEqual(expected, TextUnitKt.GetSp(value));
            Assert.AreEqual(expected, new Sp(value).PackedValue);
            Assert.AreEqual(expected, value.Sp().PackedValue);
        }

        Assert.AreEqual(new Sp(0f), Sp.Zero);
        Assert.AreNotEqual(new Sp(-0f), Sp.Zero);
        Assert.IsTrue(new Sp(-0f) < Sp.Zero);
        Assert.AreNotEqual(default(Sp), Sp.Zero);
        Assert.AreEqual(0L, Sp.Pack(null));
    }

    [TestMethod]
    public void Sp_ExistingIntegerAndPackedLongContractsAreUnchanged()
    {
        int[] integers = [0, 16, -16, int.MinValue, int.MaxValue, 16777217];
        foreach (int value in integers)
        {
            Sp converted = value;
            long expected = TextUnitKt.GetSp(value);
            Assert.AreEqual(expected, converted.PackedValue);
            Assert.AreEqual(expected, new Sp(value).PackedValue);
            Assert.AreEqual(expected, value.Sp().PackedValue);
        }

        long[] packedValues = [0L, 16L, 0x000000023F800000L, long.MinValue, long.MaxValue];
        foreach (long packed in packedValues)
            Assert.AreEqual(packed, new Sp(packedValue: packed).PackedValue);

        uint unsigned = 16;
        Assert.AreEqual(16L, new Sp(unsigned).PackedValue);
        Assert.AreEqual(new Sp(16), new Sp(16f));
    }

    [TestMethod]
    public void Sp_FractionalArithmeticEqualityAndOrderingUseKotlinSemantics()
    {
        var small = 0.1f.Sp();
        var large = 0.5f.Sp();
        Assert.IsTrue(small != large);
        Assert.IsTrue(large == new Sp(0.5f));
        Assert.IsTrue(large.Equals((object)new Sp(0.5f)));
        Assert.IsFalse(large.Equals((object)0.5f));
        Assert.IsTrue(small < large);
        Assert.IsTrue(large > small);
        Assert.IsTrue(small <= large);
        Assert.IsTrue(large >= small);
        Assert.AreEqual(0, large.CompareTo(0.5f.Sp()));
        Assert.AreEqual(0.25f.Sp(), large / 2f);
        Assert.AreEqual(1.25f.Sp(), large * 2.5f);
        Assert.AreEqual(1.25f.Sp(), 2.5f * large);
        Assert.AreEqual((-0.5f).Sp(), -large);
        Assert.AreEqual(float.PositiveInfinity.Sp(), large / 0f);
        Assert.AreEqual(
            TextUnit.CompareTo(float.NaN.Sp().PackedValue, large.PackedValue),
            float.NaN.Sp().CompareTo(large));
        Assert.ThrowsExactly<Java.Lang.IllegalArgumentException>(() => large.CompareTo(default));
        Assert.ThrowsExactly<Java.Lang.IllegalArgumentException>(() => large.CompareTo(new Sp(0x000000023F800000L)));
    }

    [TestMethod]
    public void Sp_FractionalTypographySurvivesTextStyleAndSpanInterop()
    {
        using var style = new TextStyle
        {
            FontSize = new Sp(16.25f),
            LetterSpacing = (-0.5f).Sp(),
            LineHeight = 24.75f.Sp(),
        }.Build();
        Assert.AreEqual(16.25f.Sp().PackedValue, style.FontSize);
        Assert.AreEqual((-0.5f).Sp().PackedValue, style.LetterSpacing);
        Assert.AreEqual(24.75f.Sp().PackedValue, style.LineHeight);

        using var span = new SpanStyle { FontSize = 12.5f.Sp(), LetterSpacing = 0.1f.Sp() }.Build();
        Assert.AreEqual(12.5f.Sp().PackedValue, span.FontSize);
        Assert.AreEqual(0.1f.Sp().PackedValue, span.LetterSpacing);

        using var typography = MaterialTheme.BuildTypography(
            labelSmall: new TextStyle { FontSize = 11, LineHeight = 16, LetterSpacing = 0.5f.Sp() },
            labelLarge: new TextStyle { FontSize = 14, LineHeight = 20, LetterSpacing = 0.1f.Sp() });
        Assert.AreEqual(0.5f.Sp().PackedValue, typography.LabelSmall.LetterSpacing);
        Assert.AreEqual(0.1f.Sp().PackedValue, typography.LabelLarge.LetterSpacing);
    }

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
