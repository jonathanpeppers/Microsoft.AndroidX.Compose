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

    [Theory]
    [InlineData(-1d, 100f)]
    [InlineData(60d, 100f)]
    [InlineData(180d, 180f)]
    public void CustomWidth_HonorsRootRequestAboveMinimum(
        double request,
        float expected)
    {
        Assert.Equal(expected, SwipePanelSizing.CustomWidth(request, 100f));
    }

    [Theory]
    [InlineData(true, true, false, true)]
    [InlineData(true, true, true, false)]
    [InlineData(true, false, false, false)]
    [InlineData(false, true, false, false)]
    public void ExecutionWidth_RequiresHorizontalMenuOnlyPanel(
        bool horizontal,
        bool executeMode,
        bool hasCustomItem,
        bool expected)
    {
        Assert.Equal(expected, SwipePanelSizing.UsesExecutionWidth(
            horizontal,
            executeMode,
            hasCustomItem));
    }
}
