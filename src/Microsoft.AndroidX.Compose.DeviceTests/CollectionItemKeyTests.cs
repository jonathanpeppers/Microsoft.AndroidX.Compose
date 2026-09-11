using AndroidX.Compose;
using Android.OS;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Verifies value-returning keys, snapshot consistency, and fail-fast validation.</summary>
[TestClass]
public class CollectionItemKeyTests
{
    [TestMethod]
    public void KeysHaveJavaValueEqualityAndSurviveParcelRoundTrip()
    {
        object[] values = ["", "record-42", int.MinValue, int.MaxValue, long.MinValue, long.MaxValue, 42, 42L, "42"];
        var (_, adapter) = CollectionItemKey.Create(values, static value => value);
        using var key = adapter ?? throw new InvalidOperationException("Key adapter was not created.");
        for (int i = 0; i < values.Length; i++)
        {
            using var index = OwnedIndex(i);
            // ValueOf results may share a peer; only dispose independently owned inputs.
            var first = key.Invoke(index);
            var second = key.Invoke(index);
            Assert.IsTrue(first.Equals(second), "Repeated key calls must compare equal in Kotlin.");
            Assert.AreEqual(first.GetHashCode(), second.GetHashCode());
            using var parcel = Parcel.Obtain()
                ?? throw new InvalidOperationException("Parcel.Obtain returned null.");
            parcel.WriteValue(first);
            parcel.SetDataPosition(0);
            var restored = parcel.ReadValue(Java.Lang.ClassLoader.SystemClassLoader);
            Assert.IsTrue(first.Equals(restored), "Key must survive Android saved-state serialization.");
        }
    }

    [TestMethod]
    public void NullSelectorKeepsOriginalCollectionAndNoCallback()
    {
        IReadOnlyList<int> items = [1, 2, 3];
        var snapshot = CollectionItemKey.Create(items, key: null);
        Assert.AreSame(items, snapshot.Items);
        Assert.IsNull(snapshot.Key);
    }

    [TestMethod]
    public void SnapshotKeepsKeysAndItemsTogetherAndInvokesSelectorOncePerItem()
    {
        List<string> items = ["A", "B"];
        int calls = 0;
        var (snapshot, adapter) = CollectionItemKey.Create(items, item => { calls++; return item; });
        using var key = adapter ?? throw new InvalidOperationException("Key adapter was not created.");
        items.Insert(0, "X");
        using var index = OwnedIndex(1);
        using var boxed = key.Invoke(index);
        Assert.AreEqual("B", snapshot[1]);
        Assert.AreEqual("B", boxed.ToString());
        Assert.AreEqual(2, calls);
    }

    [TestMethod]
    public void InvalidAndDuplicateResultsFailBeforeComposition()
    {
        int[] arrayKey = [1];
        object[] unsupported = [1.0, 1f, true, Guid.Empty, new object(), arrayKey];
        foreach (object value in unsupported)
        {
            var ex = Assert.ThrowsExactly<ArgumentException>(() =>
                CollectionItemKey.Create<object>([value], static item => item));
            StringAssert.Contains(ex.Message, "index 0");
            StringAssert.Contains(ex.Message, "string, int, or long");
        }

        // Deliberately violate the selector's non-null return contract.
#pragma warning disable CS8603
        var nullError = Assert.ThrowsExactly<ArgumentException>(() =>
            CollectionItemKey.Create<int>([1], static _ => null));
#pragma warning restore CS8603
        StringAssert.Contains(nullError.Message, "got null");

        var duplicateError = Assert.ThrowsExactly<ArgumentException>(() =>
            CollectionItemKey.Create<string>(["A", "B", "A"], static item => item));
        StringAssert.Contains(duplicateError.Message, "indices 0 and 2");
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            CollectionItemKey.Create<int>([1], static _ => throw new InvalidOperationException("Selector failure.")));
    }

    [TestMethod]
    public void PagerCount_PublishesWithKeySnapshotAndRestoresUnkeyedLiveBehavior()
    {
        int count = 2;
        var state = new PagerState(() => count);
        Assert.AreEqual(2, state.PageCount);
        state.SetRenderedPageCount(2);
        count = 3;
        Assert.AreEqual(2, state.PageCount, "Count must not overtake the rendered key snapshot.");
        state.SetRenderedPageCount(3);
        Assert.AreEqual(3, state.PageCount);
        count = 4;
        state.SetRenderedPageCount(null);
        Assert.AreEqual(4, state.PageCount);
        Assert.ThrowsExactly<InvalidOperationException>(() => state.SetRenderedPageCount(3));
        count = -1;
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = state.PageCount);
    }

    [TestMethod]
    public void InvalidCallbackIndicesFailClearly()
    {
        var (_, adapter) = CollectionItemKey.Create<int>([1], static item => item);
        using var key = adapter ?? throw new InvalidOperationException("Key adapter was not created.");
        Assert.ThrowsExactly<ArgumentException>(() => key.Invoke(null));
        using var wrongType = new Java.Lang.String("0");
        Assert.ThrowsExactly<ArgumentException>(() => key.Invoke(wrongType));
        using var negative = OwnedIndex(-1);
        using var pastEnd = OwnedIndex(1);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => key.Invoke(negative));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => key.Invoke(pastEnd));
    }

    // The deprecated constructor guarantees ownership; ValueOf can share a live peer with another test.
#pragma warning disable CA1422
    static Java.Lang.Integer OwnedIndex(int value) => new(value);
#pragma warning restore CA1422
}
