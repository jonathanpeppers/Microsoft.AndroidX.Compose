using System.Collections.Generic;
using System.Text;
using Xunit;

namespace AndroidX.Compose.SourceGenerators.Tests;

public class KotlinDefaultMaskPlanTests
{
    [Theory]
    [InlineData("enabled")]
    [InlineData("shape")]
    [InlineData("icon")]
    [InlineData("fontSize")]
    public void SurfacedParameter_ClearsGeneratedBitOnlyWhenNotOmitted(string kotlinName)
    {
        var plan = CreatePlan(
            "WidgetDefault",
            [new DefaultsSlot(kotlinName, 0, Pascal(kotlinName))],
            [new DefaultArgumentBinding(kotlinName, 3)]);

        var emitted = Emit(plan);

        Assert.Contains("var __defaults = global::AndroidX.Compose.WidgetDefault.All;", emitted);
        Assert.Contains(
            $"if ((__omittedArguments & 0x8UL) == 0) __defaults &= ~global::AndroidX.Compose.WidgetDefault.{Pascal(kotlinName)};",
            emitted);
        Assert.Equal(["(int)__defaults"], plan.ArgumentExpressions("__defaults"));
    }

    [Fact]
    public void SuppressedAndUnsurfacedSlots_RemainInInitialAllMask()
    {
        var plan = CreatePlan(
            "DialogDefault",
            [
                new DefaultsSlot("required", 0, null),
                new DefaultsSlot("icon", 1, "Icon"),
                new DefaultsSlot("title", 2, "Title"),
            ],
            [
                new DefaultArgumentBinding("required", 0),
                new DefaultArgumentBinding("title", 1),
            ]);

        var emitted = Emit(plan);

        Assert.DoesNotContain(".Required", emitted);
        Assert.DoesNotContain(".Icon", emitted);
        Assert.Contains(".Title;", emitted);
    }

    [Fact]
    public void BranchRoutes_BuildIndependentMasks()
    {
        var primary = CreatePlan(
            "TopAppBarDefault",
            [
                new DefaultsSlot("title", 0, null),
                new DefaultsSlot("navigationIcon", 1, "NavigationIcon"),
            ],
            [new DefaultArgumentBinding("navigationIcon", 1)]);
        var alternate = CreatePlan(
            "TopAppBarWithSubtitleDefault",
            [
                new DefaultsSlot("title", 0, null),
                new DefaultsSlot("subtitle", 1, "Subtitle"),
                new DefaultsSlot("navigationIcon", 2, "NavigationIcon"),
            ],
            [
                new DefaultArgumentBinding("subtitle", 1),
                new DefaultArgumentBinding("navigationIcon", 2),
            ]);

        var primarySource = Emit(primary, "__primaryDefaults");
        var alternateSource = Emit(alternate, "__alternateDefaults");

        Assert.Contains("TopAppBarDefault.NavigationIcon", primarySource);
        Assert.DoesNotContain("Subtitle", primarySource);
        Assert.Contains("TopAppBarWithSubtitleDefault.Subtitle", alternateSource);
        Assert.Contains("TopAppBarWithSubtitleDefault.NavigationIcon", alternateSource);
    }

    [Fact]
    public void SecondaryRoute_UsesItsOwnDiscriminatorAndEnum()
    {
        var primary = CreatePlan(
            "IconPainterDefault",
            [new DefaultsSlot("painter", 0, null), new DefaultsSlot("tint", 1, "Tint")],
            [new DefaultArgumentBinding("tint", 2)]);
        var secondary = CreatePlan(
            "IconImageVectorDefault",
            [new DefaultsSlot("imageVector", 0, "ImageVector"), new DefaultsSlot("tint", 1, "Tint")],
            [
                new DefaultArgumentBinding("imageVector", 0),
                new DefaultArgumentBinding("tint", 2),
            ]);

        var primarySource = Emit(primary, "__primaryDefaults");
        var secondarySource = Emit(secondary, "__secondaryDefaults");

        Assert.DoesNotContain("ImageVector", primarySource);
        Assert.Contains("IconImageVectorDefault.ImageVector", secondarySource);
        Assert.Contains("IconImageVectorDefault.Tint", secondarySource);
    }

    [Fact]
    public void WideMask_UsesGeneratedSplitHelper()
    {
        var slots = new List<DefaultsSlot>();
        for (int i = 0; i < 40; i++)
            slots.Add(new DefaultsSlot("slot" + i, i, "Slot" + i));
        var plan = CreatePlan(
            "ColorSchemeDefault",
            slots,
            [new DefaultArgumentBinding("slot39", 39)]);

        var emitted = Emit(plan);

        Assert.True(plan.IsWide);
        Assert.Contains("ColorSchemeDefault.Slot39", emitted);
        Assert.Contains(
            "var (__defaultsMask0, __defaultsMask1) = __defaults.Split();",
            emitted);
        Assert.Equal(
            ["__defaultsMask0", "__defaultsMask1"],
            plan.ArgumentExpressions("__defaults"));
        Assert.DoesNotContain(">> 32", emitted);
        Assert.DoesNotContain("0xFFFFFFFF", emitted);
    }

    [Fact]
    public void ThirtyTwoSlots_UsesSingleJvmMask()
    {
        var slots = new List<DefaultsSlot>();
        for (int i = 0; i < 32; i++)
            slots.Add(new DefaultsSlot("slot" + i, i, "Slot" + i));
        var plan = CreatePlan(
            "ThirtyTwoDefault",
            slots,
            [new DefaultArgumentBinding("slot31", 31)]);

        var emitted = Emit(plan);

        Assert.False(plan.IsWide);
        Assert.DoesNotContain(".Split()", emitted);
        Assert.Equal(["unchecked((int)__defaults)"], plan.ArgumentExpressions("__defaults"));
    }

    [Fact]
    public void InvalidBindings_AreRejected()
    {
        var defaults = new DefaultsInfo(
            "WidgetDefault",
            [new DefaultsSlot("enabled", 0, "Enabled")]);

        Assert.Throws<System.InvalidOperationException>(() =>
            KotlinDefaultMaskPlan.Create(
                defaults,
                [new DefaultArgumentBinding("missing", 0)]));
        Assert.Throws<System.ArgumentOutOfRangeException>(() =>
            KotlinDefaultMaskPlan.Create(
                defaults,
                [new DefaultArgumentBinding("enabled", 64)]));
    }

    [Fact]
    public void MoreThan64KotlinSlots_IsRejected()
    {
        var slots = new List<DefaultsSlot>();
        for (int i = 0; i < 65; i++)
            slots.Add(new DefaultsSlot("slot" + i, i, "Slot" + i));

        Assert.Throws<System.NotSupportedException>(() =>
            KotlinDefaultMaskPlan.Create(
                new DefaultsInfo("TooWideDefault", slots),
                []));
    }

    static KotlinDefaultMaskPlan CreatePlan(
        string enumName,
        IReadOnlyList<DefaultsSlot> slots,
        IReadOnlyList<DefaultArgumentBinding> bindings) =>
        KotlinDefaultMaskPlan.Create(new DefaultsInfo(enumName, slots), bindings);

    static string Emit(
        KotlinDefaultMaskPlan plan,
        string maskVariable = "__defaults")
    {
        var sb = new StringBuilder();
        plan.EmitInitialization(sb, "", "__omittedArguments", maskVariable);
        return sb.ToString();
    }

    static string Pascal(string value) =>
        char.ToUpperInvariant(value[0]) + value.Substring(1);
}
