using Microsoft.AndroidX.Compose.Maui.Handlers;
using Xunit;

namespace AndroidX.Compose.SourceGenerators.Tests;

public sealed class ReferenceOccurrenceKeysTests
{
    [Fact]
    public void DuplicateSameReference_GetsDistinctStableKeys()
    {
        var cache = new ReferenceOccurrenceKeys();
        var item = new object();

        var first = cache.GetKeys([item, item]);
        var second = cache.GetKeys([item, item]);

        Assert.NotSame(first[0], first[1]);
        Assert.NotEqual(first[0].Value, first[1].Value);
        Assert.Same(first[0], second[0]);
        Assert.Same(first[1], second[1]);
    }

    [Fact]
    public void MoveAroundOtherRows_PreservesOccurrenceKeys()
    {
        var cache = new ReferenceOccurrenceKeys();
        var duplicate = new object();
        var other = new object();
        var first = cache.GetKeys([duplicate, duplicate, other]);

        var moved = cache.GetKeys([other, duplicate, duplicate]);

        Assert.Same(first[0], moved[1]);
        Assert.Same(first[1], moved[2]);
        Assert.Same(first[2], moved[0]);
    }

    [Fact]
    public void RemovedOccurrence_IsNotReusedAfterTrim()
    {
        var cache = new ReferenceOccurrenceKeys();
        var item = new object();
        var first = cache.GetKeys([item, item]);
        cache.GetKeys([item]);

        var restored = cache.GetKeys([item, item]);

        Assert.Same(first[0], restored[0]);
        Assert.NotSame(first[1], restored[1]);
        Assert.NotEqual(first[1].Value, restored[1].Value);
    }
}
