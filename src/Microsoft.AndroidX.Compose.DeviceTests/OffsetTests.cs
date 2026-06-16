using AndroidX.Compose;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>
/// Verifies <see cref="Offset"/> packing math and JNI <c>Offset.Unbox</c>
/// null-handling. The packed-long representation matches Kotlin's
/// <c>androidx.compose.ui.geometry.Offset</c> wire format; we exercise
/// both the round-trip and the <c>null</c> short-circuit so that a
/// <c>boxed</c> arg from <c>OffsetCallback</c> never NPEs the JNI cast.
/// </summary>
[TestClass]
public class OffsetTests
{
    [TestMethod]
    public void Unbox_Null_ReturnsZero()
    {
        Assert.AreEqual(Offset.Zero, Offset.Unbox(null));
    }

    [TestMethod]
    public void Zero_HasZeroComponents()
    {
        var z = Offset.Zero;
        Assert.AreEqual(0f, z.X);
        Assert.AreEqual(0f, z.Y);
    }

    [TestMethod]
    public void RoundTrip_PackedRepresentation()
    {
        var a = new Offset(12.5f, -7.25f);
        Assert.AreEqual(12.5f, a.X);
        Assert.AreEqual(-7.25f, a.Y);
    }

    [TestMethod]
    public void Equality_SameComponentsCompareEqual()
    {
        var a = new Offset(3f, 4f);
        var b = new Offset(3f, 4f);
        Assert.AreEqual(a, b);
        Assert.IsTrue(a == b);
        Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
    }

    [TestMethod]
    public void Equality_DifferentComponentsCompareUnequal()
    {
        var a = new Offset(3f, 4f);
        var b = new Offset(3f, 5f);
        Assert.AreNotEqual(a, b);
        Assert.IsTrue(a != b);
    }

    [TestMethod]
    public void Infinite_HasPositiveInfinityComponents()
    {
        var inf = Offset.Infinite;
        Assert.AreEqual(float.PositiveInfinity, inf.X);
        Assert.AreEqual(float.PositiveInfinity, inf.Y);
    }
}
