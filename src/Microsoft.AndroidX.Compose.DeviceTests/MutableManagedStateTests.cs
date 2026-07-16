using AndroidX.Compose;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Verifies managed state mutation and synchronization behavior.</summary>
[TestClass]
public class MutableManagedStateTests
{
    [TestMethod]
    public void Value_ExposesInitialAndAssignedValues()
    {
        var state = new MutableManagedState<string>("initial");

        Assert.AreEqual("initial", state.Value);

        state.Value = "updated";

        Assert.AreEqual("updated", state.Value);
    }

    [TestMethod]
    public void TrySet_ReportsWhetherValueChanged()
    {
        var state = new MutableManagedState<int>(1);

        Assert.IsFalse(state.TrySet(1));
        Assert.IsTrue(state.TrySet(2));
        Assert.AreEqual(2, state.Value);
    }

    [TestMethod]
    public void Update_ReturnsAndStoresTransformedValue()
    {
        var state = new MutableManagedState<int>(2);

        var result = state.Update(static value => value * 3);

        Assert.AreEqual(6, result);
        Assert.AreEqual(6, state.Value);
    }

    [TestMethod]
    public void ConcurrentUpdates_DoNotLoseWrites()
    {
        const int updateCount = 100;
        var state = new MutableManagedState<int>(0);

        Parallel.For(0, updateCount, _ => state.Update(static value => value + 1));

        Assert.AreEqual(updateCount, state.Value);
    }
}
