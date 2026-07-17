using AndroidX.Compose;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Verifies managed slider-range semantics and Kotlin interop.</summary>
[TestClass]
public class FloatRangeTests
{
    [TestMethod]
    public void Default_IsComposeSliderRange()
    {
        FloatRange range = default;

        Assert.AreEqual(0f, range.Start);
        Assert.AreEqual(1f, range.End);
        Assert.AreEqual(new FloatRange(0f, 1f), range);
        Assert.AreEqual("FloatRange(0..1)", range.ToString());
    }

    [TestMethod]
    public void Equality_UsesEndpoints()
    {
        var first = new FloatRange(-2.5f, 4.25f);
        var second = new FloatRange(-2.5f, 4.25f);

        Assert.AreEqual(first, second);
        Assert.AreEqual(first.GetHashCode(), second.GetHashCode());
        Assert.IsTrue(first == second);
        Assert.IsTrue(first != new FloatRange(-2.5f, 5f));
        Assert.AreEqual("FloatRange(-2.5..4.25)", first.ToString());
    }

    [TestMethod]
    public void Constructor_ValidatesFiniteAscendingEndpoints()
    {
        Assert.AreEqual(
            "start",
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(
                () => new FloatRange(float.NaN, 1f)).ParamName);
        Assert.AreEqual(
            "start",
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(
                () => new FloatRange(float.NegativeInfinity, 1f)).ParamName);
        Assert.AreEqual(
            "end",
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(
                () => new FloatRange(0f, float.PositiveInfinity)).ParamName);
        Assert.AreEqual(
            "end",
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(
                () => new FloatRange(2f, 1f)).ParamName);

        Assert.AreEqual(new FloatRange(1f, 1f), new FloatRange(1f, 1f));
    }

    [TestMethod]
    public void KotlinConversion_AndCallback_RoundTrip()
    {
        var expected = new FloatRange(-3.5f, 7.25f);
        using var kotlin = expected.ToKotlin();

        Assert.AreEqual(expected, FloatRangeInterop.FromKotlin(kotlin));

        FloatRange? callbackValue = null;
        var callback = FloatRangeInterop.WrapCallback(value => callbackValue = value);
        callback.Invoke((Java.Lang.Object)kotlin);

        Assert.AreEqual(expected, callbackValue);
    }
}
