using System.Linq;
using Microsoft.AndroidX.Compose.Maui.Handlers;
using Xunit;

namespace AndroidX.Compose.SourceGenerators.Tests;

public sealed class CollectionScrollTrackerTests
{
    [Fact]
    public void InitialSnapshot_DoesNotEmit()
    {
        var tracker = new CollectionScrollTracker(horizontal: false);

        Assert.False(tracker.TryObserve(Snapshot((0, 0), (1, 100)), 2f, out var args));
        Assert.Null(args);
    }

    [Fact]
    public void VerticalScroll_UsesSharedItemOffsetAndDensity()
    {
        var tracker = new CollectionScrollTracker(horizontal: false);
        tracker.TryObserve(Snapshot((0, 0), (1, 100)), 2f, out _);

        Assert.True(tracker.TryObserve(Snapshot((0, -24), (1, 76)), 2f, out var args));
        Assert.NotNull(args);
        Assert.Equal(0d, args.HorizontalDelta);
        Assert.Equal(12d, args.VerticalDelta);
        Assert.Equal(12d, args.VerticalOffset);
        Assert.Equal(0, args.FirstVisibleItemIndex);
        Assert.Equal(1, args.LastVisibleItemIndex);
        Assert.Equal(1, args.CenterItemIndex);
    }

    [Fact]
    public void HorizontalScroll_PreservesSignedDelta()
    {
        var tracker = new CollectionScrollTracker(horizontal: true);
        tracker.TryObserve(Snapshot((3, -10), (4, 90)), 2f, out _);

        Assert.True(tracker.TryObserve(Snapshot((3, 20), (4, 120)), 2f, out var args));
        Assert.NotNull(args);
        Assert.Equal(-15d, args.HorizontalDelta);
        Assert.Equal(0d, args.VerticalDelta);
        Assert.Equal(-15d, args.HorizontalOffset);
    }

    [Fact]
    public void IndexTransition_UsesActualSharedVariableItemOffset()
    {
        var tracker = new CollectionScrollTracker(horizontal: false);
        tracker.TryObserve(Snapshot((0, -90), (1, 10)), 1f, out _);

        Assert.True(tracker.TryObserve(Snapshot((1, -15), (2, 165)), 1f, out var args));
        Assert.NotNull(args);
        Assert.Equal(25d, args.VerticalDelta);
        Assert.Equal(1, args.FirstVisibleItemIndex);
        Assert.Equal(2, args.LastVisibleItemIndex);
    }

    [Fact]
    public void CenterIndex_UsesItemGeometryRatherThanMedianIndex()
    {
        var tracker = new CollectionScrollTracker(horizontal: false);
        tracker.TryObserve(
            Snapshot((0, 0, 180), (1, 180, 20), (2, 200, 20)),
            1f,
            out _);

        Assert.True(tracker.TryObserve(
            Snapshot((0, -10, 180), (1, 170, 20), (2, 190, 20)),
            1f,
            out var args));
        Assert.NotNull(args);
        Assert.Equal(0, args.CenterItemIndex);
    }

    [Fact]
    public void DiscontinuousJumpWithoutSharedItem_DoesNotFabricateDelta()
    {
        var tracker = new CollectionScrollTracker(horizontal: false);
        tracker.TryObserve(Snapshot((0, 0), (1, 100)), 1f, out _);

        Assert.False(tracker.TryObserve(Snapshot((50, 0), (51, 100)), 1f, out var args));
        Assert.Null(args);
    }

    [Fact]
    public void NewTrackerAfterReconnect_SuppressesDuplicateInitialEvent()
    {
        var first = new CollectionScrollTracker(horizontal: false);
        first.TryObserve(Snapshot((2, -20), (3, 80)), 1f, out _);
        first.TryObserve(Snapshot((2, -40), (3, 60)), 1f, out _);

        var reconnected = new CollectionScrollTracker(horizontal: false);
        Assert.False(reconnected.TryObserve(Snapshot((2, -40), (3, 60)), 1f, out var args));
        Assert.Null(args);
    }

    static LazyListScrollSnapshot Snapshot(params (int Index, int Offset)[] items) =>
        Snapshot(items.Select(item => (item.Index, item.Offset, 100)).ToArray());

    static LazyListScrollSnapshot Snapshot(params (int Index, int Offset, int Size)[] items) =>
        new(
            items.Select(item => new LazyListVisibleItemSnapshot(
                item.Index,
                item.Offset,
                item.Size)).ToArray(),
            viewportStart: 0,
            viewportEnd: 200);
}
