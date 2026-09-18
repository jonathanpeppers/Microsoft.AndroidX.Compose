using AndroidX.Compose;
using AndroidX.Compose.UI.Text;
using AccessibilityNodeInfo = Android.Views.Accessibility.AccessibilityNodeInfo;
using SearchBarValue = AndroidX.Compose.Material3.SearchBarValue;
using WideNavigationRailValue = AndroidX.Compose.Material3.WideNavigationRailValue;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Verifies public state-control operations against live Compose peers.</summary>
[TestClass]
[DoNotParallelize]
public class StateControlTests
{
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task StateControls_PreservePendingValuesReusePeersAndRunOperations(bool direct)
    {
        StateControlTestActivity.Reset();
        var search = Require(StateControlTestActivity.Search, "Search state");
        var searchText = Require(StateControlTestActivity.SearchText, "Search text state");
        var secureText = Require(StateControlTestActivity.SecureText, "Secure text state");

        Assert.AreEqual("pending search", searchText.Text);
        Assert.AreEqual("pending secure", secureText.Text);
        Assert.AreEqual(SearchBarValue.Expanded, search.CurrentValue);
        Assert.AreEqual(1f, search.Progress);

        var activity = await StartActivity(direct);
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
            Assert.AreEqual(
                SearchBarValue.Collapsed,
                await RunOnUiThread(activity, () => Task.FromResult(search.TargetValue)));
            Assert.AreEqual(
                SearchBarValue.Collapsed,
                await RunOnUiThread(activity, () => Task.FromResult(search.CurrentValue)));
            Assert.AreEqual(0f, search.Progress, 0.01f);
            await RunOnUiThread(activity, () => search.ExpandAsync());
            Assert.AreEqual(
                SearchBarValue.Expanded,
                await RunOnUiThread(activity, () => Task.FromResult(search.TargetValue)));
            Assert.AreEqual(
                SearchBarValue.Expanded,
                await RunOnUiThread(activity, () => Task.FromResult(search.CurrentValue)));
            Assert.AreEqual(1f, search.Progress, 0.01f);
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
            await WaitFor(
                () => AccessibilityTextExists("Dismiss me"),
                static visible => visible,
                "SnackbarHost did not render the queued SnackbarData payload.");
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

            Assert.IsTrue(
                global::Android.Runtime.JNIEnv.IsSameObject(
                    searchJvm.Handle,
                    Require(search.Jvm, "Search JVM peer").Handle),
                "Search state mutation replaced its active composition-owned peer.");
            Assert.IsTrue(
                global::Android.Runtime.JNIEnv.IsSameObject(
                    searchTextJvm.Handle,
                    Require(searchText.Jvm, "Search text JVM peer").Handle),
                "Search text mutation replaced its active composition-owned peer.");

            var retainedSearchValue = search.CurrentValue;
            string retainedSearchText = searchText.Text;
            long retainedSearchSelection = searchTextJvm.Selection;
            int priorPass = StateControlTestActivity.CompletedRenderPasses;
            activity.RunOnUiThread(() =>
                Require(StateControlTestActivity.Visible, "Visibility state").Value = false);
            await WaitFor(
                static () => StateControlTestActivity.CompletedRenderPasses,
                value => value > priorPass,
                "Controls did not leave composition.");
            await WaitFor(
                () => search.Jvm is null && searchText.Jvm is null,
                static released => released,
                "Search owners retained native peers after full removal.");
            Assert.AreEqual(retainedSearchValue, search.CurrentValue);
            Assert.AreEqual(retainedSearchText, searchText.Text);
            priorPass = StateControlTestActivity.CompletedRenderPasses;
            activity.RunOnUiThread(() =>
                Require(StateControlTestActivity.Visible, "Visibility state").Value = true);
            await WaitFor(
                static () => StateControlTestActivity.CompletedRenderPasses,
                value => value > priorPass,
                "Controls did not re-enter composition.");

            var reboundSearch = Require(search.Jvm, "Search JVM peer");
            var reboundSearchText = Require(searchText.Jvm, "Search text JVM peer");
            Assert.IsFalse(
                global::Android.Runtime.JNIEnv.IsSameObject(
                    searchJvm.Handle, reboundSearch.Handle));
            Assert.IsFalse(
                global::Android.Runtime.JNIEnv.IsSameObject(
                    searchTextJvm.Handle, reboundSearchText.Handle));
            Assert.AreEqual(retainedSearchValue, search.CurrentValue);
            Assert.AreEqual(retainedSearchText, searchText.Text);
            Assert.AreEqual(retainedSearchSelection, reboundSearchText.Selection);
            Assert.AreEqual(secureTextJvm.Handle, Require(secureText.Jvm, "Secure text JVM peer").Handle);
            Assert.AreEqual(pullJvm.Handle, Require(pull.Jvm, "Pull JVM peer").Handle);
            Assert.AreEqual(railJvm.Handle, Require(rail.Jvm, "Rail JVM peer").Handle);
        }
        finally
        {
            activity.RunOnUiThread(activity.Finish);
            await WaitFor(
                static () => StateControlTestActivity.Current,
                static current => current is null,
                "State-control test activity did not finish.");
        }
    }

    static bool AccessibilityTextExists(string expected)
    {
        var automation = TestInstrumentation.Current?.UiAutomation
            ?? throw new InvalidOperationException(
                "Instrumentation UI automation is unavailable.");
        using var root = automation.RootInActiveWindow;
        if (root is null || root.PackageName != "net.compose.devicetests")
            return false;
        return Visit(root);

        bool Visit(AccessibilityNodeInfo node)
        {
            if (node.Text == expected)
                return true;
            for (int i = 0; i < node.ChildCount; i++)
            {
                using var child = node.GetChild(i);
                if (child is not null && Visit(child))
                    return true;
            }
            return false;
        }
    }

    static async Task<StateControlTestActivity> StartActivity(bool direct)
    {
        var context = global::Android.App.Application.Context
            ?? throw new InvalidOperationException(
                "Application.Context not set for state-control tests.");
        using var intent = new global::Android.Content.Intent(
            context,
            typeof(StateControlTestActivity));
        intent.AddFlags(global::Android.Content.ActivityFlags.NewTask);
        intent.PutExtra("direct", direct);
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
