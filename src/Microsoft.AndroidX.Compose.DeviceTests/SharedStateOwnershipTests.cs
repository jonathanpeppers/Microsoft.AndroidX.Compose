using AccessibilityNodeInfo = Android.Views.Accessibility.AccessibilityNodeInfo;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Verifies shared native state remains registered for activity save and restore.</summary>
[TestClass]
[DoNotParallelize]
public class SharedStateOwnershipTests
{
    [TestMethod]
    [DataRow("replace-owned", false, false)]
    [DataRow("replace-owned", true, false)]
    [DataRow("replace-tree", false, false)]
    [DataRow("replace-tree", true, false)]
    [DataRow("replace-direct", false, false)]
    [DataRow("replace-direct", true, false)]
    [DataRow("replace-owned", true, true)]
    [DataRow("replace-tree", true, true)]
    [DataRow("replace-direct", true, true)]
    public async Task WrapperReplacement_IsolatesNativeStateAndRetainedValues(string mode, bool pending, bool keepOriginalSibling)
    {
        var activity = await StartActivity(mode, keepOriginalSibling);
        try
        {
            var originalPeer = activity.State.Jvm;
            Assert.IsNotNull(originalPeer);
            await OnUiThread(activity, () =>
            {
                activity.State.Hour = 8;
                activity.State.Minute = 16;
                if (pending)
                    activity.ReplacementState.Minute = 42;
                activity.UseReplacement.Value = true;
                activity.Pass.Value = 1;
            });
            await WaitFor(() => activity.CompletedPass == 1 && (keepOriginalSibling
                    ? activity.State.Jvm is { } current && !ReferenceEquals(current, originalPeer)
                    : activity.State.Jvm is null),
                "Replaced wrapper was not released.");
            var survivingPeer = activity.State.Jvm;
            var replacementPeer = activity.ReplacementState.Jvm;
            Assert.IsNotNull(replacementPeer);
            Assert.AreNotSame(originalPeer, replacementPeer, "Replacement wrapper reused the previous native state.");
            Assert.AreEqual(19, activity.ReplacementState.Hour);
            Assert.AreEqual(pending ? 42 : 27, activity.ReplacementState.Minute);
            Assert.AreEqual(8, activity.State.Hour, "Replacement contaminated the previous wrapper.");
            Assert.AreEqual(16, activity.State.Minute, "Pending replacement write contaminated the previous wrapper.");

            await OnUiThread(activity, () =>
            {
                activity.ReplacementState.Hour = 20;
                activity.UseReplacement.Value = false;
                activity.Pass.Value = 2;
            });
            await WaitFor(() => activity.CompletedPass == 2 && activity.ReplacementState.Jvm is null,
                "Returning original wrapper did not replace the second owner.");
            Assert.AreNotSame(replacementPeer, activity.State.Jvm);
            Assert.AreEqual(8, activity.State.Hour);
            Assert.AreEqual(16, activity.State.Minute);
            Assert.AreEqual(20, activity.ReplacementState.Hour);
            Assert.AreEqual(pending ? 42 : 27, activity.ReplacementState.Minute);
            if (keepOriginalSibling)
            {
                Assert.AreSame(survivingPeer, activity.State.Jvm, "Returning consumer replaced the surviving sibling's peer.");
                return;
            }

            await OnUiThread(activity, activity.Recreate);
            await WaitFor(() => SharedStateOwnershipTestActivity.Current is { } current
                && !ReferenceEquals(current, activity) && current.CompletedPass >= 0,
                "Replacement activity did not recreate.");
            activity = SharedStateOwnershipTestActivity.Current
                ?? throw new InvalidOperationException("Recreated replacement activity was unavailable.");
            Assert.AreEqual(8, activity.State.Hour, "Replacement grouping destabilized native restore identity.");
            Assert.AreEqual(16, activity.State.Minute);
        }
        finally
        {
            await OnUiThread(activity, activity.Finish);
            await WaitFor(() => !ReferenceEquals(SharedStateOwnershipTestActivity.Current, activity),
                "Replacement activity did not finish.");
        }
    }

    [TestMethod]
    [DataRow("native-dial")]
    [DataRow("owned-dial")]
    public async Task DialReentry_RetainsSelectedMinuteAndPeer(string mode)
    {
        var automation = TestInstrumentation.Current?.UiAutomation
            ?? throw new InvalidOperationException("Instrumentation UI automation is unavailable.");
        var activity = await StartActivity(mode);
        try
        {
            var peer = activity.State.Jvm;
            await OnUiThread(activity, () =>
            {
                activity.State.Hour = 19;
                activity.State.Minute = 25;
            });
            await WaitFor(() => SelectMinutes(automation), "Native minute selector was not available.");
            await WaitFor(() => activity.NativeSelection == 1, "Native picker did not select minutes.");
            await Task.Delay(500);
            SaveDialScreenshot(automation, mode + "-before");

            await OnUiThread(activity, () =>
            {
                activity.ShowFirst.Value = false;
                activity.Pass.Value = 1;
            });
            await WaitFor(() => activity.CompletedPass == 1, "Dial did not leave composition.");
            Assert.AreSame(peer, activity.State.Jvm);
            await OnUiThread(activity, () =>
            {
                activity.ShowFirst.Value = true;
                activity.Pass.Value = 2;
            });
            await WaitFor(() => activity.CompletedPass == 2, "Dial did not reenter composition.");
            Assert.AreSame(peer, activity.State.Jvm);
            Assert.AreEqual(19, activity.State.Hour);
            Assert.AreEqual(25, activity.State.Minute);
            Assert.AreEqual(1, activity.NativeSelection, "Native selection must still be minutes, not hours.");
            await Task.Delay(500);
            SaveDialScreenshot(automation, mode + "-after");
            Console.WriteLine($"{mode}: before/after selection=Minute(1), hour=19, minute=25, peer unchanged.");
        }
        finally
        {
            await OnUiThread(activity, activity.Finish);
            await WaitFor(() => !ReferenceEquals(SharedStateOwnershipTestActivity.Current, activity),
                "Dial comparison activity did not finish.");
        }
    }

    static bool SelectMinutes(UiAutomation automation)
    {
        using var root = automation.RootInActiveWindow;
        if (root is null || root.PackageName != "net.compose.devicetests")
            return false;
        return Visit(root);

        static bool Visit(AccessibilityNodeInfo node)
        {
            if (node.ContentDescription == "Select minutes")
            {
                using var parent = node.Parent;
                return parent?.PerformAction(global::Android.Views.Accessibility.Action.Click) == true;
            }
            for (int i = 0; i < node.ChildCount; i++)
            {
                using var child = node.GetChild(i);
                if (child is not null && Visit(child))
                    return true;
            }
            return false;
        }
    }

    static void SaveDialScreenshot(UiAutomation automation, string name)
    {
        using var root = automation.RootInActiveWindow;
        Assert.AreEqual("net.compose.devicetests", root?.PackageName, "Only capture the test activity.");
        using var screenshot = automation.TakeScreenshot()
            ?? throw new InvalidOperationException("Native dial screenshot was unavailable.");
        string directory = global::Android.App.Application.Context.GetExternalFilesDir(null)?.AbsolutePath
            ?? throw new InvalidOperationException("Test artifact directory was unavailable.");
        using var output = File.Create(Path.Combine(directory, name + ".png"));
        Assert.IsTrue(screenshot.Compress(global::Android.Graphics.Bitmap.CompressFormat.Png
            ?? throw new InvalidOperationException("PNG format was unavailable."), 100, output));
    }

    [TestMethod]
    [DataRow("omitted-direct")]
    [DataRow("omitted-tree")]
    public async Task OmittedDirectConsumer_RepeatedRenderThenRecreation_RestoresTime(string mode)
    {
        var automation = TestInstrumentation.Current?.UiAutomation
            ?? throw new InvalidOperationException("Instrumentation UI automation is unavailable.");
        var activity = await StartActivity(mode);
        try
        {
            await WaitFor(() => ReadTimeFields(automation).Count == 2,
                "Omitted direct consumer did not expose its hour and minute fields.");
            Assert.IsNull(activity.State.Jvm, "The omitted consumer must not bind the fixture's supplied wrapper.");
            await SetTimeField(automation, 0, "19");
            await SetTimeField(automation, 1, "27");
            string[] expected = ["19", "27"];
            await WaitFor(() => ReadTimeFields(automation).SequenceEqual(expected),
                "Editing the omitted consumer's native time fields failed.");

            for (int pass = 1; pass <= 3; pass++)
            {
                int nextPass = pass;
                await OnUiThread(activity, () => activity.Pass.Value = nextPass);
                await WaitFor(() => activity.CompletedPass == nextPass,
                    "Omitted direct consumer's parent did not execute again.");
                await WaitFor(() => ReadTimeFields(automation).SequenceEqual(expected),
                    $"Omitted direct consumer lost its selection after execution {nextPass}.");
            }

            var originalActivity = activity;
            await OnUiThread(activity, activity.Recreate);
            await WaitFor(() => SharedStateOwnershipTestActivity.Current is { } current
                && !ReferenceEquals(current, originalActivity) && current.CompletedPass >= 0,
                "Omitted direct consumer's activity did not recreate.");
            activity = SharedStateOwnershipTestActivity.Current
                ?? throw new InvalidOperationException("Recreated omitted-consumer activity was not available.");
            Assert.IsNull(activity.State.Jvm, "Recreation must still use the consumer's omitted wrapper path.");
            await WaitFor(() => ReadTimeFields(automation).SequenceEqual(expected),
                "Native save registration did not restore the omitted consumer's selected time.");
            CollectionAssert.AreEqual(expected, ReadTimeFields(automation));
        }
        finally
        {
            await OnUiThread(activity, activity.Finish);
            await WaitFor(() => !ReferenceEquals(SharedStateOwnershipTestActivity.Current, activity),
                "Omitted direct consumer's activity did not finish.");
        }
    }

    [TestMethod]
    [DataRow("native")]
    [DataRow("tree")]
    [DataRow("direct")]
    [DataRow("owned-tree")]
    [DataRow("owned-direct")]
    [DataRow("owned-omitted-tree")]
    [DataRow("owned-omitted-direct")]
    public async Task RepeatedRenderThenRecreation_RestoresSharedTime(string mode)
        => await VerifyRepeatedRenderThenRecreation(mode, collect: false);

    [TestMethod]
    [DataRow("tree")]
    [DataRow("direct")]
    [DataRow("owned-tree")]
    [DataRow("owned-direct")]
    public async Task ForcedGc_RepeatedRenderThenRecreation_RestoresSharedTime(string mode)
        => await VerifyRepeatedRenderThenRecreation(mode, collect: true);

    static async Task VerifyRepeatedRenderThenRecreation(string mode, bool collect)
    {
        var activity = await StartActivity(mode);
        try
        {
            var originalState = activity.State;
            for (int pass = 1; pass <= 3; pass++)
            {
                if (collect)
                    SharedStateOwnerLifetimeTests.CollectBothRuntimes();
                int nextPass = pass;
                await OnUiThread(activity, () =>
                {
                    activity.State.Hour = 19;
                    activity.State.Minute = 27;
                    activity.Pass.Value = nextPass;
                });
                await WaitFor(() => activity.CompletedPass == nextPass,
                    "Shared-state parent did not execute again.");
                Assert.IsTrue(activity.SiblingsSharePeer, "Sibling consumers must use one state peer.");
            }

            await OnUiThread(activity, activity.Recreate);
            await WaitFor(() => SharedStateOwnershipTestActivity.Current is { } current
                && !ReferenceEquals(current, activity) && current.CompletedPass >= 0,
                "Shared-state activity did not recreate.");
            activity = SharedStateOwnershipTestActivity.Current
                ?? throw new InvalidOperationException("Recreated activity was not available.");

            Assert.AreNotSame(originalState, activity.State, "Restore must use a fresh managed wrapper.");
            Assert.AreEqual(19, activity.State.Hour, "Native save provider lost the selected hour.");
            Assert.AreEqual(27, activity.State.Minute, "Native save provider lost the selected minute.");
            Assert.IsTrue(activity.SiblingsSharePeer);
        }
        finally
        {
            await OnUiThread(activity, activity.Finish);
            await WaitFor(() => !ReferenceEquals(SharedStateOwnershipTestActivity.Current, activity),
                "Shared-state activity did not finish.");
        }
    }

    [TestMethod]
    [DataRow("owned-tree")]
    [DataRow("owned-direct")]
    [DataRow("owned-omitted-tree")]
    [DataRow("owned-omitted-direct")]
    public async Task AncestorOwner_PreservesPeerWhenConsumersLeaveAndReturn(string mode)
    {
        var activity = await StartActivity(mode);
        try
        {
            var peer = activity.State.Jvm
                ?? throw new InvalidOperationException("Shared state was not bound.");
            for (int pass = 1; pass <= 3; pass++)
            {
                int nextPass = pass;
                await OnUiThread(activity, () =>
                {
                    activity.State.Hour = 19;
                    activity.State.Minute = 27;
                    activity.ShowFirst.Value = nextPass == 3;
                    activity.ShowSecond.Value = nextPass != 2;
                    activity.Pass.Value = nextPass;
                });
                await WaitFor(() => activity.CompletedPass == nextPass,
                    "Shared consumers did not leave or re-enter.");
                Assert.AreSame(peer, activity.State.Jvm, "A live ancestor must retain the exact peer.");
                Assert.AreEqual(19, activity.State.Hour);
                Assert.AreEqual(27, activity.State.Minute);
                Assert.IsTrue(activity.SiblingsSharePeer);
            }

            await OnUiThread(activity, activity.Recreate);
            await WaitFor(() => SharedStateOwnershipTestActivity.Current is { } current
                && !ReferenceEquals(current, activity) && current.CompletedPass >= 0,
                "Owned-state activity did not recreate.");
            activity = SharedStateOwnershipTestActivity.Current
                ?? throw new InvalidOperationException("Recreated owned-state activity was not available.");
            Assert.AreEqual(19, activity.State.Hour, "Owner save provider did not survive consumer removal.");
            Assert.AreEqual(27, activity.State.Minute);
        }
        finally
        {
            await OnUiThread(activity, activity.Finish);
            await WaitFor(() => !ReferenceEquals(SharedStateOwnershipTestActivity.Current, activity),
                "Owned-state activity did not finish.");
        }
    }

    [TestMethod]
    [DataRow("tree")]
    [DataRow("direct")]
    public async Task ImplicitOwnerRemoval_TransfersValuesToRemainingSibling(string mode)
    {
        var activity = await StartActivity(mode);
        try
        {
            var originalPeer = activity.State.Jvm
                ?? throw new InvalidOperationException("Implicit owner was not bound.");
            await OnUiThread(activity, () =>
            {
                activity.State.Hour = 19;
                activity.State.Minute = 27;
                activity.ShowFirst.Value = false;
                activity.Pass.Value = 1;
            });
            await WaitFor(() => activity.CompletedPass == 1 && activity.State.Jvm is { } peer
                && !ReferenceEquals(peer, originalPeer),
                "Remaining consumer did not acquire a new native owner.");
            var successor = activity.State.Jvm;
            Assert.AreEqual(19, activity.State.Hour);
            Assert.AreEqual(27, activity.State.Minute);

            await OnUiThread(activity, () =>
            {
                activity.ShowFirst.Value = true;
                activity.Pass.Value = 2;
            });
            await WaitFor(() => activity.CompletedPass == 2, "Original consumer did not return.");
            Assert.AreSame(successor, activity.State.Jvm, "Returning consumer must use the surviving owner.");
            Assert.IsTrue(activity.SiblingsSharePeer);

            await OnUiThread(activity, () =>
            {
                activity.ShowFirst.Value = false;
                activity.ShowSecond.Value = false;
                activity.Pass.Value = 3;
            });
            await WaitFor(() => activity.CompletedPass == 3 && activity.State.Jvm is null,
                "Last owner did not release its binding.");
            Assert.AreEqual(19, activity.State.Hour);
            Assert.AreEqual(27, activity.State.Minute);
            await OnUiThread(activity, () =>
            {
                activity.State.Minute = 42;
                activity.ShowFirst.Value = true;
                activity.ShowSecond.Value = true;
                activity.Pass.Value = 4;
            });
            await WaitFor(() => activity.CompletedPass == 4 && activity.State.Jvm is not null,
                "Hidden shared state did not acquire a new owner.");
            Assert.AreEqual(19, activity.State.Hour);
            Assert.AreEqual(42, activity.State.Minute);
            Assert.AreNotSame(successor, activity.State.Jvm);
            Assert.IsTrue(activity.SiblingsSharePeer);
        }
        finally
        {
            await OnUiThread(activity, activity.Finish);
            await WaitFor(() => !ReferenceEquals(SharedStateOwnershipTestActivity.Current, activity),
                "Implicit-owner activity did not finish.");
        }
    }

    static List<string?> ReadTimeFields(UiAutomation automation)
    {
        List<string?> fields = [];
        VisitTimeFields(automation, (node, _) => fields.Add(node.Text));
        return fields;
    }

    static async Task SetTimeField(UiAutomation automation, int index, string value)
    {
        bool activated = false;
        VisitTimeFields(automation, (node, currentIndex) =>
        {
            if (currentIndex != index)
                return;
            if (node.ClassName == "android.widget.EditText")
                activated = true;
            else
            {
                using var parent = node.Parent;
                activated = parent?.PerformAction(global::Android.Views.Accessibility.Action.Click) == true;
            }
        });
        Assert.IsTrue(activated, $"Native time field {index} could not be selected.");
        await WaitFor(() =>
        {
            bool editable = false;
            VisitTimeFields(automation, (node, currentIndex) =>
            {
                if (currentIndex == index)
                    editable = node.ClassName == "android.widget.EditText";
            });
            return editable;
        }, $"Native time field {index} did not enter editing mode.");

        using var arguments = new Bundle();
        arguments.PutString(AccessibilityNodeInfo.ActionArgumentSetTextCharsequence, value);
        bool updated = false;
        VisitTimeFields(automation, (node, currentIndex) =>
        {
            if (currentIndex == index)
                updated = node.PerformAction(global::Android.Views.Accessibility.Action.SetText, arguments);
        });
        Assert.IsTrue(updated, $"Native time field {index} rejected the text '{value}'.");
    }

    static void VisitTimeFields(UiAutomation automation, Action<AccessibilityNodeInfo, int> visit)
    {
        using var root = automation.RootInActiveWindow;
        if (root is null || root.PackageName != "net.compose.devicetests" || !root.Refresh())
            return;
        int index = 0;
        Visit(root);

        void Visit(AccessibilityNodeInfo node)
        {
            // TimeInput exposes only its selected field as an editor; the other is selectable text.
            if (node.ClassName == "android.widget.EditText"
                || (node.ClassName == "android.widget.TextView" && int.TryParse(node.Text, out _)))
                visit(node, index++);
            for (int i = 0; i < node.ChildCount; i++)
            {
                using var child = node.GetChild(i);
                if (child is not null)
                    Visit(child);
            }
        }
    }

    static async Task<SharedStateOwnershipTestActivity> StartActivity(string mode, bool keepOriginalSibling = false)
    {
        var context = global::Android.App.Application.Context;
        using var intent = new global::Android.Content.Intent(context, typeof(SharedStateOwnershipTestActivity));
        intent.PutExtra("mode", mode);
        intent.PutExtra("keep-original-sibling", keepOriginalSibling);
        intent.AddFlags(global::Android.Content.ActivityFlags.NewTask);
        context.StartActivity(intent);
        await WaitFor(() => SharedStateOwnershipTestActivity.Current is { CompletedPass: >= 0 },
            "Shared-state activity did not start.");
        return SharedStateOwnershipTestActivity.Current
            ?? throw new InvalidOperationException("Shared-state activity was not available.");
    }

    static Task OnUiThread(SharedStateOwnershipTestActivity activity, Action action)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        activity.RunOnUiThread(() =>
        {
            try
            {
                action();
                completion.SetResult();
            }
            catch (Exception ex)
            {
                completion.SetException(ex);
            }
        });
        return completion.Task;
    }

    static async Task WaitFor(Func<bool> condition, string message)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(15);
        while (DateTime.UtcNow < deadline)
        {
            if (condition())
                return;
            await Task.Delay(20);
        }
        Assert.Fail(message);
    }
}
