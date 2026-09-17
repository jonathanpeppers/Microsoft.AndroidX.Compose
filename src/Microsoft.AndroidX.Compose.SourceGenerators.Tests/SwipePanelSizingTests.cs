using Microsoft.AndroidX.Compose.Maui.Handlers;
using Xunit;

namespace AndroidX.Compose.SourceGenerators.Tests;

public sealed class SwipePanelSizingTests
{
    [Theory]
    [InlineData(true, true, false)]
    [InlineData(false, false, false)]
    [InlineData(false, true, true)]
    public void IntrinsicSizing_IsOnlyForVerticalCustomPanels(
        bool horizontal,
        bool hasCustomItem,
        bool expected)
    {
        Assert.Equal(expected, SwipePanelSizing.UsesIntrinsicVerticalSize(
            horizontal,
            hasCustomItem));
    }

    [Theory]
    [InlineData(-1d, null)]
    [InlineData(72d, 72f)]
    public void RequestedHeight_UsesMauiUnsetSentinel(
        double request,
        float? expected)
    {
        Assert.Equal(expected, SwipePanelSizing.RequestedCustomHeight(request));
    }
}
