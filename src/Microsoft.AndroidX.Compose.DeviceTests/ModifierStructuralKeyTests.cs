using AndroidX.Compose;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>
/// Verifies <see cref="Modifier.StructuralKey"/> and
/// <see cref="ModifierOpKey"/> structural equality — the diff input the
/// facade generator passes to <c>composer.DiffSlot</c> for the modifier
/// slot. If two semantically-equal chains hash differently the entire
/// <c>$changed</c>-skip story collapses for any subtree carrying a
/// <see cref="Modifier"/>.
/// </summary>
[TestClass]
public class ModifierStructuralKeyTests
{
    [TestMethod]
    public void EmptyModifier_StructuralKeyIsEmpty()
    {
        var key = Modifier.Companion.StructuralKey;
        Assert.AreEqual(0, key.Count);
    }

    [TestMethod]
    public void EmptyModifier_TwoCallsCompareEqual()
    {
        var a = Modifier.Companion.StructuralKey;
        var b = Modifier.Companion.StructuralKey;
        Assert.AreEqual(a, b);
        Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
    }

    [TestMethod]
    public void Padding_SameArgsCompareEqual()
    {
        var a = Modifier.Padding(16).StructuralKey;
        var b = Modifier.Padding(16).StructuralKey;
        Assert.AreEqual(a, b);
        Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
    }

    [TestMethod]
    public void Padding_DifferentArgsCompareUnequal()
    {
        var a = Modifier.Padding(16).StructuralKey;
        var b = Modifier.Padding(24).StructuralKey;
        Assert.AreNotEqual(a, b);
    }

    [TestMethod]
    public void FillMaxWidth_SameFractionCompareEqual()
    {
        var a = Modifier.FillMaxWidth().StructuralKey;
        var b = Modifier.FillMaxWidth().StructuralKey;
        Assert.AreEqual(a, b);
    }

    [TestMethod]
    public void Chain_SameOpsInOrderCompareEqual()
    {
        var a = Modifier.Padding(16).FillMaxWidth().StructuralKey;
        var b = Modifier.Padding(16).FillMaxWidth().StructuralKey;
        Assert.AreEqual(a, b);
        Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
    }

    [TestMethod]
    public void Chain_DifferentOrderCompareUnequal()
    {
        var a = Modifier.Padding(16).FillMaxWidth().StructuralKey;
        var b = Modifier.FillMaxWidth().Padding(16).StructuralKey;
        Assert.AreNotEqual(a, b);
    }

    [TestMethod]
    public void Then_ConcatenatesKeys()
    {
        var a = Modifier.Padding(16).Then(Modifier.FillMaxWidth()).StructuralKey;
        var b = Modifier.Padding(16).FillMaxWidth().StructuralKey;
        Assert.AreEqual(a, b);
    }

    [TestMethod]
    public void OpaqueKeysAreNeverEqual()
    {
        // Two fresh opaque keys must compare unequal — that's the whole
        // point of the unkeyed Append() overload's conservative fallback.
        var a = ModifierOpKey.Opaque;
        var b = ModifierOpKey.Opaque;
        Assert.AreNotEqual(a, b);
    }

    [TestMethod]
    public void NamedKey_SameNameSameArgsCompareEqual()
    {
        var a = new ModifierOpKey(nameof(Modifier.StatusBarsPadding), null);
        var b = new ModifierOpKey(nameof(Modifier.StatusBarsPadding), null);
        Assert.AreEqual(a, b);
        Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
    }
}
