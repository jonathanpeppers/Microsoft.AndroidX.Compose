using Xunit;

namespace AndroidX.Compose.SourceGenerators.Tests;

public class ChangedBitsContractTests
{
    [Theory]
    [InlineData(ChangedBits.Uncertain, false)]
    [InlineData(ChangedBits.Same, true)]
    [InlineData(ChangedBits.Different, false)]
    [InlineData(ChangedBits.Static, true)]
    public void OneParameter_ObeysKotlinSkipPredicate(ChangedBits state, bool skips)
    {
        int dirty = (int)state << 1;
        Assert.Equal(skips, (dirty & 0xB) == 0x2);
        Assert.NotEqual(0x2, ((dirty | 1) & 0xB));
    }

    internal static void AssertSkipContract(int mask, int expected, int count)
    {
        // Exercise every Same/Static combination against the emitted predicate,
        // then replace each individual slot with states that must execute.
        for (int combination = 0; combination < (1 << count); combination++)
        {
            int dirty = 0;
            for (int slot = 0; slot < count; slot++)
                dirty |= (int)((combination & (1 << slot)) == 0
                    ? ChangedBits.Same : ChangedBits.Static) << (1 + slot * 3);
            Assert.Equal(expected, dirty & mask);
            Assert.NotEqual(expected, (dirty | 1) & mask);
            for (int slot = 0; slot < count; slot++)
            {
                int shift = 1 + slot * 3;
                int cleared = dirty & ~(0b111 << shift);
                Assert.NotEqual(expected, (cleared | ((int)ChangedBits.Uncertain << shift)) & mask);
                Assert.NotEqual(expected, (cleared | ((int)ChangedBits.Different << shift)) & mask);
                // Kotlin Unknown, formerly mislabelled Static, is not skippable.
                Assert.NotEqual(expected, (cleared | (0b100 << shift)) & mask);
            }
        }
    }
}
