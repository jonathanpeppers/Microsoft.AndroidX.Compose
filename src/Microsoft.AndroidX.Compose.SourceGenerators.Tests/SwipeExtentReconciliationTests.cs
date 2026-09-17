using Microsoft.AndroidX.Compose.Maui.Handlers;
using Xunit;

namespace AndroidX.Compose.SourceGenerators.Tests;

public sealed class SwipeExtentReconciliationTests
{
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void ActiveMotion_DoesNotSnapOpen(bool isDragging, bool isAnimating)
    {
        var result = SwipeExtentReconciliationPolicy.Resolve(
            isOpen: true,
            isSettledOpen: false,
            isDragging,
            isAnimating,
            extent: 100f);

        Assert.Equal(SwipeExtentReconciliation.None, result);
    }

    [Fact]
    public void SettledOpenRow_SnapsToChangedExtent()
    {
        var result = SwipeExtentReconciliationPolicy.Resolve(
            isOpen: true,
            isSettledOpen: true,
            isDragging: false,
            isAnimating: false,
            extent: 200f);

        Assert.Equal(SwipeExtentReconciliation.SnapOpen, result);
    }

    [Fact]
    public void EmptyActiveSide_ClosesDuringMotion()
    {
        var result = SwipeExtentReconciliationPolicy.Resolve(
            isOpen: true,
            isSettledOpen: false,
            isDragging: true,
            isAnimating: false,
            extent: 0f);

        Assert.Equal(SwipeExtentReconciliation.Close, result);
    }
}
