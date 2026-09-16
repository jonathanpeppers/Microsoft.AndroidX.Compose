using Android.Views.InputMethods;
using AndroidX.Compose;
using AndroidX.Compose.Samples.Jetchat;
using AndroidX.Compose.UI.Text;
using ImeAction = Android.Views.InputMethods.ImeAction;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Exercises Foundation editing through real Android input connections on all authoring surfaces.</summary>
/// <remarks>
/// The fixture excludes the system IME from its own window so it cannot finish a
/// composition concurrently with the test-owned native connection. Live keyboard
/// acceptance is exercised separately in Jetchat.
/// </remarks>
[TestClass]
[DoNotParallelize]
public class BasicTextFieldTests
{
    /// <summary>Video send accepts an empty caption and clears the shared editor state.</summary>
    [TestMethod]
    public void Send_WithVideo_PreservesUriAndTrimsCaption()
    {
        using var input = ComposeExtensions.NewTextFieldValue("  caption  ");
        var state = new MutableState<global::AndroidX.Compose.UI.Text.Input.TextFieldValue>(input);
        string? sentUri = null;
        string? sentCaption = null;
        int resetCount = 0;
        int dismissCount = 0;

        bool sent = MessageInput.Send(
            state,
            "file:///video.mp4",
            _ => Assert.Fail("Video send must not use the text-only callback."),
            (uri, caption) =>
            {
                sentUri = uri;
                sentCaption = caption;
            },
            () => resetCount++,
            () => dismissCount++);

        Assert.IsTrue(sent);
        Assert.AreEqual("file:///video.mp4", sentUri);
        Assert.AreEqual("caption", sentCaption);
        Assert.AreEqual("", state.Value.Text);
        Assert.AreEqual(1, resetCount);
        Assert.AreEqual(1, dismissCount);
    }

    /// <summary>Diagnoses native admission independently of the editing assertions.</summary>
    [TestMethod]
    public async Task NativeControl_AdmitsTestOwnedConnection()
    {
        BasicTextFieldTestActivity.Ready = BasicTextFieldTestActivity.NewReady();
        BasicTextFieldTestActivity.Created = BasicTextFieldTestActivity.NewReady();
        var context = global::Android.App.Application.Context;
        using var intent = new global::Android.Content.Intent(context, typeof(BasicTextFieldTestActivity));
        intent.AddFlags(global::Android.Content.ActivityFlags.NewTask);
        intent.PutExtra("route", 6);
        context.StartActivity(intent);
        var activity = await BasicTextFieldTestActivity.Created.Task.WaitAsync(TimeSpan.FromSeconds(20));
        try
        {
            await BasicTextFieldTestActivity.Ready.Task.WaitAsync(TimeSpan.FromSeconds(20));
            await activity.MutateAsync(activity.Requester.RequestFocus);
            Assert.IsTrue(activity.Focused);
            using var info = new EditorInfo();
            await activity.MutateAsync(() => activity.OpenConnection(info));
            Assert.IsGreaterThan(0, activity.EditorHeight);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(await activity.AdmissionStateAsync(), ex);
        }
        finally
        {
            activity.RunOnUiThread(activity.Finish);
            await activity.Destroyed.Task.WaitAsync(TimeSpan.FromSeconds(10));
        }
    }

    /// <summary>Verifies native string-route selection and IME composition survive recomposition.</summary>
    [TestMethod]
    [DataRow(3)]
    [DataRow(4)]
    [DataRow(5)]
    [DataRow(7)]
    public async Task StringEditor_RetainsNativeSelectionAndCompositionAcrossRenders(int route)
    {
        BasicTextFieldTestActivity.Ready = BasicTextFieldTestActivity.NewReady();
        BasicTextFieldTestActivity.Created = BasicTextFieldTestActivity.NewReady();
        var context = global::Android.App.Application.Context;
        using var intent = new global::Android.Content.Intent(context, typeof(BasicTextFieldTestActivity));
        intent.AddFlags(global::Android.Content.ActivityFlags.NewTask);
        intent.PutExtra("route", route);
        context.StartActivity(intent);
        var activity = await BasicTextFieldTestActivity.Created.Task.WaitAsync(TimeSpan.FromSeconds(20));
        try
        {
            await BasicTextFieldTestActivity.Ready.Task.WaitAsync(TimeSpan.FromSeconds(20));
            Assert.AreEqual(0, activity.BorrowedFlags.IntValue(), "Decoration must not dispose a caller's shared boxed flags.");
            await activity.MutateAsync(activity.Requester.RequestFocus);
            using var info = new EditorInfo();
            IInputConnection? connection = null;
            await activity.MutateAsync(() => connection = activity.OpenConnection(info));
            var input = connection ?? throw new InvalidOperationException("String input connection missing.");
            Assert.AreEqual((int)ImeAction.Send, (int)info.ImeOptions & (int)ImeAction.ImeMaskAction);
            await activity.MutateAsync(() => Assert.IsTrue(input.CommitText("abcd", 1)));
            await activity.MutateAsync(() => Assert.IsTrue(input.SetSelection(1, 3)));
            await activity.MutateAsync(() => Assert.IsTrue(input.SetComposingText("XY", 1)));
            Assert.AreEqual("aXYd", activity.StringInput.Value);
            for (int i = 0; i < 4; i++)
                await activity.MutateAsync(() => { });
            Assert.AreEqual(0, activity.BorrowedFlags.IntValue());
            Assert.AreEqual(activity.Revision.Value, activity.DecorationRevision);
            await activity.MutateAsync(() =>
            {
                Assert.AreEqual("aXY", input.GetTextBeforeCursor(100, 0));
                Assert.AreEqual("d", input.GetTextAfterCursor(100, 0));
            });
            await activity.MutateAsync(() => Assert.IsTrue(input.SetComposingText("Z", 1)));
            Assert.AreEqual("aZd", activity.StringInput.Value, "The previous native composition must be replaced, not appended to after recomposition.");
            await activity.MutateAsync(() => Assert.IsTrue(input.FinishComposingText()));
            await activity.MutateAsync(() => Assert.IsTrue(input.SetSelection(1, 2)));
            await activity.MutateAsync(() => { });
            await activity.MutateAsync(() => Assert.IsTrue(input.CommitText("\U0001F680", 1)));
            Assert.AreEqual("a\U0001F680d", activity.StringInput.Value);
            await activity.MutateAsync(() => Assert.IsTrue(input.PerformEditorAction(ImeAction.Send)));
            Assert.HasCount(1, activity.Sent);
            Assert.AreEqual("a\U0001F680d", activity.Sent[0]);
            Assert.AreEqual("", activity.StringInput.Value);
            Assert.IsTrue(activity.Focused);
            await activity.MutateAsync(() => Assert.IsTrue(input.PerformEditorAction(ImeAction.Send)));
            Assert.HasCount(1, activity.Sent);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"String route {route}, stage {activity.Stage}, revision {activity.Revision.Value}. " +
                await activity.AdmissionStateAsync(), ex);
        }
        finally
        {
            activity.RunOnUiThread(activity.Finish);
            await activity.Destroyed.Task.WaitAsync(TimeSpan.FromSeconds(10));
        }
    }

    /// <summary>Verifies TFV editing, decoration, Send, keyboard settings and native line limits.</summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(6)]
    public async Task Editor_PreservesSelectionCompositionDecorationAndSend(int route)
    {
        BasicTextFieldTestActivity.Ready = BasicTextFieldTestActivity.NewReady();
        BasicTextFieldTestActivity.Created = BasicTextFieldTestActivity.NewReady();
        var context = global::Android.App.Application.Context;
        using var intent = new global::Android.Content.Intent(context, typeof(BasicTextFieldTestActivity));
        intent.AddFlags(global::Android.Content.ActivityFlags.NewTask);
        intent.PutExtra("route", route);
        context.StartActivity(intent);
        var activity = await BasicTextFieldTestActivity.Created.Task.WaitAsync(TimeSpan.FromSeconds(20));
        try
        {
            await BasicTextFieldTestActivity.Ready.Task.WaitAsync(TimeSpan.FromSeconds(20));
            Assert.AreEqual(0, activity.BorrowedFlags.IntValue(), "Decoration must not dispose a caller's shared boxed flags.");
            await activity.MutateAsync(activity.Requester.RequestFocus);
            Assert.IsTrue(activity.Focused);
            using var info = new EditorInfo();
            IInputConnection? connection = null;
            await activity.MutateAsync(() => connection = activity.OpenConnection(info));
            var input = connection ?? throw new InvalidOperationException("Input connection missing.");
            Assert.AreEqual((int)ImeAction.Send, (int)info.ImeOptions & (int)ImeAction.ImeMaskAction);
            await activity.MutateAsync(() => Assert.IsTrue(input.CommitText(" abcd ", 1)));
            Assert.AreEqual(" abcd ", activity.Input.Value.Text);
            await activity.MutateAsync(() => Assert.IsTrue(input.SetSelection(2, 4)));
            Assert.AreEqual(TextRangeKt.TextRange(2, 4), activity.Input.Value.Selection);
            await activity.MutateAsync(() => Assert.IsTrue(input.SetComposingText("XY", 1)));
            Assert.AreEqual(" aXYd ", activity.Input.Value.Text);
            Assert.IsNotNull(activity.Input.Value.Composition);
            var composing = activity.Input.Value.Composition;
            await activity.MutateAsync(() => { });
            Assert.AreEqual(composing, activity.Input.Value.Composition);
            Assert.AreEqual(activity.Revision.Value, activity.DecorationRevision);
            await activity.MutateAsync(() => Assert.IsTrue(input.FinishComposingText()));
            Assert.IsNull(activity.Input.Value.Composition);
            await activity.MutateAsync(() => Assert.IsTrue(input.SetSelection(4, 2)));
            await activity.MutateAsync(() =>
                activity.Input.Value = MessageInput.Insert(activity.Input.Value, "\U0001F680"));
            Assert.AreEqual(" a\U0001F680d ", activity.Input.Value.Text);
            Assert.AreEqual(TextRangeKt.TextRange(activity.Input.Value.Text.Length), activity.Input.Value.Selection);
            for (int i = 0; i < 4; i++)
                await activity.MutateAsync(() => { });
            Assert.AreEqual(0, activity.BorrowedFlags.IntValue());
            Assert.AreEqual(activity.Revision.Value, activity.DecorationRevision);
            Assert.IsNotNull(activity.TextLayout);
            Assert.AreEqual(Sp.Pack(20.Sp()), activity.TextLayout.LayoutInput.Style.FontSize);

            await activity.MutateAsync(() => Assert.IsTrue(input.PerformEditorAction(ImeAction.Send)));
            string[] expectedMessages = [" a\U0001F680d "];
            CollectionAssert.AreEqual(expectedMessages, activity.Sent);
            Assert.AreEqual("", activity.Input.Value.Text);
            Assert.AreEqual(0L, activity.Input.Value.Selection);
            Assert.IsNull(activity.Input.Value.Composition);
            Assert.IsTrue(activity.Focused, "Send must not clear focus; upstream supports rapid entry.");
            Assert.AreEqual(1, activity.ResetCount);
            Assert.AreEqual(1, activity.DismissCount);
            await activity.MutateAsync(() => Assert.IsTrue(input.PerformEditorAction(ImeAction.Send)));
            Assert.HasCount(1, activity.Sent);
            await activity.MutateAsync(() => Assert.IsTrue(input.CommitText("  ", 1)));
            await activity.MutateAsync(() => Assert.IsTrue(input.PerformEditorAction(ImeAction.Send)));
            Assert.HasCount(1, activity.Sent);
            Assert.AreEqual("  ", activity.Input.Value.Text);
            await activity.MutateAsync(() =>
            {
                activity.Input.Value = ComposeExtensions.NewTextFieldValue("button");
                activity.Send();
            });
            Assert.HasCount(2, activity.Sent);
            Assert.AreEqual("", activity.Input.Value.Text);
            Assert.AreEqual(2, activity.ResetCount);

            await activity.MutateAsync(() =>
            {
                activity.Decorated = false;
                activity.SingleLine = true;
                activity.Input.Value = ComposeExtensions.NewTextFieldValue("a");
            });
            Assert.AreEqual(1, activity.TextLayout.LineCount);
            Assert.IsGreaterThan(0, activity.EditorHeight);
            Assert.AreEqual((int)Math.Ceiling(activity.TextLayout.GetLineBottom(0)), activity.EditorHeight);
            await activity.MutateAsync(() =>
                activity.Input.Value = ComposeExtensions.NewTextFieldValue("a\nb\nc"));
            Assert.AreEqual(3, activity.TextLayout.LineCount, "Hard newlines remain in the full native paragraph layout.");
            Assert.IsFalse(activity.TextLayout.LayoutInput.SoftWrap);
            Assert.AreEqual("a\nb\nc", activity.Input.Value.Text);
            // Native line-height trimming differs for one-line and multi-line paragraphs.
            int singleLineHeight = (int)Math.Ceiling(activity.TextLayout.GetLineBottom(0));
            Assert.AreEqual(singleLineHeight, activity.EditorHeight,
                "singleLine must constrain the viewport to the native first-line height.");
            await activity.MutateAsync(() => activity.MinLines = 2);
            Assert.AreEqual(singleLineHeight, activity.EditorHeight,
                "singleLine must ignore minLines=2 without changing the same-content viewport.");
            await activity.MutateAsync(() =>
            {
                activity.SingleLine = false;
                activity.MaxLines = 2;
                activity.MinLines = 2;
            });
            Assert.AreEqual(3, activity.TextLayout.LineCount, "Line limits constrain viewport, not text content.");
            Assert.AreEqual("a\nb\nc", activity.Input.Value.Text);
            int twoLineHeight = activity.EditorHeight;
            Assert.IsGreaterThan(singleLineHeight, twoLineHeight, "maxLines=2 must expand the single-line viewport.");
            await activity.MutateAsync(() => activity.MaxLines = 3);
            Assert.IsGreaterThan(twoLineHeight, activity.EditorHeight, "maxLines must constrain the measured editor, not truncate its value.");
            await activity.MutateAsync(() => activity.Input.Value = ComposeExtensions.NewTextFieldValue("a"));
            Assert.AreEqual(twoLineHeight, activity.EditorHeight, "minLines=2 must reserve two lines even for one line of text.");
            await activity.MutateAsync(() => activity.ReadOnly = true);
            await activity.MutateAsync(activity.Requester.RequestFocus);
            await activity.MutateAsync(() =>
            {
                using var readOnlyInfo = new EditorInfo();
                using var readOnlyConnection = activity.TryOpenConnection(readOnlyInfo);
                Assert.IsNull(readOnlyConnection, "A read-only editor must not admit an IME editing connection.");
            });
            Assert.AreEqual("a", activity.Input.Value.Text);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Route {route}, stage {activity.Stage}, revision {activity.Revision.Value}. " +
                await activity.AdmissionStateAsync(), ex);
        }
        finally
        {
            activity.RunOnUiThread(activity.Finish);
            await activity.Destroyed.Task.WaitAsync(TimeSpan.FromSeconds(10));
        }
    }
}
