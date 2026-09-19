using AndroidX.Compose.Material3;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Checks actual facade handoffs and round trips across native sheet factory domains.</summary>
[TestClass]
[DoNotParallelize]
public class SheetStateHandoffTests
{
    /// <summary>Publishes standard-sheet veto replacements only after composition commits.</summary>
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task StandardSheet_CommittedVetoReplacementControlsTransitions(bool direct)
    {
        var context = global::Android.App.Application.Context;
        using var intent = new global::Android.Content.Intent(context, typeof(SheetStateHandoffTestActivity));
        intent.AddFlags(global::Android.Content.ActivityFlags.NewTask);
        intent.PutExtra("direct", direct);
        intent.PutExtra("standard", true);
        SheetStateHandoffTestActivity? activity = null;
        try
        {
            context.StartActivity(intent);
            await WaitFor(() => SheetStateHandoffTestActivity.Current is not null,
                "Standard sheet activity did not start.");
            activity = SheetStateHandoffTestActivity.Current
                ?? throw new InvalidOperationException("Standard sheet activity was unavailable.");
            await WaitFor(() => activity.CompletedPass == 0,
                "Initial standard sheet owner did not commit.");
            await WaitFor(() => activity.Sheet.HasPartiallyExpandedState,
                "Standard sheet did not install its partial anchor.");

            await OnUiThread(activity, () => activity.Sheet.ExpandAsync());
            Assert.AreEqual(SheetValue.PartiallyExpanded, activity.Sheet.CurrentValue,
                "Initial callback did not veto the expand transition.");

            await OnUiThread(activity, () =>
            {
                activity.AllowTransitions.Value = true;
                activity.Pass.Value = 1;
            });
            await WaitFor(() => activity.CompletedPass == 1,
                "Allowing callback replacement did not commit.");
            await OnUiThread(activity, () => activity.Sheet.ExpandAsync());
            Assert.AreEqual(SheetValue.Expanded, activity.Sheet.CurrentValue,
                "Committed callback replacement did not allow the expand transition.");

            await OnUiThread(activity, () =>
            {
                activity.AllowTransitions.Value = false;
                activity.Pass.Value = 2;
            });
            await WaitFor(() => activity.CompletedPass == 2,
                "Vetoing callback replacement did not commit.");
            await OnUiThread(activity, () => activity.Sheet.PartialExpandAsync());
            Assert.AreEqual(SheetValue.Expanded, activity.Sheet.CurrentValue,
                "Latest committed callback did not veto the partial-expand transition.");
        }
        finally
        {
            var started = activity ?? SheetStateHandoffTestActivity.Current;
            if (started is not null)
            {
                await OnUiThread(started, started.Finish);
                await WaitFor(() => !ReferenceEquals(SheetStateHandoffTestActivity.Current, started),
                    "Standard sheet activity did not finish.");
            }
        }
    }

    /// <summary>Transfers a retired sheet through the other factory and back using tree or direct facades.</summary>
    [TestMethod]
    [DataRow(false, false, false, false)]
    [DataRow(false, false, true, false)]
    [DataRow(false, true, false, false)]
    [DataRow(false, true, true, false)]
    [DataRow(true, false, false, false)]
    [DataRow(true, false, true, false)]
    [DataRow(true, true, false, false)]
    [DataRow(true, true, true, false)]
    [DataRow(false, false, false, true)]
    [DataRow(false, false, true, true)]
    [DataRow(false, true, false, true)]
    [DataRow(false, true, true, true)]
    [DataRow(true, false, false, true)]
    [DataRow(true, false, true, true)]
    [DataRow(true, true, false, true)]
    [DataRow(true, true, true, true)]
    public async Task ModalAndStandardOwners_TransferValidInitialValues(
        bool direct, bool sourceStandard, bool skipPartial, bool expanded)
    {
        var context = global::Android.App.Application.Context;
        using var intent = new global::Android.Content.Intent(context, typeof(SheetStateHandoffTestActivity));
        intent.AddFlags(global::Android.Content.ActivityFlags.NewTask);
        intent.PutExtra("direct", direct);
        intent.PutExtra("standard", sourceStandard);
        intent.PutExtra("skipPartial", skipPartial);
        intent.PutExtra("expanded", expanded);
        SheetStateHandoffTestActivity? activity = null;
        try
        {
            context.StartActivity(intent);
            await WaitFor(() => SheetStateHandoffTestActivity.Current is not null, "Sheet handoff activity did not start.");
            activity = SheetStateHandoffTestActivity.Current
                ?? throw new InvalidOperationException("Sheet handoff activity was unavailable.");
            await WaitFor(() => activity.CompletedPass == 0, "Initial sheet owner did not commit.");
            await WaitForAnchors(sourceStandard);
            var original = activity.Sheet.Jvm;
            Assert.IsNotNull(original);
            var initialValue = expanded ? SheetValue.Expanded
                : sourceStandard ? SheetValue.PartiallyExpanded : SheetValue.Hidden;
            Assert.AreEqual(initialValue, activity.Sheet.CurrentValue);
            await RenderPass(1);
            Assert.AreSame(original, activity.Sheet.Jvm, "Re-executing the first owner replaced its peer.");

            await RemoveOwner(2, initialValue);
            await OnUiThread(activity, () =>
            {
                activity.Standard.Value = !sourceStandard;
                activity.Visible.Value = true;
                activity.Pass.Value = 3;
            });
            await WaitFor(() => activity.CompletedPass == 3, "Receiving sheet factory did not commit.");
            await WaitForAnchors(!sourceStandard);
            var successor = activity.Sheet.Jvm;
            Assert.IsNotNull(successor);
            Assert.AreNotSame(original, successor);
            var transferredValue = expanded || (sourceStandard && skipPartial)
                ? SheetValue.Expanded : SheetValue.PartiallyExpanded;
            Assert.AreEqual(transferredValue, activity.Sheet.CurrentValue);
            Assert.AreEqual(transferredValue, activity.Sheet.TargetValue);
            Assert.IsTrue(activity.Sheet.IsVisible);
            await RenderPass(4);
            Assert.AreSame(successor, activity.Sheet.Jvm, "Re-executing the receiving owner replaced its peer.");
            Assert.AreEqual(transferredValue, activity.Sheet.CurrentValue);

            await RemoveOwner(5, transferredValue);
            await OnUiThread(activity, () =>
            {
                activity.Standard.Value = sourceStandard;
                activity.Visible.Value = true;
                activity.Pass.Value = 6;
            });
            await WaitFor(() => activity.CompletedPass == 6, "Returning sheet factory did not commit.");
            await WaitForAnchors(sourceStandard);
            Assert.IsNotNull(activity.Sheet.Jvm);
            Assert.AreNotSame(original, activity.Sheet.Jvm);
            Assert.AreNotSame(successor, activity.Sheet.Jvm);
            var returnedValue = expanded || skipPartial ? SheetValue.Expanded : SheetValue.PartiallyExpanded;
            Assert.AreEqual(returnedValue, activity.Sheet.CurrentValue);
            Assert.AreEqual(returnedValue, activity.Sheet.TargetValue);
            Assert.IsTrue(activity.Sheet.IsVisible);
            Console.WriteLine($"direct={direct}, standard={sourceStandard}, skipPartial={skipPartial}, expanded={expanded}: " +
                $"{initialValue} -> {transferredValue} -> {returnedValue}; three distinct native peers.");

            async Task RenderPass(int pass)
            {
                await OnUiThread(activity, () => activity.Pass.Value = pass);
                await WaitFor(() => activity.CompletedPass == pass, $"Sheet render pass {pass} did not commit.");
            }

            Task WaitForAnchors(bool standard) =>
                WaitFor(() => activity.Sheet.HasExpandedState &&
                    activity.Sheet.HasPartiallyExpandedState == (standard || !skipPartial),
                    $"The {(standard ? "standard" : "modal")} sheet did not install its expected layout anchors.");

            async Task RemoveOwner(int pass, SheetValue? retained)
            {
                await OnUiThread(activity, () =>
                {
                    activity.Visible.Value = false;
                    activity.Pass.Value = pass;
                });
                await WaitFor(() => activity.CompletedPass == pass, $"Sheet removal pass {pass} did not commit.");
                Assert.IsNull(activity.Sheet.Jvm, "Retired sheet owner retained its peer.");
                Assert.AreEqual(retained, activity.Sheet.CurrentValue, "Unbound current value was normalized early.");
                Assert.AreEqual(retained, activity.Sheet.TargetValue, "Unbound target value was normalized early.");
                Assert.AreEqual(retained != SheetValue.Hidden, activity.Sheet.IsVisible);
            }
        }
        finally
        {
            var started = activity ?? SheetStateHandoffTestActivity.Current;
            if (started is not null)
            {
                await OnUiThread(started, started.Finish);
                await WaitFor(() => !ReferenceEquals(SheetStateHandoffTestActivity.Current, started),
                    "Sheet handoff activity did not finish.");
            }
        }
    }

    static Task OnUiThread(SheetStateHandoffTestActivity activity, Action action)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        activity.RunOnUiThread(() =>
        {
            try
            {
                action();
                completion.SetResult();
            }
            catch (Exception error)
            {
                completion.SetException(error);
            }
        });
        return completion.Task.WaitAsync(TimeSpan.FromSeconds(15));
    }

    static Task OnUiThread(SheetStateHandoffTestActivity activity, Func<Task> action)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        activity.RunOnUiThread(async () =>
        {
            try
            {
                await action();
                completion.SetResult();
            }
            catch (Exception error)
            {
                completion.SetException(error);
            }
        });
        return completion.Task.WaitAsync(TimeSpan.FromSeconds(15));
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
