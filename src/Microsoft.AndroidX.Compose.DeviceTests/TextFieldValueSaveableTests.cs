using Android.Runtime;
using AndroidX.Compose;
using AndroidX.Compose.UI.Text.Input;
using TextRangeKt = AndroidX.Compose.UI.Text.TextRangeKt;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Verifies the native TextFieldValue value saver, keyed wrapper identity and sibling isolation.</summary>
[TestClass]
[DoNotParallelize]
public class TextFieldValueSaveableTests
{
    /// <summary>Text/annotations/reversed selection restore, while only restore drops IME composition.</summary>
    [TestMethod]
    public async Task NativeSaver_RoundTripsValueAndResetsOnlyChangedOwner()
    {
        string key = "composers";
        MutableState<TextFieldValue>? observed = null, sibling = null;
        int calls = 0;
        RememberSaveableTestActivity.Reset(c =>
        {
            var draft = c.RememberSaveable(() =>
            {
                calls++;
                return new MutableState<TextFieldValue>(ComposeExtensions.NewTextFieldValue(key));
            }, key1: key);
            var other = c.RememberSaveable(() =>
                new MutableState<TextFieldValue>(ComposeExtensions.NewTextFieldValue("other")));
            c.SideEffect(() => { observed = draft; sibling = other; });
        });
        var context = global::Android.App.Application.Context;
        using var intent = new global::Android.Content.Intent(context, typeof(RememberSaveableTestActivity));
        intent.AddFlags(global::Android.Content.ActivityFlags.NewTask);
        context.StartActivity(intent);
        await WaitFor(() => RememberSaveableTestActivity.Current is { Resumed: true, HasWindowFocus: true } &&
            observed is not null, "Saveable editor activity did not resume.");
        var activity = RememberSaveableTestActivity.Current
            ?? throw new InvalidOperationException("Saveable editor activity is unavailable.");
        try
        {
            var original = State();
            var other = Sibling();
            TextFieldValue? edited = null;
            await Change(() =>
            {
                var builder = new AnnotatedStringBuilder();
                builder.Append("  a\U0001F600bc\nz  ");
                builder.AddStyle(new SpanStyle { FontWeight = FontWeight.Bold }, 2, 5);
                builder.AddStringAnnotation("draft", "metadata", 2, 5);
                using var annotated = builder.ToAnnotatedString();
                using var native = annotated.JavaCast<global::AndroidX.Compose.UI.Text.AnnotatedString>();
                using var composition = TextFieldValueTestBridges.BoxRange(TextRangeKt.TextRange(2, 5));
                edited = ComposeExtensions.NewTextFieldValue().Copy(native, TextRangeKt.TextRange(7, 2), composition);
                original.Value = edited;
                other.Value = ComposeExtensions.NewTextFieldValue(" sibling386 ", cursor: 4);
            });
            var expected = edited ?? throw new InvalidOperationException("Edited TextFieldValue was not created.");
            await Change(() => { });
            Assert.AreSame(original, State());
            Assert.AreEqual(expected, State().Value);
            Assert.IsNotNull(State().Value.Composition, "Ordinary recomposition must not apply/clear live IME composition.");
            Assert.AreEqual(1, calls);

            var old = activity;
            int passes = RememberSaveableTestActivity.Passes;
            await OnUi(old, old.Recreate);
            await WaitFor(() => old.IsDestroyed && !old.Resumed &&
                RememberSaveableTestActivity.Current is { Resumed: true, HasWindowFocus: true } replacement &&
                !ReferenceEquals(old, replacement) && RememberSaveableTestActivity.Passes > passes,
                "The old activity was not destroyed and replaced by a resumed activity.");
            activity = RememberSaveableTestActivity.Current
                ?? throw new InvalidOperationException("Replacement saveable editor activity is unavailable.");
            Assert.IsTrue(activity.Restored);
            Assert.AreNotSame(original, State());
            Assert.AreNotSame(other, Sibling());
            Assert.AreEqual(expected.Text, State().Value.Text);
            var expectedAnnotations = expected.AnnotatedString;
            var restoredAnnotations = State().Value.AnnotatedString;
            using var plain = new global::AndroidX.Compose.UI.Text.AnnotatedString(expected.Text, []);
            Assert.IsFalse(expectedAnnotations.HasEqualAnnotations(plain),
                "The annotation comparison must detect missing styles and metadata on identical text.");
            // Generic managed equality compares Java peer wrappers, not Kotlin annotation values.
            Assert.IsTrue(expectedAnnotations.HasEqualAnnotations(restoredAnnotations),
                "The native saver must preserve the complete annotation values and ranges.");
            Assert.AreEqual(expected.Selection, State().Value.Selection);
            Assert.IsNull(State().Value.Composition, "The native saver must not serialize an IME composition range.");
            Assert.AreEqual(" sibling386 ", Sibling().Value.Text);
            Assert.AreEqual(TextRangeKt.TextRange(4), Sibling().Value.Selection);
            Assert.AreEqual(2, calls);

            var restored = State();
            var restoredSibling = Sibling();
            await Change(() => key = "new-conversation");
            Assert.AreNotSame(restored, State());
            Assert.AreEqual("new-conversation", State().Value.Text);
            Assert.AreEqual(3, calls);
            Assert.AreSame(restoredSibling, Sibling());
            Assert.AreEqual(" sibling386 ", Sibling().Value.Text);
            Assert.AreEqual(expected.Text, restored.Value.Text, "Reset must not mutate a previous owner's value.");
            await Change(() => State().Value = ComposeExtensions.NewTextFieldValue(" changed ", cursor: 3));
            Assert.AreEqual(" changed ", State().Value.Text);
            Assert.AreEqual(TextRangeKt.TextRange(3), State().Value.Selection);
        }
        finally
        {
            await OnUi(activity, activity.Finish);
            await WaitFor(() => activity.IsDestroyed, "Saveable editor activity did not finish.");
            RememberSaveableTestActivity.Content = null;
        }

        MutableState<TextFieldValue> State() => observed
            ?? throw new InvalidOperationException("Saveable editor state was not observed.");
        MutableState<TextFieldValue> Sibling() => sibling
            ?? throw new InvalidOperationException("Sibling saveable editor state was not observed.");
        async Task Change(Action action)
        {
            int pass = RememberSaveableTestActivity.Passes;
            await OnUi(activity, () => { action(); RememberSaveableTestActivity.Revision.Value++; });
            await WaitFor(() => RememberSaveableTestActivity.Passes > pass, "Saveable editor did not recompose.");
        }
    }

    static Task OnUi(RememberSaveableTestActivity activity, Action action)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        activity.RunOnUiThread(() =>
        {
            try { action(); completion.TrySetResult(); }
            catch (Exception error) { completion.TrySetException(error); }
        });
        return completion.Task.WaitAsync(TimeSpan.FromSeconds(15));
    }

    static async Task WaitFor(Func<bool> predicate, string message)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (!predicate())
        {
            if (DateTime.UtcNow >= deadline) Assert.Fail(message);
            await Task.Delay(20);
        }
    }
}
