using AndroidX.Compose;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Verifies Compose packed <see cref="Size"/> value semantics.</summary>
[TestClass]
public class SizeTests
{
    [TestMethod]
    public void RoundTrip_PackedRepresentation()
    {
        var size = new Size(123.5f, 48.25f);
        Assert.AreEqual(123.5f, size.Width);
        Assert.AreEqual(48.25f, size.Height);
    }

    [TestMethod]
    public void Equality_SameComponentsCompareEqual()
    {
        var first = new Size(10f, 20f);
        var second = new Size(10f, 20f);
        Assert.AreEqual(first, second);
        Assert.IsTrue(first == second);
    }
}
