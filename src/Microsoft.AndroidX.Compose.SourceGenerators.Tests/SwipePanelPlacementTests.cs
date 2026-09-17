using Microsoft.AndroidX.Compose.Maui.Handlers;
using Xunit;

namespace AndroidX.Compose.SourceGenerators.Tests;

public sealed class SwipePanelPlacementTests
{
    [Theory]
    [InlineData(1, SwipePanelPlacement.LeftItems)]
    [InlineData(2, SwipePanelPlacement.RightItems)]
    [InlineData(8, SwipePanelPlacement.TopItems)]
    [InlineData(4, SwipePanelPlacement.BottomItems)]
    public void ActiveDirection_PlacesOnlyMatchingPanel(
        int direction,
        int expectedPanel) =>
        Assert.Equal(
            expectedPanel,
            SwipePanelPlacement.ActivePanelIndex(direction));

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(16)]
    public void ClosedOrInvalidDirection_PlacesNoPanel(int direction) =>
        Assert.Equal(
            SwipePanelPlacement.None,
            SwipePanelPlacement.ActivePanelIndex(direction));

    [Fact]
    public void LeftItemsRoute_DoesNotPlaceBottomInteractivePanel()
    {
        int placed = SwipePanelPlacement.ActivePanelIndex(1);

        Assert.Equal(SwipePanelPlacement.LeftItems, placed);
        Assert.NotEqual(SwipePanelPlacement.BottomItems, placed);
    }

    [Fact]
    public void ContentAndDismissOverlay_KeepStableSiblingIndices()
    {
        Assert.Equal(4, SwipePanelPlacement.Content);
        Assert.Equal(5, SwipePanelPlacement.DismissOverlay);
    }
}
