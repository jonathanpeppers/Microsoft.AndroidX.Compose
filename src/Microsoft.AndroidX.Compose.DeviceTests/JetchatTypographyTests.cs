using AndroidX.Compose;
using AndroidX.Compose.Samples.Jetchat;
using AndroidX.Compose.Samples.Jetchat.Theme;
using NativeTextStyle = AndroidX.Compose.UI.Text.TextStyle;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Checks the actual sample's pinned text metrics across the Kotlin style boundary.</summary>
[TestClass]
public class JetchatTypographyTests
{
    [TestMethod]
    public void ThemeSlotsMatchPinnedJetchatMetricsAndFamilies()
    {
        var karla = JetchatFonts.Karla;
        var montserrat = JetchatFonts.Montserrat;
        using var typography = Typography.CreateJetchatTypography(karla, montserrat);
        using var defaults = MaterialTheme.BuildTypography(bodyLarge: new TextStyle());
        var defaultStyle = defaults.BodyLarge;
        (NativeTextStyle Style, int Size, int Height, float Spacing, int Weight, FontFamily Family)[] slots =
        [
            (typography.DisplayLarge, 57, 64, 0f, 300, montserrat),
            (typography.DisplayMedium, 45, 52, 0f, 300, montserrat),
            (typography.DisplaySmall, 36, 44, 0f, 400, montserrat),
            (typography.HeadlineLarge, 32, 40, 0f, 600, montserrat),
            (typography.HeadlineMedium, 28, 36, 0f, 600, montserrat),
            (typography.HeadlineSmall, 24, 32, 0f, 600, montserrat),
            (typography.TitleLarge, 22, 28, 0f, 600, montserrat),
            (typography.TitleMedium, 16, 24, 0.15f, 600, montserrat),
            (typography.TitleSmall, 14, 20, 0.1f, 700, karla),
            (typography.BodyLarge, 16, 24, 0.15f, 400, karla),
            (typography.BodyMedium, 14, 20, 0.25f, 500, montserrat),
            (typography.BodySmall, 12, 16, 0.4f, 700, karla),
            (typography.LabelLarge, 14, 20, 0.1f, 600, montserrat),
            (typography.LabelMedium, 12, 16, 0.5f, 600, montserrat),
            (typography.LabelSmall, 11, 16, 0.5f, 600, montserrat),
        ];
        foreach (var slot in slots)
        {
            Assert.AreEqual(slot.Size.Sp().PackedValue, slot.Style.FontSize);
            Assert.AreEqual(slot.Height.Sp().PackedValue, slot.Style.LineHeight);
            Assert.AreEqual(slot.Spacing.Sp().PackedValue, slot.Style.LetterSpacing);
            Assert.AreEqual(slot.Weight, slot.Style.FontWeight?.Weight);
            Assert.IsTrue(slot.Style.FontFamily?.Equals(slot.Family) == true);
            AssertInheritedDefaults(slot.Style, defaultStyle);
        }
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void PlainAndAnnotatedTextAdoptMetricsWithoutOverwritingColorOrFamily(bool withResourceFamilies)
    {
        var family = withResourceFamilies ? JetchatFonts.Montserrat : FontFamily.Monospace;
        var text = new Text("Today") { Color = Color.Red, FontFamily = family }
            .WithTypography(Typography.LabelSmall);
        Assert.AreEqual(11.Sp(), text.FontSize);
        Assert.AreEqual(16.Sp(), text.LineHeight);
        Assert.AreEqual(0.5f.Sp(), text.LetterSpacing);
        Assert.AreSame(FontWeight.SemiBold, text.FontWeight);
        Assert.AreEqual(Color.Red, text.Color);
        Assert.AreSame(family, text.FontFamily);

        using var value = new AnnotatedString("Message");
        var bodyFamily = withResourceFamilies ? JetchatFonts.Karla : FontFamily.Monospace;
        var annotated = new AnnotatedText(value) { Color = Color.Blue, FontFamily = bodyFamily }
            .WithTypography(Typography.BodyLarge);
        Assert.AreEqual(16.Sp(), annotated.FontSize);
        Assert.AreEqual(24.Sp(), annotated.LineHeight);
        Assert.AreEqual(0.15f.Sp(), annotated.LetterSpacing);
        Assert.AreSame(FontWeight.Normal, annotated.FontWeight);
        Assert.AreEqual(Color.Blue, annotated.Color);
        Assert.AreSame(bodyFamily, annotated.FontFamily);
    }

    static void AssertInheritedDefaults(NativeTextStyle actual, NativeTextStyle expected)
    {
        Assert.AreEqual(expected.Color, actual.Color);
        Assert.AreEqual(expected.FontStyle, actual.FontStyle);
        Assert.AreEqual(expected.FontSynthesis, actual.FontSynthesis);
        Assert.AreEqual(expected.FontFeatureSettings, actual.FontFeatureSettings);
        Assert.AreEqual(expected.BaselineShift, actual.BaselineShift);
        Assert.AreEqual(expected.TextGeometricTransform, actual.TextGeometricTransform);
        Assert.AreEqual(expected.LocaleList, actual.LocaleList);
        Assert.AreEqual(expected.Background, actual.Background);
        Assert.AreEqual(expected.TextDecoration, actual.TextDecoration);
        Assert.AreEqual(expected.Shadow, actual.Shadow);
        Assert.AreEqual(expected.DrawStyle, actual.DrawStyle);
        Assert.AreEqual(expected.GetTextAlign(), actual.GetTextAlign());
        Assert.AreEqual(expected.GetTextDirection(), actual.GetTextDirection());
        Assert.AreEqual(expected.TextIndent, actual.TextIndent);
        Assert.AreEqual(expected.PlatformStyle, actual.PlatformStyle);
        Assert.AreEqual(expected.LineHeightStyle, actual.LineHeightStyle);
        Assert.AreEqual(expected.GetLineBreak(), actual.GetLineBreak());
        Assert.AreEqual(expected.GetHyphens(), actual.GetHyphens());
        Assert.AreEqual(expected.TextMotion, actual.TextMotion);
    }
}
