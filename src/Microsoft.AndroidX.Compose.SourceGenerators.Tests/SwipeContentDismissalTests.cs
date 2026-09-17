using Microsoft.AndroidX.Compose.Maui.Handlers;
using Xunit;

namespace AndroidX.Compose.SourceGenerators.Tests;

public sealed class SwipeContentDismissalTests
{
    [Theory]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, true)]
    public void NonSettledContent_PreservesNormalInteraction(
        bool isOpen,
        bool isSettledOpen,
        bool isDragging)
    {
        Assert.False(SwipeContentDismissal.ShouldDismiss(
            isOpen,
            isSettledOpen,
            isDragging));
    }

    [Fact]
    public void SettledOpenContent_ConsumesTapForDismissal()
    {
        Assert.True(SwipeContentDismissal.ShouldDismiss(
            isOpen: true,
            isSettledOpen: true,
            isDragging: false));
    }
}
