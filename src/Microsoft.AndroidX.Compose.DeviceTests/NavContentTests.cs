namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Exercises destination refresh independently of graph and back-stack identity.</summary>
[TestClass]
[DoNotParallelize]
public class NavContentTests
{
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task ActiveRoute_RefreshesValuesAndCallbacksWithoutReset(bool staticChildren)
    {
        var activity = await StartActivity(staticChildren);
        try
        {
            var first = await WaitForObservation("home", 0);
            var firstContent = NavContentTestActivity.LatestHomeContent
                ?? throw new InvalidOperationException("Initial content reference was not captured.");
            var controller = NavContentTestActivity.Controller.Jvm
                ?? throw new InvalidOperationException("Test controller was not bound.");
            var graph = controller.Graph.Handle;
            var entry = controller.CurrentBackStackEntry?.Handle;

            for (int generation = 1; generation <= 3; generation++)
            {
                await UpdateParent(activity, generation);
                var current = await WaitForObservation("home", generation);
                Assert.AreSame(first.Remembered, current.Remembered);
                Assert.AreEqual(graph, controller.Graph.Handle);
                Assert.AreEqual(entry, controller.CurrentBackStackEntry?.Handle);
                await OnUiThread(activity, () => current.Callback.Invoke());
                Assert.AreEqual(current.Label, NavContentTestActivity.Clicked);
            }

            await AssertCollected(firstContent, "The graph retained replaced destination content.");
            int passes = NavContentTestActivity.ParentPasses;
            await Task.Delay(300);
            Assert.AreEqual(passes, NavContentTestActivity.ParentPasses,
                "Publishing destination content must not subscribe/invalidate the parent.");
        }
        finally
        {
            await FinishActivity(activity);
        }
    }

    [TestMethod]
    public async Task FactoryAndStaticContent_CanReplaceEachOther()
    {
        var activity = await StartActivity(false);
        try
        {
            var first = await WaitForObservation("home", 0);
            NavContentTestActivity.SwitchContentShape = true;
            for (int generation = 1; generation <= 2; generation++)
            {
                await UpdateParent(activity, generation);
                var current = await WaitForObservation("home", generation);
                Assert.AreSame(first.Remembered, current.Remembered);
                await OnUiThread(activity, () => current.Callback.Invoke());
                Assert.AreEqual(current.Label, NavContentTestActivity.Clicked);
            }
        }
        finally
        {
            await FinishActivity(activity);
        }
    }

    [TestMethod]
    public async Task RemovingHost_DuringTransitionReleasesContentWithControllerRetained()
    {
        var activity = await StartActivity(false);
        try
        {
            await WaitForObservation("home", 0);
            var content = NavContentTestActivity.LatestHomeContent
                ?? throw new InvalidOperationException("Destination content reference was missing.");
            var controller = NavContentTestActivity.Controller.Jvm
                ?? throw new InvalidOperationException("Test controller was not bound.");
            var graph = controller.Graph;
            await OnUiThread(activity, () => NavContentTestActivity.Controller.Navigate("detail"));
            await WaitForObservation("detail", 0);
            int passes = NavContentTestActivity.ParentPasses;
            await OnUiThread(activity, () => NavContentTestActivity.Visible.Value = false);
            await WaitFor(() => NavContentTestActivity.ParentPasses > passes, "Host was not removed.");
            await WaitFor(() => NavContentTestActivity.Unmounts == 1, "The host subtree did not leave composition.");
            await WaitFor(() => NavContentTestActivity.DestinationMounts >= 2 &&
                NavContentTestActivity.DestinationMounts == NavContentTestActivity.DestinationUnmounts,
                "The native host's deferred destination scopes did not leave composition.");
            await Task.Delay(800);
            await AssertCollected(content, "Host disposal retained destination content in the external controller.");
            GC.KeepAlive(graph);
            GC.KeepAlive(controller);

            await OnUiThread(activity, () =>
            {
                NavContentTestActivity.Generation.Value = 1;
                NavContentTestActivity.IncludeExtraRoute = true;
                NavContentTestActivity.Visible.Value = true;
            });
            await WaitForObservation("home", 1);
            await OnUiThread(activity, () => NavContentTestActivity.Controller.Navigate("extra"));
            await WaitForObservation("extra", 1);
        }
        finally
        {
            await FinishActivity(activity);
        }
    }

    [TestMethod]
    public async Task ChangedStartDestination_ReplacesGraphAndPublishesNewContent()
    {
        var activity = await StartActivity(false);
        try
        {
            await WaitForObservation("home", 0);
            var controller = NavContentTestActivity.Controller.Jvm
                ?? throw new InvalidOperationException("Test controller was not bound.");
            var graph = controller.Graph;
            await OnUiThread(activity, () =>
            {
                NavContentTestActivity.Generation.Value = 1;
                NavContentTestActivity.IncludeExtraRoute = true;
                NavContentTestActivity.StartDestination.Value = "detail";
            });
            await WaitForObservation("detail", 1);
            Assert.AreNotEqual(graph.Handle, controller.Graph.Handle);
            Assert.AreEqual("detail", controller.Graph.StartDestinationRoute);
            Assert.IsNull(controller.Graph.FindNode("extra"),
                "Changing the start destination must still use the original graph builder.");
            GC.KeepAlive(graph);
        }
        finally
        {
            await FinishActivity(activity);
        }
    }

    [TestMethod]
    public async Task RouteListEdits_PreserveTopologyAndRefreshOnlyMatchingDefinitions()
    {
        var activity = await StartActivity(false);
        try
        {
            await WaitForObservation("home", 0);
            var controller = NavContentTestActivity.Controller.Jvm
                ?? throw new InvalidOperationException("Test controller was not bound.");
            var graph = controller.Graph.Handle;
            await OnUiThread(activity, () =>
            {
                NavContentTestActivity.ReorderRoutes = true;
                NavContentTestActivity.IncludeExtraRoute = true;
            });
            await UpdateParent(activity, 1);
            await WaitForObservation("home", 1);
            Assert.AreEqual(graph, controller.Graph.Handle);
            Assert.IsNull(controller.Graph.FindNode("extra"));
            await OnUiThread(activity, () => NavContentTestActivity.Controller.Navigate("detail"));
            await WaitForObservation("detail", 1);

            await OnUiThread(activity, () => NavContentTestActivity.OmitDetail = true);
            await UpdateParent(activity, 2);
            await Task.Delay(300);
            var retained = await WaitForObservation("detail", 1);
            await OnUiThread(activity, () => retained.Callback.Invoke());
            Assert.AreEqual(retained.Label, NavContentTestActivity.Clicked);
            await OnUiThread(activity, () => Assert.IsTrue(NavContentTestActivity.Controller.PopBackStack()));
            await WaitForObservation("home", 2);
            Assert.AreEqual(graph, controller.Graph.Handle);
        }
        finally
        {
            await FinishActivity(activity);
        }
    }

    [TestMethod]
    public async Task KotlinDuplicateRegistration_UsesLastDestinationWithoutContentPublication()
    {
        var activity = await StartActivity(false, rawDuplicateGraph: true);
        try
        {
            await WaitForObservation("home", 10);
        }
        finally
        {
            await FinishActivity(activity);
        }
    }

    [TestMethod]
    public async Task DuplicateRoutes_KeepKotlinsLastRegistrationAndRefreshLastDefinition()
    {
        var activity = await StartActivity(false, duplicateHome: true);
        try
        {
            await WaitForObservation("home", 10);
            await UpdateParent(activity, 1);
            var last = await WaitForObservation("home", 11);
            await OnUiThread(activity, () => last.Callback.Invoke());
            Assert.AreEqual(last.Label, NavContentTestActivity.Clicked);
            await OnUiThread(activity, () => NavContentTestActivity.DuplicateHome = false);
            await UpdateParent(activity, 2);
            await WaitForObservation("home", 2);
        }
        finally
        {
            await FinishActivity(activity);
        }
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task RevisitedRoute_UsesLatestContentAndKeepsBackStack(bool staticChildren)
    {
        var activity = await StartActivity(staticChildren);
        try
        {
            await WaitForObservation("home", 0);
            var controller = NavContentTestActivity.Controller.Jvm
                ?? throw new InvalidOperationException("Test controller was not bound.");
            var graph = controller.Graph.Handle;
            var homeEntry = controller.CurrentBackStackEntry?.Handle;

            await OnUiThread(activity, () => NavContentTestActivity.Controller.Navigate("detail"));
            await WaitForObservation("detail", 0);
            var detailEntry = controller.CurrentBackStackEntry?.Handle;
            await Task.Delay(800); // Let the outgoing destination leave its transition.
            await UpdateParent(activity, 1);
            var detail = await WaitForObservation("detail", 1);
            Assert.AreEqual(graph, controller.Graph.Handle);
            Assert.AreEqual(detailEntry, controller.CurrentBackStackEntry?.Handle);
            await OnUiThread(activity, () => detail.Callback.Invoke());
            Assert.AreEqual(detail.Label, NavContentTestActivity.Clicked);

            await OnUiThread(activity, () => Assert.IsTrue(NavContentTestActivity.Controller.PopBackStack()));
            var home = await WaitForObservation("home", 1);
            Assert.AreEqual(homeEntry, controller.CurrentBackStackEntry?.Handle);
            Assert.AreEqual(graph, controller.Graph.Handle);
            await OnUiThread(activity, () => home.Callback.Invoke());
            Assert.AreEqual(home.Label, NavContentTestActivity.Clicked);

            await OnUiThread(activity, () => NavContentTestActivity.Controller.Navigate("detail"));
            await WaitForObservation("detail", 1);
            await UpdateParent(activity, 2);
            await WaitForObservation("detail", 2);
            Assert.AreEqual(graph, controller.Graph.Handle);
        }
        finally
        {
            await FinishActivity(activity);
        }
    }

    static async Task UpdateParent(NavContentTestActivity activity, int generation)
    {
        int priorPasses = NavContentTestActivity.ParentPasses;
        await OnUiThread(activity, () => NavContentTestActivity.Generation.Value = generation);
        await WaitFor(() => NavContentTestActivity.ParentPasses > priorPasses,
            "The parent did not rebuild its NavHost.");
    }

    static async Task<NavContentObservation> WaitForObservation(string route, int generation)
    {
        NavContentObservation? Read() => route == "home"
            ? NavContentTestActivity.Home : NavContentTestActivity.Detail;
        await WaitFor(() => Read()?.Label == $"{route}: Account {generation}",
            $"Destination {route} did not refresh to render-local Account {generation}.");
        return Read() ?? throw new InvalidOperationException("Destination observation was missing.");
    }

    static async Task<NavContentTestActivity> StartActivity(
        bool staticChildren, bool duplicateHome = false, bool rawDuplicateGraph = false)
    {
        var context = global::Android.App.Application.Context;
        NavContentTestActivity.Reset(staticChildren);
        NavContentTestActivity.DuplicateHome = duplicateHome;
        NavContentTestActivity.RawDuplicateGraph = rawDuplicateGraph;
        using var intent = new global::Android.Content.Intent(context, typeof(NavContentTestActivity));
        intent.AddFlags(global::Android.Content.ActivityFlags.NewTask);
        context.StartActivity(intent);
        await WaitFor(() => NavContentTestActivity.Current is not null, "Navigation test activity did not start.");
        return NavContentTestActivity.Current
            ?? throw new InvalidOperationException("Navigation test activity was missing.");
    }

    static async Task FinishActivity(NavContentTestActivity activity)
    {
        await OnUiThread(activity, activity.Finish);
        await WaitFor(() => !ReferenceEquals(NavContentTestActivity.Current, activity),
            "Navigation test activity did not finish.");
    }

    static Task OnUiThread(NavContentTestActivity activity, Action action)
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
        return completion.Task;
    }

    static async Task WaitFor(Func<bool> predicate, string message)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(10);
        while (!predicate())
        {
            if (DateTime.UtcNow >= deadline)
                Assert.Fail(message);
            await Task.Delay(20);
        }
    }

    internal static async Task AssertCollected(WeakReference reference, string message)
    {
        for (int attempt = 0; attempt < 10; attempt++)
        {
            Java.Lang.Runtime.GetRuntime()?.Gc();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            if (!reference.IsAlive)
                return;
            await Task.Delay(100);
        }
        Assert.IsFalse(reference.IsAlive, message);
    }
}
