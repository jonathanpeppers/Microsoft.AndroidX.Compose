using Microsoft.AndroidX.Compose.Maui.Handlers;
using Xunit;

namespace AndroidX.Compose.SourceGenerators.Tests;

public sealed class ViewportObserverBindingTests
{
    [Fact]
    public void AmbientNullRecomposition_RetainsBoundObserver()
    {
        var bound = new CollectionViewportObserver();

        var resolved = ViewportObserverBinding.Resolve(
            current: null,
            bound);

        Assert.Same(bound, resolved);
    }

    [Fact]
    public void NewParentObserver_ReplacesBoundObserver()
    {
        var bound = new CollectionViewportObserver();
        var current = new CollectionViewportObserver();

        var resolved = ViewportObserverBinding.Resolve(current, bound);

        Assert.Same(current, resolved);
    }
}
