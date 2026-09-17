using Microsoft.AndroidX.Compose.Maui.Handlers;
using Xunit;

namespace AndroidX.Compose.SourceGenerators.Tests;

public sealed class CollectionViewportContextTests
{
    [Fact]
    public void RenderItem_PublishesObserverOnlyDuringDeferredRender()
    {
        var observer = new CollectionViewportObserver();
        CollectionViewportObserver? seen = null;

        CollectionViewportContext.RenderItem(
            observer,
            () => seen = CollectionViewportContext.Current);

        Assert.Same(observer, seen);
        Assert.Null(CollectionViewportContext.Current);
    }

    [Fact]
    public void NestedRenderItem_RestoresOuterObserver()
    {
        var outer = new CollectionViewportObserver();
        var inner = new CollectionViewportObserver();

        CollectionViewportContext.RenderItem(
            outer,
            () =>
            {
                Assert.Same(outer, CollectionViewportContext.Current);
                CollectionViewportContext.RenderItem(
                    inner,
                    () => Assert.Same(
                        inner,
                        CollectionViewportContext.Current));
                Assert.Same(outer, CollectionViewportContext.Current);
            });

        Assert.Null(CollectionViewportContext.Current);
    }
}
