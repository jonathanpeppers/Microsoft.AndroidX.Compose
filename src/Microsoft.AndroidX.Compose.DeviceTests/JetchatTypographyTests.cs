using AndroidX.Compose;
using AndroidX.Compose.Samples.Jetchat;
using NativeTextStyle = AndroidX.Compose.UI.Text.TextStyle;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Checks the actual sample's pinned text metrics across the Kotlin style boundary.</summary>
[TestClass]
public class JetchatTypographyTests
{
    [TestMethod]
    public void ThemeSlotsMatchPinnedJetchatMetrics()
    {
        using var typography = JetchatTypography.Build();
        (NativeTextStyle Style, int Size, int Height, float Spacing, int Weight)[] slots =
        [
            (typography.DisplayLarge, 57, 64, 0f, 300),
            (typography.DisplayMedium, 45, 52, 0f, 300),
            (typography.DisplaySmall, 36, 44, 0f, 400),
            (typography.HeadlineLarge, 32, 40, 0f, 600),
            (typography.HeadlineMedium, 28, 36, 0f, 600),
            (typography.HeadlineSmall, 24, 32, 0f, 600),
            (typography.TitleLarge, 22, 28, 0f, 600),
            (typography.TitleMedium, 16, 24, 0.15f, 600),
            (typography.TitleSmall, 14, 20, 0.1f, 700),
            (typography.BodyLarge, 16, 24, 0.15f, 400),
            (typography.BodyMedium, 14, 20, 0.25f, 500),
            (typography.BodySmall, 12, 16, 0.4f, 700),
            (typography.LabelLarge, 14, 20, 0.1f, 600),
            (typography.LabelMedium, 12, 16, 0.5f, 600),
            (typography.LabelSmall, 11, 16, 0.5f, 600),
        ];
        foreach (var slot in slots)
        {
            Assert.AreEqual(slot.Size.Sp().PackedValue, slot.Style.FontSize);
            Assert.AreEqual(slot.Height.Sp().PackedValue, slot.Style.LineHeight);
            Assert.AreEqual(slot.Spacing.Sp().PackedValue, slot.Style.LetterSpacing);
            Assert.AreEqual(slot.Weight, slot.Style.FontWeight?.Weight);
        }
    }

    [TestMethod]
    public void PlainAndAnnotatedTextAdoptMetricsWithoutOverwritingColorOrFamily()
    {
        var family = FontFamily.Monospace;
        var text = new Text("Today") { Color = Color.Red, FontFamily = family }
            .WithTypography(JetchatTypography.LabelSmall);
        Assert.AreEqual(11.Sp(), text.FontSize);
        Assert.AreEqual(16.Sp(), text.LineHeight);
        Assert.AreEqual(0.5f.Sp(), text.LetterSpacing);
        Assert.AreSame(FontWeight.SemiBold, text.FontWeight);
        Assert.AreEqual(Color.Red, text.Color);
        Assert.AreSame(family, text.FontFamily);

        using var value = new AnnotatedString("Message");
        var annotated = new AnnotatedText(value) { Color = Color.Blue, FontFamily = family }
            .WithTypography(JetchatTypography.BodyLarge);
        Assert.AreEqual(16.Sp(), annotated.FontSize);
        Assert.AreEqual(24.Sp(), annotated.LineHeight);
        Assert.AreEqual(0.15f.Sp(), annotated.LetterSpacing);
        Assert.AreSame(FontWeight.Normal, annotated.FontWeight);
        Assert.AreEqual(Color.Blue, annotated.Color);
        Assert.AreSame(family, annotated.FontFamily);
    }
}
