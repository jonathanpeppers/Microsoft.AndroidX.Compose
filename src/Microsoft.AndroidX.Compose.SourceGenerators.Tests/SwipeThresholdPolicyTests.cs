using Microsoft.AndroidX.Compose.Maui.Handlers;
using Xunit;

namespace AndroidX.Compose.SourceGenerators.Tests;

public sealed class SwipeThresholdPolicyTests
{
    [Fact]
    public void UnsetThreshold_UsesSixtyPercentOfMeasuredPanel()
    {
        Assert.InRange(
            SwipeThresholdPolicy.ResolveOpenDistancePixels(
                thresholdDp: 0d,
                density: 2f,
                panelExtentPixels: 400f),
            239.99f,
            240.01f);
    }

    [Fact]
    public void ExplicitThreshold_ConvertsDpWithoutChangingPanelExtent()
    {
        Assert.Equal(
            200f,
            SwipeThresholdPolicy.ResolveOpenDistancePixels(
                thresholdDp: 100d,
                density: 2f,
                panelExtentPixels: 400f));
    }

    [Fact]
    public void ThresholdLargerThanPanel_RequiresFullReveal()
    {
        Assert.Equal(
            400f,
            SwipeThresholdPolicy.ResolveOpenDistancePixels(
                thresholdDp: 300d,
                density: 2f,
                panelExtentPixels: 400f));
    }
}
