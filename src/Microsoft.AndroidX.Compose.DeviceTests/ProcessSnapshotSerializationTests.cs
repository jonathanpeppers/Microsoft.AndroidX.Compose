using System.Text.Json;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Preserves the host probe JSON contracts without reflection-based serialization.</summary>
[TestClass]
public class ProcessSnapshotSerializationTests
{
    /// <summary>Preserves both identity-state dictionaries and the phase marker.</summary>
    [TestMethod]
    public void CompositionIdentity_PreservesHostContract()
    {
        var snapshot = new CompositionIdentityProcessSnapshot(
            "identity", 12, 11, 3, true, false, 15,
            new Dictionary<string, int> { ["child"] = 101 },
            new Dictionary<string, int> { ["child"] = 201 });
        Assert.AreEqual(
            """{"RunId":"identity","ProcessId":12,"PreviousProcessId":11,"TaskId":3,"Restored":true,"Saved":false,"Phase":15,"Values":{"child":101},"OrdinaryValues":{"child":201}}""",
            JsonSerializer.Serialize(snapshot, ProcessSnapshotJsonContext.Default.CompositionIdentityProcessSnapshot));
    }

    /// <summary>Preserves nullable navigation observations before the first composition.</summary>
    [TestMethod]
    public void Navigation_PreservesNullObservations()
    {
        var snapshot = new NavSaveableProcessSnapshot("navigation", 12, 11, 3, true, false, true, null, null);
        Assert.AreEqual(
            """{"RunId":"navigation","ProcessId":12,"PreviousProcessId":11,"TaskId":3,"Factory":true,"Restored":false,"Saved":true,"Value":null,"Label":null}""",
            JsonSerializer.Serialize(snapshot, ProcessSnapshotJsonContext.Default.NavSaveableProcessSnapshot));
    }

    /// <summary>Preserves ordinal ordering of the saveable probe values.</summary>
    [TestMethod]
    public void Saveable_PreservesOrderedValues()
    {
        var snapshot = new SaveableProcessSnapshot("saveable", 12, 11, 3, false, true,
            new SortedDictionary<string, int>(StringComparer.Ordinal) { ["second"] = 102, ["first"] = 101 });
        Assert.AreEqual(
            """{"RunId":"saveable","ProcessId":12,"PreviousProcessId":11,"TaskId":3,"Restored":false,"Saved":true,"Values":{"first":101,"second":102}}""",
            JsonSerializer.Serialize(snapshot, ProcessSnapshotJsonContext.Default.SaveableProcessSnapshot));
    }
}
