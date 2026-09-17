using Microsoft.AndroidX.Compose.Maui.Handlers;
using System.Collections.Generic;
using Xunit;

namespace AndroidX.Compose.SourceGenerators.Tests;

public sealed class ReferenceItemCacheTests
{
    [Fact]
    public void SameItemAndTemplate_ReusesNodeIdentity()
    {
        var cache = new ReferenceItemCache<CachedValue>();
        var item = new object();
        var template = new object();

        var first = cache.GetOrReplace(
            item,
            value => ReferenceEquals(value.Template, template),
            () => new CachedValue(template));
        var second = cache.GetOrReplace(
            item,
            value => ReferenceEquals(value.Template, template),
            () => new CachedValue(template));

        Assert.Same(first, second);
    }

    [Fact]
    public void EqualButDistinctItems_DoNotShareState()
    {
        var cache = new ReferenceItemCache<CachedValue>();
        var firstItem = new EqualItem(1);
        var secondItem = new EqualItem(1);
        var template = new object();

        var first = cache.GetOrReplace(
            firstItem,
            _ => true,
            () => new CachedValue(template));
        var second = cache.GetOrReplace(
            secondItem,
            _ => true,
            () => new CachedValue(template));

        Assert.NotSame(first, second);
    }

    [Fact]
    public void TemplateChange_ReplacesAndDisconnectsOldNode()
    {
        var cache = new ReferenceItemCache<CachedValue>();
        var item = new object();
        var firstTemplate = new object();
        var secondTemplate = new object();
        CachedValue? removed = null;
        var first = cache.GetOrReplace(
            item,
            value => ReferenceEquals(value.Template, firstTemplate),
            () => new CachedValue(firstTemplate));

        var second = cache.GetOrReplace(
            item,
            value => ReferenceEquals(value.Template, secondTemplate),
            () => new CachedValue(secondTemplate),
            value => removed = value);

        Assert.Same(first, removed);
        Assert.NotSame(first, second);
    }

    [Fact]
    public void Prune_RemovesOnlyRowsNoLongerInSource()
    {
        var cache = new ReferenceItemCache<CachedValue>();
        var keep = new object();
        var remove = new object();
        var template = new object();
        var keptValue = cache.GetOrReplace(
            keep,
            _ => true,
            () => new CachedValue(template));
        var removedValue = cache.GetOrReplace(
            remove,
            _ => true,
            () => new CachedValue(template));
        var removed = new List<CachedValue>();

        cache.Prune([keep], removed.Add);

        Assert.Equal([removedValue], removed);
        Assert.Same(
            keptValue,
            cache.GetOrReplace(
                keep,
                _ => true,
                () => new CachedValue(template)));
    }

    sealed record CachedValue(object Template);

    sealed record EqualItem(int Value);
}
