using AndroidX.Compose;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Verifies managed value semantics and pager argument contracts.</summary>
[TestClass]
public class PublicValueContractTests
{
    [TestMethod]
    public void DatePickerYearRange_DefaultMatchesKotlinDefault()
    {
        DatePickerYearRange range = default;

        Assert.AreEqual(1900, range.StartYear);
        Assert.AreEqual(2100, range.EndYear);
        Assert.AreEqual(new DatePickerYearRange(1900, 2100), range);
        Assert.IsTrue(range == new DatePickerYearRange(1900, 2100));
        Assert.AreEqual("DatePickerYearRange(1900..2100)", range.ToString());
    }

    [TestMethod]
    public void FocusState_UsesValueSemantics()
    {
        var first = new FocusState(true, true, false);
        var second = new FocusState(true, true, false);

        Assert.AreEqual(first, second);
        Assert.AreEqual(first.GetHashCode(), second.GetHashCode());
        Assert.IsTrue(first == second);
        Assert.IsTrue(first != new FocusState(false, true, false));
        Assert.AreEqual(
            "FocusState(IsFocused=True, HasFocus=True, IsCaptured=False)",
            first.ToString());
    }

    [TestMethod]
    public void Constraints_UsesValueSemanticsWithoutExposingPacking()
    {
        var first = Constraints.Create(1, 20, 3, 40);
        var second = Constraints.Create(1, 20, 3, 40);

        Assert.AreEqual(first, second);
        Assert.AreEqual(first.GetHashCode(), second.GetHashCode());
        Assert.IsTrue(first == second);
        Assert.IsTrue(first != Constraints.Create(1, 21, 3, 40));
        Assert.AreEqual(
            "Constraints(MinWidth=1, MaxWidth=20, MinHeight=3, MaxHeight=40)",
            first.ToString());
    }

    [TestMethod]
    public void BoxConstraints_UsesValueSemantics()
    {
        var first = new BoxConstraints(1, 20, 3, 40);
        var second = new BoxConstraints(1, 20, 3, 40);

        Assert.AreEqual(first, second);
        Assert.AreEqual(first.GetHashCode(), second.GetHashCode());
        Assert.IsTrue(first == second);
        Assert.IsTrue(first != new BoxConstraints(1, 21, 3, 40));
        Assert.AreEqual(
            "BoxConstraints(MinWidth=1, MaxWidth=20, MinHeight=3, MaxHeight=40)",
            first.ToString());
    }

    [TestMethod]
    public void PagerState_RejectsInvalidInitialPage()
    {
        var exception = Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => new PagerState(static () => 1, initialPage: -1));

        Assert.AreEqual("initialPage", exception.ParamName);
    }

    [TestMethod]
    [DataRow(-0.5001f)]
    [DataRow(0.5001f)]
    [DataRow(float.NaN)]
    public void PagerState_RejectsInvalidInitialPageOffsetFraction(float value)
    {
        var exception = Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => new PagerState(static () => 1, initialPageOffsetFraction: value));

        Assert.AreEqual("initialPageOffsetFraction", exception.ParamName);
    }

    [TestMethod]
    public void PagerState_AcceptsInitialPageOffsetFractionBoundaries()
    {
        _ = new PagerState(static () => 1, initialPageOffsetFraction: -0.5f);
        _ = new PagerState(static () => 1, initialPageOffsetFraction: 0.5f);
    }

    [TestMethod]
    public void PagerState_ValidatesPageCountOnlyWhenRequested()
    {
        int invocationCount = 0;
        var state = new PagerState(() =>
        {
            invocationCount++;
            return -1;
        });

        Assert.AreEqual(0, invocationCount);
        var exception = Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => _ = state.PageCount);
        Assert.AreEqual("pageCount", exception.ParamName);
        Assert.AreEqual(1, invocationCount);
    }
}
