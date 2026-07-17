using AndroidX.Compose;
using AndroidX.Compose.UI.Text;
using SearchBarValue = AndroidX.Compose.Material3.SearchBarValue;
using WideNavigationRailValue = AndroidX.Compose.Material3.WideNavigationRailValue;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Verifies public state-control operations against live Compose peers.</summary>
[TestClass]
[DoNotParallelize]
public class StateControlTests
{
    [TestMethod]
    public async Task StateControls_PreservePendingValuesReusePeersAndRunOperations()
    {
        StateControlTestActivity.Reset();
        var search = Require(StateControlTestActivity.Search, "Search state");
        var searchText = Require(StateControlTestActivity.SearchText, "Search text state");
        var secureText = Require(StateControlTestActivity.SecureText, "Secure text state");

        Assert.AreEqual("pending search", searchText.Text);
        Assert.AreEqual("pending secure", secureText.Text);
        Assert.AreEqual(SearchBarValue.Expanded, search.CurrentValue);
        Assert.AreEqual(1f, search.Progress);

        var activity = await StartActivity();
        try
        {
            var pull = Require(StateControlTestActivity.PullToRefresh, "Pull-to-refresh state");
            var rail = Require(StateControlTestActivity.Rail, "Rail state");
            var pager = Require(StateControlTestActivity.Pager, "Pager state");
            var snackbar = Require(StateControlTestActivity.Snackbar, "Snackbar state");

            var searchJvm = await WaitFor(
                () => search.Jvm,
                static value => value is not null,
                "Search state did not bind.")
                ?? throw new InvalidOperationException("Search JVM peer was unavailable.");
            var searchTextJvm = await WaitFor(
                () => searchText.Jvm,
                static value => value is not null,
                "Search text state did not bind.")
                ?? throw new InvalidOperationException("Search text JVM peer was unavailable.");
            var secureTextJvm = await WaitFor(
                () => secureText.Jvm,
                static value => value is not null,
                "Secure text state did not bind.")
                ?? throw new InvalidOperationException("Secure text JVM peer was unavailable.");
            var pullJvm = await WaitFor(
                () => pull.Jvm,
                static value => value is not null,
                "Pull-to-refresh state did not bind.")
                ?? throw new InvalidOperationException("Pull-to-refresh JVM peer was unavailable.");
            var railJvm = await WaitFor(
                () => rail.Jvm,
                static value => value is not null,
                "Rail state did not bind.")
                ?? throw new InvalidOperationException("Rail JVM peer was unavailable.");

            Assert.AreEqual(TextRangeKt.TextRange(0, "pending search".Length), searchTextJvm.Selection);
            Assert.AreEqual(TextRangeKt.TextRange(0, "pending secure".Length), secureTextJvm.Selection);

            await RunOnUiThread(activity, () =>
            {
                searchText.SetText("live search");
                searchText.SetTextAndSelectAll("selected search");
                secureText.ClearText();
            });
            Assert.AreEqual("selected search", searchText.Text);
            Assert.AreEqual(
                TextRangeKt.TextRange(0, "selected search".Length),
                searchTextJvm.Selection);
            Assert.AreEqual("", secureText.Text);

            await RunOnUiThread(activity, () => pager.ScrollToPageAsync(1, 0.25f));
            Assert.AreEqual(1, pager.CurrentPage);
            await RunOnUiThread(activity, () => pager.AnimateScrollToPageAsync(2));
            Assert.AreEqual(2, pager.CurrentPage);
            await RunOnUiThread(activity, () =>
            {
                pager.RequestScrollToPage(0);
            });
            await WaitFor(
                () => pager.CurrentPage,
                static value => value == 0,
                "Pager request-scroll did not apply.");

            await RunOnUiThread(activity, () => pull.SnapToAsync(1.25f));
            Assert.AreEqual(1.25f, pull.DistanceFraction, 0.01f);
            await RunOnUiThread(activity, () => pull.AnimateToHiddenAsync());
            Assert.AreEqual(0f, pull.DistanceFraction, 0.01f);
            await RunOnUiThread(activity, () => pull.AnimateToThresholdAsync());
            Assert.AreEqual(1f, pull.DistanceFraction, 0.01f);

            await RunOnUiThread(activity, () => search.CollapseAsync());
            Assert.AreEqual(SearchBarValue.Collapsed, search.CurrentValue);
            await RunOnUiThread(activity, () => search.ExpandAsync());
            Assert.AreEqual(SearchBarValue.Expanded, search.CurrentValue);
            await RunOnUiThread(activity, () => search.SnapToAsync(0.5f));
            Assert.AreEqual(0.5f, search.Progress, 0.01f);

            await RunOnUiThread(activity, () => rail.ExpandAsync());
            Assert.AreEqual(WideNavigationRailValue.Expanded, rail.CurrentValue);
            await RunOnUiThread(activity, () => rail.ToggleAsync());
            Assert.AreEqual(WideNavigationRailValue.Collapsed, rail.CurrentValue);
            var expanded = WideNavigationRailValue.Expanded
                ?? throw new InvalidOperationException(
                    "WideNavigationRailValue.Expanded was unavailable.");
            await RunOnUiThread(activity, () => rail.SnapToAsync(expanded));
            Assert.AreEqual(WideNavigationRailValue.Expanded, rail.CurrentValue);
            await RunOnUiThread(activity, () => rail.CollapseAsync());
            Assert.AreEqual(WideNavigationRailValue.Collapsed, rail.CurrentValue);

            var dismissedTask = await RunOnUiThread(
                activity,
                () => Task.FromResult(snackbar.ShowSnackbarAsync(
                    "Dismiss me")));
            var dismissedData = await WaitFor(
                () => snackbar.Jvm.CurrentSnackbarData,
                static value => value is not null,
                "Dismissable snackbar did not enter the host queue.")
                ?? throw new InvalidOperationException(
                    "Dismissable snackbar data was unavailable.");
            Assert.AreEqual(
                global::AndroidX.Compose.Material3.SnackbarDuration.Short,
                dismissedData.Visuals.Duration);
            activity.RunOnUiThread(dismissedData.Dismiss);
            Assert.AreEqual(SnackbarResult.Dismissed, await dismissedTask);

            var actionTask = await RunOnUiThread(
                activity,
                () => Task.FromResult(snackbar.ShowSnackbarAsync(
                    "Act on me",
                    actionLabel: "Act")));
            var actionData = await WaitFor(
                () => snackbar.Jvm.CurrentSnackbarData,
                static value => value is not null,
                "Action snackbar did not enter the host queue.")
                ?? throw new InvalidOperationException(
                    "Action snackbar data was unavailable.");
            Assert.AreEqual(
                global::AndroidX.Compose.Material3.SnackbarDuration.Indefinite,
                actionData.Visuals.Duration);
            activity.RunOnUiThread(actionData.PerformAction);
            Assert.AreEqual(SnackbarResult.ActionPerformed, await actionTask);

            using var cts = new CancellationTokenSource();
            var snackbarTask = await RunOnUiThread(
                activity,
                () => Task.FromResult(snackbar.ShowSnackbarAsync(
                    "Cancel me",
                    duration: SnackbarDuration.Indefinite,
                    cancellationToken: cts.Token)));
            await WaitFor(
                () => snackbar.Jvm.CurrentSnackbarData,
                static value => value is not null,
                "Cancelable snackbar did not enter the host queue.");
            cts.Cancel();
            await Assert.ThrowsExactlyAsync<TaskCanceledException>(
                async () => await snackbarTask);
            await WaitFor(
                () => snackbar.Jvm.CurrentSnackbarData,
                static value => value is null,
                "Cancelled snackbar remained in the host queue.");

            int priorPass = StateControlTestActivity.CompletedRenderPasses;
            activity.RunOnUiThread(() =>
                Require(StateControlTestActivity.Visible, "Visibility state").Value = false);
            await WaitFor(
                static () => StateControlTestActivity.CompletedRenderPasses,
                value => value > priorPass,
                "Controls did not leave composition.");
            priorPass = StateControlTestActivity.CompletedRenderPasses;
            activity.RunOnUiThread(() =>
                Require(StateControlTestActivity.Visible, "Visibility state").Value = true);
            await WaitFor(
                static () => StateControlTestActivity.CompletedRenderPasses,
                value => value > priorPass,
                "Controls did not re-enter composition.");

            Assert.AreEqual(searchJvm.Handle, Require(search.Jvm, "Search JVM peer").Handle);
            Assert.AreEqual(searchTextJvm.Handle, Require(searchText.Jvm, "Search text JVM peer").Handle);
            Assert.AreEqual(secureTextJvm.Handle, Require(secureText.Jvm, "Secure text JVM peer").Handle);
            Assert.AreEqual(pullJvm.Handle, Require(pull.Jvm, "Pull JVM peer").Handle);
            Assert.AreEqual(railJvm.Handle, Require(rail.Jvm, "Rail JVM peer").Handle);
        }
        finally
        {
            activity.RunOnUiThread(activity.Finish);
        }
    }

    static async Task<StateControlTestActivity> StartActivity()
    {
        var context = global::Android.App.Application.Context
            ?? throw new InvalidOperationException(
                "Application.Context not set for state-control tests.");
        using var intent = new global::Android.Content.Intent(
            context,
            typeof(StateControlTestActivity));
        intent.AddFlags(global::Android.Content.ActivityFlags.NewTask);
        context.StartActivity(intent);
        return await WaitFor(
            static () => StateControlTestActivity.Current,
            static value => value is not null,
            "State-control test activity did not start.")
            ?? throw new InvalidOperationException(
                "State-control test activity was unavailable.");
    }

    static Task RunOnUiThread(
        StateControlTestActivity activity,
        Action action)
    {
        var completion = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
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

    static Task RunOnUiThread(
        StateControlTestActivity activity,
        Func<Task> action)
    {
        var completion = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        activity.RunOnUiThread(async () =>
        {
            try
            {
                await action();
                completion.SetResult();
            }
            catch (Exception ex)
            {
                completion.SetException(ex);
            }
        });
        return completion.Task;
    }

    static async Task<T> RunOnUiThread<T>(
        StateControlTestActivity activity,
        Func<Task<T>> action)
    {
        var completion = new TaskCompletionSource<T>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        activity.RunOnUiThread(async () =>
        {
            try
            {
                completion.SetResult(await action());
            }
            catch (Exception ex)
            {
                completion.SetException(ex);
            }
        });
        return await completion.Task;
    }

    static T Require<T>(T? value, string name) where T : class =>
        value ?? throw new InvalidOperationException($"{name} was unavailable.");

    static async Task<T> WaitFor<T>(
        Func<T> read,
        Func<T, bool> predicate,
        string message)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(10);
        T value;
        do
        {
            value = read();
            if (predicate(value))
                return value;
            await Task.Delay(20);
        }
        while (DateTime.UtcNow < deadline);

        Assert.Fail(message);
        return value;
    }
}
