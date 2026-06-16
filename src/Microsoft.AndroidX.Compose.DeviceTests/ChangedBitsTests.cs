using AndroidX.Compose;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>
/// Verifies <see cref="ChangedBits"/> values match the Kotlin
/// compose-compiler's 3-bit codes (<c>0b000</c>/<c>0b001</c>/<c>0b010</c>/
/// <c>0b100</c>) and that <see cref="ComposeExtensions.DiffSlotShift"/>
/// returns the bit position used by the per-param <c>DiffSlot</c>
/// emission in <c>ComposeFacadeGenerator</c>. A regression here would
/// silently mis-align every <c>$changed</c> slot the facade emits.
/// </summary>
[TestClass]
public class ChangedBitsTests
{
    [TestMethod]
    public void EnumValues_MatchKotlinWireFormat()
    {
        Assert.AreEqual(0, (int)ChangedBits.Uncertain);
        Assert.AreEqual(1, (int)ChangedBits.Same);
        Assert.AreEqual(2, (int)ChangedBits.Different);
        Assert.AreEqual(4, (int)ChangedBits.Static);
    }

    [TestMethod]
    public void DiffSlotShift_Param0_BitOne()
    {
        Assert.AreEqual(1, ComposeExtensions.DiffSlotShift(0));
    }

    [TestMethod]
    public void DiffSlotShift_Param1_BitFour()
    {
        Assert.AreEqual(4, ComposeExtensions.DiffSlotShift(1));
    }

    [TestMethod]
    public void DiffSlotShift_ThreeBitStrideMatchesParamIndex()
    {
        for (int i = 0; i < 10; i++)
            Assert.AreEqual(1 + i * 3, ComposeExtensions.DiffSlotShift(i));
    }
}
