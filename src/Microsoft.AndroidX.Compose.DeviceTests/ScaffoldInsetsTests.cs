using Android.Runtime;
using Edges = (float Left, float Top, float Right, float Bottom);

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Native layout regressions for Scaffold inset forwarding, recomposition, and recreation.</summary>
[TestClass]
[DoNotParallelize]
public class ScaffoldInsetsTests
{
    /// <summary>Runs one existing matrix row independently to verify native admission before the full suite.</summary>
    [TestMethod]
    public Task InitialAdmissionControl() =>
        OmittedContentWindowInsets_UsesBoundMaterialDefault(0, false);

    /// <summary>Verifies a second native activity can be admitted after the first one is destroyed.</summary>
    [TestMethod]
    public async Task InitialAdmissionTurnoverControl()
    {
        await OmittedContentWindowInsets_UsesBoundMaterialDefault(0, false);
        await OmittedContentWindowInsets_UsesBoundMaterialDefault(0, true);
    }

    /// <summary>Runs the existing first transition/recreation row independently of earlier activities.</summary>
    [TestMethod]
    public Task TransitionInitialControl() =>
        ContentWindowInsets_AllStyles_RecomposeAndRecreate(0, false);

    /// <summary>Checks literal argument omission independently of the live supplied-argument call site.</summary>
    [TestMethod]
    [DataRow(1, false)] [DataRow(1, true)]
    [DataRow(2, false)] [DataRow(2, true)]
    [DataRow(3, false)] [DataRow(3, true)]
    public async Task OmittedContentWindowInsets_UsesBoundMaterialDefault(int style, bool bars)
    {
        ScaffoldInsetsTestActivity? activity = null;
        try
        {
            ScaffoldInsetsTestActivity.Configure(style, bars, ScaffoldInsetsTestActivity.Omitted);
            activity = await StartActivity();
            var snapshot = await activity.WaitForSnapshotAsync(0);
            Assert.AreEqual(ScaffoldInsetsTestActivity.Omitted, snapshot.Mode);
            AssertLayout(snapshot, bars);
        }
        finally
        {
            await FinishActivity(activity);
        }
    }

    /// <summary>Exercises both tree body paths and both adapters, with and without real app bars.</summary>
    [TestMethod]
    [DataRow(0, true)]
    [DataRow(1, false)] [DataRow(1, true)]
    [DataRow(2, false)] [DataRow(2, true)]
    [DataRow(3, false)] [DataRow(3, true)]
    public async Task ContentWindowInsets_AllStyles_RecomposeAndRecreate(int style, bool bars)
    {
        ScaffoldInsetsTestActivity? activity = null;
        try
        {
            ScaffoldInsetsTestActivity.Configure(style, bars);
            activity = await StartActivity();
            var initial = await activity.WaitForSnapshotAsync(0);
            Assert.AreEqual(0, initial.CounterValue, "The initial saveable counter must be fresh.");
            var before = await ExerciseInsets(activity, bars, initial);
            int beforeMutation = 0;
            await RunOnUiThread(activity, () =>
            {
                beforeMutation = activity.BodyObservationVersion;
                before.Counter.Value = 84;
            });
            var mutated = await activity.WaitForSnapshotAsync(before.Generation, beforeMutation);
            AssertRetainedState(before, mutated, 84);
            before = mutated;

            var previous = activity;
            await RunOnUiThread(previous, () =>
            {
                previous.ExpectLifecycleEnd();
                previous.Recreate();
            });
            activity = await ScaffoldInsetsTestActivity.WaitForActivityAsync(previous);
            Assert.AreNotSame(previous, activity, "Recreate must start a fresh activity and composition.");
            await previous.Destroyed.WaitAsync(TimeSpan.FromSeconds(10));
            var restored = await activity.WaitForSnapshotAsync(0);
            Assert.AreEqual(84, restored.CounterValue,
                "Scaffold body saveable state did not survive activity recreation.");
            Assert.AreNotSame(before.BodySentinel, restored.BodySentinel,
                "Ordinary remembered state must be fresh in the recreated composition.");
            Assert.AreNotSame(before.Counter, restored.Counter,
                "Recreation must restore a new counter, not reuse the old state object.");
            var after = await ExerciseInsets(activity, bars, restored);

            AssertEdges(before.Forwarded, after.Forwarded, 1 / after.Density,
                "Recreation changed the restored default padding.");
            AssertEdges(before.Body, after.Body, 1, "Recreation changed the default body bounds.");
        }
        finally
        {
            await FinishActivity(activity);
        }
    }

    static async Task<ScaffoldInsetsSnapshot> ExerciseInsets(
        ScaffoldInsetsTestActivity activity, bool bars, ScaffoldInsetsSnapshot initial)
    {
        AssertLayout(initial, bars);
        int counterValue = initial.CounterValue + 7;
        int beforeMutation = 0;
        await RunOnUiThread(activity, () =>
        {
            beforeMutation = activity.BodyObservationVersion;
            initial.Counter.Value = counterValue;
        });
        var baseline = await activity.WaitForSnapshotAsync(initial.Generation, beforeMutation);
        AssertRetainedState(initial, baseline, counterValue);
        int[] modes =
        [
            ScaffoldInsetsTestActivity.Default,
            ScaffoldInsetsTestActivity.Zero,
            ScaffoldInsetsTestActivity.Excluded,
            ScaffoldInsetsTestActivity.Default,
            ScaffoldInsetsTestActivity.ExplicitNull,
            ScaffoldInsetsTestActivity.Fixed,
            ScaffoldInsetsTestActivity.CompoundZero,
            ScaffoldInsetsTestActivity.ExplicitNull,
        ];
        ScaffoldInsetsSnapshot latest = baseline;
        foreach (int mode in modes)
        {
            int generation = -1;
            await RunOnUiThread(activity, () => generation = activity.ChangeInsets(mode));
            latest = await activity.WaitForSnapshotAsync(generation);
            Assert.AreEqual(mode, latest.Mode, "Received a stale inset measurement.");
            AssertRetainedState(baseline, latest, counterValue);
            AssertLayout(latest, bars);
        }
        AssertEdges(initial.Forwarded, latest.Forwarded, 1 / latest.Density,
            "Returning to null insets did not restore default padding.");
        return latest;
    }

    static void AssertRetainedState(
        ScaffoldInsetsSnapshot expected, ScaffoldInsetsSnapshot actual, int counterValue)
    {
        string context = $"mode={actual.Mode}, generation={actual.Generation}";
        Assert.AreSame(expected.BodySentinel, actual.BodySentinel,
            $"Scaffold remounted its remembered body subtree ({context}).");
        Assert.AreSame(expected.Counter, actual.Counter,
            $"Scaffold replaced the live saveable counter ({context}).");
        Assert.AreEqual(counterValue, actual.CounterValue,
            $"Scaffold lost a mutation to its body counter ({context}).");
#if DEBUG
        Assert.IsTrue(JNIEnv.IsSameObject(expected.ContentLambda, actual.ContentLambda),
            $"Scaffold replaced the actual native content argument ({context}).");
#endif
    }

    static void AssertLayout(ScaffoldInsetsSnapshot snapshot, bool bars)
    {
        string context = $"mode={snapshot.Mode}, generation={snapshot.Generation}, bars={bars}, " +
            $"root={snapshot.Root}, body={snapshot.Body}, forwardedDp={snapshot.Forwarded}; {snapshot.Trace}";
        Console.WriteLine(context);
        Assert.AreEqual(snapshot.ExpectedMarkerWidth, snapshot.ActualMarkerWidth,
            $"Independent generation marker did not complete the requested native layout ({context}).");
        AssertEdges(snapshot.BoundDefault, snapshot.ExplicitReader, 0.01f,
            $"Explicit default reader differs from bound ScaffoldDefaults ({context}).");
        AssertEdges(snapshot.BoundDefault, snapshot.ImplicitReader, 0.01f,
            $"Implicit default reader differs from bound ScaffoldDefaults ({context}).");
        AssertEdges((0, 0, snapshot.WindowWidth, snapshot.WindowHeight), snapshot.Root, 1,
            $"Scaffold must fill the edge-to-edge window ({context}).");

        Edges insets = snapshot.Mode switch
        {
            ScaffoldInsetsTestActivity.Zero or ScaffoldInsetsTestActivity.CompoundZero => (0, 0, 0, 0),
            ScaffoldInsetsTestActivity.Fixed => (13, 23, 31, 41),
            ScaffoldInsetsTestActivity.Excluded => Exclude(
                Exclude(snapshot.BoundDefault, snapshot.NavigationBars), snapshot.Ime),
            _ => snapshot.BoundDefault,
        };
        Edges expected = Scale(insets, snapshot.Density);
#if DEBUG
        var native = snapshot.NativeInsetsAtPlacement
            ?? throw new InvalidOperationException($"Native Scaffold insets were not observed ({context}).");
        AssertEdges(expected, native, 1, $"The actual bound contentWindowInsets argument is incorrect ({context}).");
#endif
        if (bars)
        {
            var top = snapshot.TopBar
                ?? throw new InvalidOperationException($"Top bar was not measured ({context}).");
            var bottom = snapshot.BottomBar
                ?? throw new InvalidOperationException($"Bottom bar was not measured ({context}).");
            Assert.IsTrue(top.Bottom > top.Top && bottom.Bottom > bottom.Top,
                $"Actual Material app bars must have positive height ({context}).");
            Assert.AreEqual(snapshot.Root.Top, top.Top, 1, $"Top bar moved ({context}).");
            Assert.AreEqual(snapshot.Root.Bottom, bottom.Bottom, 1, $"Bottom bar moved ({context}).");
            // Scaffold replaces the vertical insets with measured bar heights, not their sum.
            expected.Top = top.Bottom - top.Top;
            expected.Bottom = bottom.Bottom - bottom.Top;
        }
        else
        {
            Assert.IsNull(snapshot.TopBar);
            Assert.IsNull(snapshot.BottomBar);
        }

        AssertEdges(expected, Scale(snapshot.Forwarded, snapshot.Density), 1,
            $"Forwarded PaddingValues are incorrect ({context}).");
        Edges expectedBody = (
            snapshot.Root.Left + expected.Left,
            snapshot.Root.Top + expected.Top,
            snapshot.Root.Right - expected.Right,
            snapshot.Root.Bottom - expected.Bottom);
        Assert.IsTrue(snapshot.Body.Right > snapshot.Body.Left
            && snapshot.Body.Bottom > snapshot.Body.Top, $"Body was not laid out ({context}).");
        AssertEdges(expectedBody, snapshot.Body, 1,
            $"Content padding must be applied exactly once ({context}).");
    }

    static Edges Exclude(Edges source, Edges excluded) => (
        Math.Max(0, source.Left - excluded.Left),
        Math.Max(0, source.Top - excluded.Top),
        Math.Max(0, source.Right - excluded.Right),
        Math.Max(0, source.Bottom - excluded.Bottom));

    static Edges Scale(Edges value, float density) => (
        value.Left * density, value.Top * density, value.Right * density, value.Bottom * density);

    static void AssertEdges(Edges expected, Edges actual, float tolerance, string message)
    {
        Assert.AreEqual(expected.Left, actual.Left, tolerance, $"Left: {message}");
        Assert.AreEqual(expected.Top, actual.Top, tolerance, $"Top: {message}");
        Assert.AreEqual(expected.Right, actual.Right, tolerance, $"Right: {message}");
        Assert.AreEqual(expected.Bottom, actual.Bottom, tolerance, $"Bottom: {message}");
    }

    static async Task<ScaffoldInsetsTestActivity> StartActivity()
    {
        var context = global::Android.App.Application.Context;
        var instrumentation = TestInstrumentation.Current
            ?? throw new InvalidOperationException("Test instrumentation is not running.");
        var launched = await Task.Run(() =>
        {
            using var intent = new global::Android.Content.Intent(context, typeof(ScaffoldInsetsTestActivity));
            intent.AddFlags(global::Android.Content.ActivityFlags.NewTask);
            var started = instrumentation.StartActivitySync(intent);
            instrumentation.WaitForIdleSync();
            return started;
        }).WaitAsync(TimeSpan.FromSeconds(10));
        var activity = await ScaffoldInsetsTestActivity.WaitForActivityAsync();
        Assert.IsNotNull(launched);
        Assert.IsTrue(JNIEnv.IsSameObject(launched.Handle, activity.Handle),
            "Instrumentation returned a different activity from the published Scaffold test instance.");
        return activity;
    }

    static async Task FinishActivity(ScaffoldInsetsTestActivity? activity)
    {
        try
        {
            var current = ScaffoldInsetsTestActivity.Current ?? activity;
            if (current is not null && !current.Destroyed.IsCompleted)
            {
                await RunOnUiThread(current, () =>
                {
                    current.ExpectLifecycleEnd();
                    current.Finish();
                });
                await current.Destroyed.WaitAsync(TimeSpan.FromSeconds(10));
            }
        }
        finally
        {
#if DEBUG
            await ScaffoldInsetsLambdaObserver.ReleaseActiveAsync();
#endif
        }
    }

    static async Task RunOnUiThread(ScaffoldInsetsTestActivity activity, Action action)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        activity.RunOnUiThread(() =>
        {
            try
            {
                action();
                completion.TrySetResult();
            }
            catch (Exception ex)
            {
                completion.TrySetException(ex);
            }
        });
        await completion.Task.WaitAsync(TimeSpan.FromSeconds(10));
    }
}
