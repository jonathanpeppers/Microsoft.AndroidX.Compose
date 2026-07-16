using AndroidX.Compose;
using AndroidX.Compose.UI.Text;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>
/// Verifies managed text-field selection overloads reach the JNI constructor
/// with the packed <c>TextRange</c> expected by Compose.
/// </summary>
[TestClass]
public class TextFieldValueTests
{
    [TestMethod]
    public void NewTextFieldValue_Cursor_CreatesCollapsedSelection()
    {
        using var value = ComposeExtensions.NewTextFieldValue("hello", cursor: 3);

        Assert.AreEqual("hello", value.Text);
        Assert.AreEqual(TextRangeKt.TextRange(3), value.Selection);
        Assert.IsNull(value.Composition);
    }

    [TestMethod]
    public void NewTextFieldValue_Range_CreatesSelection()
    {
        using var value = ComposeExtensions.NewTextFieldValue(
            "hello",
            selectionStart: 1,
            selectionEnd: 4);

        Assert.AreEqual("hello", value.Text);
        Assert.AreEqual(TextRangeKt.TextRange(1, 4), value.Selection);
        Assert.IsNull(value.Composition);
    }
}
