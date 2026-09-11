using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace AndroidX.Compose.SourceGenerators.Tests;

public class CompositionOccurrenceRegistryTests
{
    [Fact]
    public void Ordinals_AreScopedByCompositionParentAndFullSite()
    {
        var registry = new CompositionOccurrenceRegistry<object>();
        var first = new object();
        var second = new object();
        Assert.Equal(0, registry.Acquire(first, 1, "Aa"));
        Assert.Equal(1, registry.Acquire(first, 1, "Aa"));
        // Aa and BB collide in Java String.hashCode, but are different lexical identities.
        Assert.Equal(0, registry.Acquire(first, 1, "BB"));
        Assert.Equal(0, registry.Acquire(first, 2, "Aa"));
        Assert.Equal(0, registry.Acquire(second, 1, "Aa"));
        registry.Release(first, 1, "Aa", 0);
        registry.Release(first, 1, "Aa", 1);
        registry.Release(first, 1, "BB", 0);
        registry.Release(first, 2, "Aa", 0);
        Assert.Equal(1, registry.CompositionCount);
        registry.Release(second, 1, "Aa", 0);
        Assert.Equal(0, registry.CompositionCount);
    }

    [Fact]
    public void FreedOrdinals_AreReusedAfterOutOfOrderForgetOrAbandon()
    {
        var pool = new CompositionOccurrencePool();
        Assert.Equal([0, 1, 2, 3], Enumerable.Range(0, 4).Select(_ => pool.Acquire()).ToArray());
        pool.Release(1);
        pool.Release(3);
        Assert.Equal(1, pool.Acquire());
        Assert.Equal(3, pool.Acquire());
        pool.Release(0);
        pool.Release(2);
        pool.Release(1);
        pool.Release(3);
        Assert.Equal(0, pool.Count);
        Assert.Equal(0, pool.Acquire());
        pool.Release(0);
        Assert.Throws<InvalidOperationException>(() => pool.Release(0));
    }

    [Fact]
    public void IndependentCompositions_CanAllocateConcurrentlyAndReleaseAllRoots()
    {
        var registry = new CompositionOccurrenceRegistry<object>();
        Parallel.For(0, 32, _ =>
        {
            var composition = new object();
            for (int i = 0; i < 100; i++)
                Assert.Equal(i, registry.Acquire(composition, 1, "loop"));
            for (int i = 99; i >= 0; i--)
                registry.Release(composition, 1, "loop", i);
        });
        Assert.Equal(0, registry.CompositionCount);
    }
}
