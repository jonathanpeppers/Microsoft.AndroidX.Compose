using Android.Runtime;
using AndroidX.Activity;
using AndroidX.Compose;
using AndroidX.Compose.Runtime;
using Composable = AndroidX.Compose.ComposableAttribute;
using Constraints = AndroidX.Compose.Constraints;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Drives Constraints reads from native measure callbacks, not from test-thread substitutes.</summary>
[Activity(Theme = "@android:style/Theme.Material.Light.NoActionBar")]
[Register("net/compose/devicetests/ConstraintsMeasurementTestActivity")]
public class ConstraintsMeasurementTestActivity : ComponentActivity
{
    static ConstraintsMeasurementTestActivity? s_current;
    internal static ConstraintsMeasurementTestActivity? Current => Volatile.Read(ref s_current);
    internal static MutableNumberState<int> Phase { get; private set; } = new(0);
    internal static int Compositions;
    internal static int Measurements;
    internal static int PlacedPhase = -1;
    internal static bool ReadAccessors;
    internal static int FirstAccessor;
    internal static string RunId = "";

    internal static void Reset(bool readAccessors, int firstAccessor)
    {
        Volatile.Write(ref s_current, null);
        Phase = new(0);
        Compositions = 0;
        Measurements = 0;
        PlacedPhase = -1;
        ReadAccessors = readAccessors;
        FirstAccessor = firstAccessor;
        RunId = Guid.NewGuid().ToString("N");
    }

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        var view = new global::AndroidX.Compose.UI.Platform.ComposeView(this);
        view.SetContent(c => Screen(c));
        SetContentView(view);
        Volatile.Write(ref s_current, this);
    }

    protected override void OnDestroy()
    {
        if (ReferenceEquals(Current, this))
            Volatile.Write(ref s_current, null);
        base.OnDestroy();
    }

    /// <summary>Changes the native child's constraint envelope on every recomposition.</summary>
    [Composable]
    public static void Screen(IComposer composer)
    {
        int phase = Phase.Value;
        var bounds = Bounds(phase);
        var envelope = Constraints.Create(bounds.MinWidth, bounds.MaxWidth, bounds.MinHeight, bounds.MaxHeight);
        // Changing padding invalidates native measurement; replacing the cached policy's delegate does not.
        Composables.Layout(composer, (scope, children, _) =>
        {
            Assert.HasCount(1, children);
            var child = children[0].Measure(envelope);
            return scope.Layout(400, 400, placement => placement.Place(child, 0, 0));
        }, c =>
        {
            Composables.Layout(c, (scope, children, constraints) =>
            {
                Assert.AreEqual(envelope, constraints, "Native measurement must receive this phase's packed bounds.");
                Assert.HasCount(1, children);
                if (ReadAccessors)
                    ConstraintsMeasurementTests.CheckAccessors(constraints, bounds, collect: true);
                else
                    ConstraintsMeasurementTests.Collect();
                int width = ReadAccessors ? constraints.ConstrainWidth(80) : Math.Clamp(80, bounds.MinWidth, bounds.MaxWidth);
                int height = ReadAccessors ? constraints.ConstrainHeight(90) : Math.Clamp(90, bounds.MinHeight, bounds.MaxHeight);
                var child = children[0].Measure(Constraints.Create(width, width, height, height));
                Assert.AreEqual(width, child.Width);
                Assert.AreEqual(height, child.Height);
                Interlocked.Increment(ref Measurements);
                return scope.Layout(width, height, placement =>
                {
                    placement.Place(child, 0, 0);
                    Volatile.Write(ref PlacedPhase, phase);
                });
            }, leaf => Composables.Box(leaf, _ => { }, modifier: Modifier.Size(1)));
        }, modifier: Modifier.Padding(phase));
        composer.SideEffect(() => Interlocked.Increment(ref Compositions));
    }

    internal static (int MinWidth, int MaxWidth, int MinHeight, int MaxHeight) Bounds(int phase) =>
        (phase % 4) switch
        {
            0 => (10 + phase, 200 + phase, 20 + phase, 220 + phase),
            1 => (40 + phase, 40 + phase, 50 + phase, 50 + phase),
            2 => (10 + phase, int.MaxValue, 20 + phase, 220 + phase),
            _ => (10 + phase, 200 + phase, 20 + phase, int.MaxValue),
        };
}
