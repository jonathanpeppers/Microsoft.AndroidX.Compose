using Microsoft.AndroidX.Compose.Maui.Handlers;
using Xunit;

namespace AndroidX.Compose.SourceGenerators.Tests;

public sealed class SwipeDragLifecycleTests
{
    [Fact]
    public void Cancel_AllowsNextGestureToStart()
    {
        var lifecycle = new SwipeDragLifecycle();

        Assert.True(lifecycle.Begin());
        lifecycle.Cancel();

        Assert.True(lifecycle.Begin());
        Assert.True(lifecycle.End());
    }

    [Fact]
    public void EndingInactiveGesture_DoesNotEmitCompletion()
    {
        var lifecycle = new SwipeDragLifecycle();

        Assert.False(lifecycle.End());
    }
}
