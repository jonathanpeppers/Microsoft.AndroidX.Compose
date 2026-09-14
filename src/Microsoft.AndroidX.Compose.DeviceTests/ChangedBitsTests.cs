using AndroidX.Compose;
using AndroidX.Compose.Runtime;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>
/// Verifies <see cref="ChangedBits"/> values match the Kotlin
/// compose-compiler's 3-bit codes (<c>0b000</c>/<c>0b001</c>/<c>0b010</c>/
/// <c>0b011</c>) and that <see cref="ComposeExtensions.DiffSlotShift"/>
/// returns the bit position used by the per-param <c>DiffSlot</c>
/// emission in <c>ComposeFacadeGenerator</c>. A regression here would
/// silently mis-align every <c>$changed</c> slot the facade emits.
/// </summary>
[TestClass]
public class ChangedBitsTests
{
    [TestMethod]
    [DataRow(ChangedBits.Uncertain, false)]
    [DataRow(ChangedBits.Same, true)]
    [DataRow(ChangedBits.Different, false)]
    [DataRow(ChangedBits.Static, true)]
    public void States_ObeyKotlinSkipContractAtEverySupportedShift(ChangedBits state, bool skips)
    {
        for (int slot = 0; slot < 10; slot++)
        {
            int shift = ComposeExtensions.DiffSlotShift(slot);
            int dirty = (int)state << shift;
            int mask = 1 | (0b101 << shift);
            int expected = 0b001 << shift;
            Assert.AreEqual(skips, (dirty & mask) == expected);
            Assert.AreNotEqual(expected, (dirty | 1) & mask);
            Assert.AreEqual(0, dirty & 1);
        }
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

    [TestMethod]
    public void DiffSlot_RealSlotTableTracksNullAndValueChangesAcrossAllTenPositions()
    {
        using var applier = new IdentityTestApplier();
        using var recomposer = new Recomposer(Kotlin.Coroutines.EmptyCoroutineContext.Instance
            ?? throw new InvalidOperationException("Empty coroutine context is unavailable."));
        var composition = CompositionKt.ControlledComposition(applier, recomposer);
        try
        {
            string?[] values = [null, null, "changed", "changed", null];
            ChangedBits[] expected = [ChangedBits.Different, ChangedBits.Same,
                ChangedBits.Different, ChangedBits.Same, ChangedBits.Different];
            for (int pass = 0; pass < values.Length; pass++)
            {
                int[] contributions = new int[10];
                composition.ComposeContent(new ComposableLambda2(c =>
                {
                    for (int slot = 0; slot < 10; slot++)
                        contributions[slot] = c.DiffSlot(values[pass], ComposeExtensions.DiffSlotShift(slot));
                }));
                composition.ApplyChanges();
                for (int slot = 0; slot < 10; slot++)
                    Assert.AreEqual((int)expected[pass] << (1 + slot * 3), contributions[slot]);
            }
        }
        finally
        {
            composition.Dispose();
        }
    }
}
