using Microsoft.AndroidX.Compose.Maui.Handlers;
using Xunit;

namespace AndroidX.Compose.SourceGenerators.Tests;

public sealed class SwipePanelOrderTests
{
    [Theory]
    [InlineData(1, false)]
    [InlineData(2, true)]
    [InlineData(4, false)]
    [InlineData(8, false)]
    public void OnlyLeftSwipe_ReversesSourceOrder(
        int direction,
        bool expected) =>
        Assert.Equal(expected, SwipePanelOrder.ShouldReverse(direction));
}
