using AndroidX.Compose;
using AndroidX.Compose.Material3;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Checks retained sheet values against the receiving factory's native invariants.</summary>
[TestClass]
[DoNotParallelize]
public class SheetStateTransferDomainTests
{
    /// <summary>Reuses a retired holder with the other sheet factory's constructor flags.</summary>
    [TestMethod]
    [DataRow(false, false, false)]
    [DataRow(false, true, false)]
    [DataRow(true, false, false)]
    [DataRow(true, true, false)]
    [DataRow(false, false, true)]
    [DataRow(false, true, true)]
    [DataRow(true, false, true)]
    [DataRow(true, true, true)]
    public void RetainedValue_IsAcceptedByReceivingFactory(
        bool sourceStandard, bool skipPartiallyExpanded, bool expanded)
    {
        var holder = new SheetStateHolder(skipPartiallyExpanded);
        using var threshold = new ObjectFunction0(() => Java.Lang.Float.ValueOf(56f));
        using var velocity = new ObjectFunction0(() => Java.Lang.Float.ValueOf(125f));
        using var confirm = new SheetValueConfirmStateChange();
        var initialValue = (expanded ? SheetValue.Expanded
            : sourceStandard ? SheetValue.PartiallyExpanded : SheetValue.Hidden)
            ?? throw new InvalidOperationException("Initial native sheet value was unavailable.");
        using var original = new SheetState(
            sourceStandard ? false : skipPartiallyExpanded,
            threshold, velocity, initialValue, confirm, sourceStandard);
        holder.Jvm = original;
        holder.UnbindJvm();
        Assert.IsNull(holder.Jvm);
        Assert.AreEqual(initialValue, holder.CurrentValue, "Retirement changed the retained public value.");
        Assert.AreEqual(initialValue, holder.TargetValue);

        // These are the constructor flags used by the two native remember factories.
        bool targetStandard = !sourceStandard;
        using var successor = new SheetState(
            targetStandard ? false : skipPartiallyExpanded,
            threshold, velocity,
            targetStandard ? holder.RememberStandardValue : holder.RememberValue,
            confirm, targetStandard);
        Assert.AreNotSame(original, successor);
        var expected = expanded || (sourceStandard && skipPartiallyExpanded)
            ? SheetValue.Expanded : SheetValue.PartiallyExpanded;
        Assert.AreEqual(expected, successor.CurrentValue);
        Assert.AreEqual(initialValue, holder.CurrentValue, "Factory normalization changed unbound current value.");
        Assert.AreEqual(initialValue, holder.TargetValue, "Factory normalization changed unbound target value.");
        Assert.AreEqual(initialValue != SheetValue.Hidden, holder.IsVisible);
    }

    /// <summary>New holders keep distinct modal and standard defaults without changing public visibility.</summary>
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void FreshHolder_KeepsFactoryDefaults(bool skipPartiallyExpanded)
    {
        var holder = new SheetStateHolder(skipPartiallyExpanded);
        Assert.AreEqual(SheetValue.Hidden, holder.RememberValue);
        Assert.AreEqual(SheetValue.PartiallyExpanded, holder.RememberStandardValue);
        Assert.AreEqual(SheetValue.Hidden, holder.CurrentValue);
        Assert.AreEqual(SheetValue.Hidden, holder.TargetValue);
        Assert.IsFalse(holder.IsVisible);
    }
}
