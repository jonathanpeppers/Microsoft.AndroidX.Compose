using System;
using System.Linq;
using Microsoft.AndroidX.Compose.Maui.Handlers;
using Xunit;

namespace AndroidX.Compose.SourceGenerators.Tests;

public sealed class CollectionViewportObserverTests
{
    [Fact]
    public void InitialSnapshot_DoesNotNotify()
    {
        var observer = new CollectionViewportObserver();

        Assert.False(observer.HasSignificantChange(
            Snapshot((0, 0), (1, 100)), 2f));
    }

    [Fact]
    public void SnapshotString_RoundTripsVisibleGeometry()
    {
        var snapshot = LazyListScrollSnapshot.Parse("3,-17,120;4,103,80");

        Assert.Equal(
            [
                new LazyListVisibleItemSnapshot(3, -17, 120),
                new LazyListVisibleItemSnapshot(4, 103, 80),
            ],
            snapshot.VisibleItems);
    }

    [Fact]
    public void SnapshotString_RejectsMalformedEntry()
    {
        Assert.Throws<FormatException>(() =>
            LazyListScrollSnapshot.Parse("3,-17"));
    }

    [Theory]
    [InlineData(-24, true)]
    [InlineData(24, true)]
    [InlineData(-20, false)]
    [InlineData(20, false)]
    public void SharedItem_UsesAbsoluteDpThreshold(
        int newOffset,
        bool expected)
    {
        var observer = new CollectionViewportObserver();
        observer.HasSignificantChange(Snapshot((0, 0), (1, 100)), 2f);

        Assert.Equal(expected, observer.HasSignificantChange(
            Snapshot((0, newOffset), (1, 100 + newOffset)), 2f));
    }

    [Fact]
    public void VariableSizeIndexTransition_UsesSharedItemOffset()
    {
        var observer = new CollectionViewportObserver();
        observer.HasSignificantChange(
            Snapshot((0, -90, 100), (1, 10, 180)), 1f);

        Assert.True(observer.HasSignificantChange(
            Snapshot((1, -15, 180), (2, 165, 20)), 1f));
    }

    [Fact]
    public void DiscontinuousJumpWithoutSharedItem_Notifies()
    {
        var observer = new CollectionViewportObserver();
        observer.HasSignificantChange(Snapshot((0, 0), (1, 100)), 1f);

        Assert.True(observer.HasSignificantChange(
            Snapshot((50, 0), (51, 100)), 1f));
    }

    [Fact]
    public void RepeatedSubthresholdMovement_DoesNotAccumulate()
    {
        var observer = new CollectionViewportObserver();
        observer.HasSignificantChange(Snapshot((0, 0), (1, 100)), 1f);

        for (int offset = -4; offset >= -40; offset -= 4)
        {
            Assert.False(observer.HasSignificantChange(
                Snapshot((0, offset), (1, 100 + offset)), 1f));
        }
    }

    [Fact]
    public void IdenticalDiscontinuousViewport_DoesNotNotify()
    {
        var observer = new CollectionViewportObserver();
        observer.HasSignificantChange(Snapshot((0, 0), (1, 100)), 1f);
        observer.HasSignificantChange(Snapshot((50, 0), (51, 100)), 1f);

        Assert.False(observer.HasSignificantChange(
            Snapshot((50, 0), (51, 100)), 1f));
    }

    [Fact]
    public void RegistrationReplacementAndRemoval_AvoidDuplicates()
    {
        var observer = new CollectionViewportObserver();
        int calls = 0;
        Action callback = () => calls++;

        observer.Register(callback);
        observer.Register(callback);
        observer.NotifySignificantChange();
        Assert.Equal(1, calls);

        observer.Unregister(callback);
        observer.NotifySignificantChange();
        Assert.Equal(1, calls);
    }

    [Fact]
    public void Observers_AreIsolatedPerCollection()
    {
        var first = new CollectionViewportObserver();
        var second = new CollectionViewportObserver();
        int firstCalls = 0;
        int secondCalls = 0;
        first.Register(() => firstCalls++);
        second.Register(() => secondCalls++);

        first.NotifySignificantChange();

        Assert.Equal(1, firstCalls);
        Assert.Equal(0, secondCalls);
    }

    [Fact]
    public void ActiveOwner_RemainsNotifiedAcrossForcedCollection()
    {
        var observer = new CollectionViewportObserver();
        var owner = new ViewportListenerOwner();
        observer.Register(owner.Callback);

        ForceCollection();
        observer.NotifySignificantChange();

        Assert.Equal(1, owner.Calls);
        GC.KeepAlive(owner);
    }

    [Fact]
    public void Rebind_RemovesOldObserverAndAvoidsDuplicateNotifications()
    {
        var first = new CollectionViewportObserver();
        var second = new CollectionViewportObserver();
        var owner = new ViewportListenerOwner();
        first.Register(owner.Callback);

        first.Unregister(owner.Callback);
        second.Register(owner.Callback);
        first.NotifySignificantChange();
        second.NotifySignificantChange();

        Assert.Equal(1, owner.Calls);
    }

    [Fact]
    public void UnregisteredOwner_CanBeCollectedAndIsNotNotified()
    {
        var observer = new CollectionViewportObserver();
        var ownerReference = RegisterThenUnregister(observer);

        ForceCollection();
        observer.NotifySignificantChange();

        Assert.False(ownerReference.IsAlive);
    }

    [Fact]
    public void ResetTracking_SuppressesReconnectInitialNotification()
    {
        var observer = new CollectionViewportObserver();
        observer.HasSignificantChange(Snapshot((0, 0), (1, 100)), 1f);
        observer.HasSignificantChange(Snapshot((0, -20), (1, 80)), 1f);

        observer.ResetTracking();

        Assert.False(observer.HasSignificantChange(
            Snapshot((0, -20), (1, 80)), 1f));
    }

    static LazyListScrollSnapshot Snapshot(params (int Index, int Offset)[] items) =>
        Snapshot(items.Select(item => (item.Index, item.Offset, 100)).ToArray());

    static LazyListScrollSnapshot Snapshot(
        params (int Index, int Offset, int Size)[] items) =>
        new(items.Select(item => new LazyListVisibleItemSnapshot(
            item.Index,
            item.Offset,
            item.Size)).ToArray());

    static WeakReference RegisterThenUnregister(
        CollectionViewportObserver observer)
    {
        var owner = new ViewportListenerOwner();
        observer.Register(owner.Callback);
        observer.Unregister(owner.Callback);
        return new WeakReference(owner);
    }

    static void ForceCollection()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }
}
