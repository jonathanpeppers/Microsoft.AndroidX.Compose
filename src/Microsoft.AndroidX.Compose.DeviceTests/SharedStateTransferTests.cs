using AndroidX.Compose.Material3;
using NavigationValue = AndroidX.Compose.Material3.Adaptive.NavigationSuite.NavigationSuiteScaffoldValue;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Checks settled-value transfer and native confirm callbacks across owner lifetimes.</summary>
[TestClass]
[DoNotParallelize]
public class SharedStateTransferTests
{
    [TestMethod]
    public async Task NonPickerOwners_TransferSettledValuesAndRefreshConfirmDelegate()
    {
        var context = global::Android.App.Application.Context;
        using var intent = new global::Android.Content.Intent(context, typeof(SharedStateTransferTestActivity));
        intent.AddFlags(global::Android.Content.ActivityFlags.NewTask);
        context.StartActivity(intent);
        await WaitFor(() => SharedStateTransferTestActivity.Current is { CompletedPass: >= 0 },
            "Shared state transfer activity did not start.");
        var activity = SharedStateTransferTestActivity.Current
            ?? throw new InvalidOperationException("Shared state transfer activity was unavailable.");
        try
        {
            var sheet = activity.Sheet.Jvm;
            var drawer = activity.Drawer.Jvm;
            var navigation = activity.Navigation.Jvm;
            Assert.AreEqual(SheetValue.Expanded, activity.Sheet.CurrentValue);
            Assert.AreEqual(DrawerValue.Open, activity.Drawer.CurrentValue);

            Task operation = Task.CompletedTask;
            await OnUiThread(activity, () => operation = activity.HideNavigation());
            await operation.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.AreEqual(NavigationValue.Hidden, activity.Navigation.CurrentValue);
            for (int pass = 1; pass <= 2; pass++)
            {
                int nextPass = pass;
                await OnUiThread(activity, () => activity.Pass.Value = nextPass);
                await WaitFor(() => activity.CompletedPass == nextPass, "Native owner did not execute again.");
                Assert.AreSame(sheet, activity.Sheet.Jvm, "Rebinding a callback must not replace native state.");
                await OnUiThread(activity, () => operation = activity.Sheet.HideAsync());
                await operation.WaitAsync(TimeSpan.FromSeconds(10));
                Assert.AreEqual(nextPass, activity.LastConfirmPass, "Native state called a stale confirm delegate.");
                Assert.AreEqual(SheetValue.Expanded, activity.Sheet.CurrentValue, "Confirm callback must veto hiding.");
            }

            await OnUiThread(activity, () =>
            {
                activity.Visible.Value = false;
                activity.Pass.Value = 3;
            });
            await WaitFor(() => activity.CompletedPass == 3, "Owners did not leave composition.");
            Assert.IsNull(activity.Sheet.Jvm);
            Assert.IsNull(activity.Drawer.Jvm);
            Assert.IsNull(activity.Navigation.Jvm);
            Assert.AreEqual(SheetValue.Expanded, activity.Sheet.CurrentValue);
            Assert.AreEqual(DrawerValue.Open, activity.Drawer.CurrentValue);
            Assert.AreEqual(NavigationValue.Hidden, activity.Navigation.CurrentValue);

            await OnUiThread(activity, () =>
            {
                activity.Visible.Value = true;
                activity.Pass.Value = 4;
            });
            await WaitFor(() => activity.CompletedPass == 4, "Owners did not return.");
            Assert.AreNotSame(sheet, activity.Sheet.Jvm);
            Assert.AreNotSame(drawer, activity.Drawer.Jvm);
            Assert.AreNotSame(navigation, activity.Navigation.Jvm);
            Assert.AreEqual(SheetValue.Expanded, activity.Sheet.CurrentValue);
            Assert.AreEqual(DrawerValue.Open, activity.Drawer.CurrentValue);
            Assert.AreEqual(NavigationValue.Hidden, activity.Navigation.CurrentValue);
        }
        finally
        {
            await OnUiThread(activity, activity.Finish);
            await WaitFor(() => !ReferenceEquals(SharedStateTransferTestActivity.Current, activity),
                "Shared state transfer activity did not finish.");
        }
    }

    static Task OnUiThread(SharedStateTransferTestActivity activity, Action action)
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
