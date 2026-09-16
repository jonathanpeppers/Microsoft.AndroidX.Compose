using System.Reflection;
using System.Runtime.CompilerServices;
using Android.Runtime;
using AndroidX.Compose;
using Constraints = AndroidX.Compose.Constraints;
using Process = Android.OS.Process;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Exercises all six JNI getters and clamp helpers across GC inside real Layout measurement.</summary>
[TestClass]
[DoNotParallelize]
public class ConstraintsMeasurementTests
{
    const int Passes = 24;
    static WeakReference<Java.Lang.Class>? s_legacyPeer;

    public TestContext? TestContext { get; set; }

    /// <summary>Runs the same layout/GC control without calling any Constraints getter.</summary>
    [TestMethod]
    public Task MeasurementControl_ExcludesAccessors() => Run(readAccessors: false, firstAccessor: 0);

    /// <summary>Rotates the first post-GC getter so each JNI entry can be identified independently.</summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(4)]
    [DataRow(5)]
    public Task MeasurementAccessors_SurviveCollection(int firstAccessor) =>
        Run(readAccessors: true, firstAccessor);

    async Task Run(bool readAccessors, int firstAccessor)
    {
        ConstraintsMeasurementTestActivity.Reset(readAccessors, firstAccessor);
        s_legacyPeer = null;
        var testContext = TestContext ?? throw new InvalidOperationException("Test context was not supplied.");
        testContext.WriteLine($"run={ConstraintsMeasurementTestActivity.RunId} pid={Process.MyPid()} " +
            $"getters={readAccessors} first={firstAccessor}");
        Log($"start getters={readAccessors} first={firstAccessor}");
        var context = global::Android.App.Application.Context;
        using var intent = new global::Android.Content.Intent(context, typeof(ConstraintsMeasurementTestActivity));
        intent.AddFlags(global::Android.Content.ActivityFlags.NewTask);
        context.StartActivity(intent);
        await WaitFor(() => ConstraintsMeasurementTestActivity.Current is not null, "Activity did not start.");
        var activity = ConstraintsMeasurementTestActivity.Current
            ?? throw new InvalidOperationException("Constraints measurement activity did not publish itself.");
        try
        {
            for (int phase = 0; phase < Passes; phase++)
            {
                if (phase > 0)
                {
                    int next = phase;
                    await OnUi(activity, () => ConstraintsMeasurementTestActivity.Phase.Value = next);
                }
                await WaitFor(() => Volatile.Read(ref ConstraintsMeasurementTestActivity.PlacedPhase) == phase &&
                    Volatile.Read(ref ConstraintsMeasurementTestActivity.Compositions) >= phase + 1,
                    $"Phase {phase} did not both compose and measure/place.");
            }
            Assert.IsGreaterThanOrEqualTo(Passes, Volatile.Read(ref ConstraintsMeasurementTestActivity.Measurements));
            string result = $"run={ConstraintsMeasurementTestActivity.RunId} pid={Process.MyPid()} " +
                $"getters={readAccessors} first={firstAccessor} " +
                $"compositions={ConstraintsMeasurementTestActivity.Compositions} measurements={ConstraintsMeasurementTestActivity.Measurements} " +
                $"placedPhase={ConstraintsMeasurementTestActivity.PlacedPhase}";
            testContext.WriteLine(result);
            Log(result);
        }
        finally
        {
            await OnUi(activity, activity.Finish);
            await WaitFor(() => ConstraintsMeasurementTestActivity.Current is null, "Activity did not finish.");
        }
    }

    internal static void CheckAccessors(Constraints constraints,
        (int MinWidth, int MaxWidth, int MinHeight, int MaxHeight) bounds, bool collect)
    {
        // Prime only through the production facade. Do not create or retain an alternate class owner.
        ReadAll(constraints, bounds, trace: false);
        CaptureLegacyPeer();
        if (collect)
            Collect();
        LogLegacyPeer();
        ReadAll(constraints, bounds, trace: true);
        Assert.AreEqual(bounds.MinWidth, constraints.ConstrainWidth(-1));
        Assert.AreEqual(bounds.MaxWidth, constraints.ConstrainWidth(int.MaxValue));
        Assert.AreEqual(Math.Clamp(80, bounds.MinWidth, bounds.MaxWidth), constraints.ConstrainWidth(80));
        Assert.AreEqual(bounds.MinHeight, constraints.ConstrainHeight(-1));
        Assert.AreEqual(bounds.MaxHeight, constraints.ConstrainHeight(int.MaxValue));
        Assert.AreEqual(Math.Clamp(90, bounds.MinHeight, bounds.MaxHeight), constraints.ConstrainHeight(90));
    }

    static void ReadAll(Constraints constraints,
        (int MinWidth, int MaxWidth, int MinHeight, int MaxHeight) bounds, bool trace)
    {
        for (int i = 0; i < 6; i++)
        {
            int accessor = (ConstraintsMeasurementTestActivity.FirstAccessor + i) % 6;
            if (trace)
                Log($"before accessor={accessor} class={ClassHandle(accessor)}");
            switch (accessor)
            {
                case 0: Assert.AreEqual(bounds.MinWidth, constraints.MinWidth); break;
                case 1: Assert.AreEqual(bounds.MaxWidth, constraints.MaxWidth); break;
                case 2: Assert.AreEqual(bounds.MinHeight, constraints.MinHeight); break;
                case 3: Assert.AreEqual(bounds.MaxHeight, constraints.MaxHeight); break;
                case 4: Assert.AreEqual(bounds.MaxWidth != int.MaxValue, constraints.HasBoundedWidth); break;
                case 5: Assert.AreEqual(bounds.MaxHeight != int.MaxValue, constraints.HasBoundedHeight); break;
            }
        }
    }

    internal static void Collect()
    {
        for (int i = 0; i < 3; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Java.Lang.JavaSystem.Gc();
            Java.Lang.JavaSystem.RunFinalization();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void CaptureLegacyPeer()
    {
        if (s_legacyPeer is not null || LegacyHandle() is not { } handle)
            return;
        var peer = Java.Lang.Object.GetObject<Java.Lang.Class>(handle, JniHandleOwnership.DoNotTransfer)
            ?? throw new InvalidOperationException("Primed legacy Constraints class peer was unavailable.");
        s_legacyPeer = new(peer, trackResurrection: true);
        Log($"legacy owner captured weakly handle=0x{peer.Handle.ToInt64():x}");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void LogLegacyPeer()
    {
        if (s_legacyPeer is null)
            return;
        Log(s_legacyPeer.TryGetTarget(out var peer)
            ? $"legacy owner after GC handle=0x{peer.Handle.ToInt64():x}"
            : "legacy owner after GC collected");
    }

    static IntPtr? LegacyHandle() => typeof(ComposeBridges).GetField("s_constraintsClass",
        BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null) is IntPtr handle ? handle : null;

    static string ClassHandle(int accessor)
    {
        string[] methods = ["ConstraintsGetMinWidth", "ConstraintsGetMaxWidth", "ConstraintsGetMinHeight",
            "ConstraintsGetMaxHeight", "ConstraintsHasBoundedWidth", "ConstraintsHasBoundedHeight"];
        var handle = LegacyHandle() ?? (typeof(ComposeBridges).GetField($"s_{methods[accessor]}_class",
            BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null) is IntPtr generated ? generated : IntPtr.Zero);
        return $"0x{handle.ToInt64():x}";
    }

    static void Log(string message) => global::Android.Util.Log.Info("ConstraintsMeasurement",
        $"run={ConstraintsMeasurementTestActivity.RunId} pid={Process.MyPid()} {message}");

    static Task OnUi(ConstraintsMeasurementTestActivity activity, Action action)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        activity.RunOnUiThread(() =>
        {
            try { action(); completion.SetResult(); }
            catch (Exception ex) { completion.SetException(ex); }
        });
        return completion.Task;
    }

    static async Task WaitFor(Func<bool> predicate, string message)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);
        while (!predicate())
        {
            if (DateTime.UtcNow >= deadline)
                Assert.Fail($"{message} Compositions={ConstraintsMeasurementTestActivity.Compositions}, " +
                    $"measurements={ConstraintsMeasurementTestActivity.Measurements}, " +
                    $"placedPhase={ConstraintsMeasurementTestActivity.PlacedPhase}.");
            await Task.Delay(20);
        }
    }
}
