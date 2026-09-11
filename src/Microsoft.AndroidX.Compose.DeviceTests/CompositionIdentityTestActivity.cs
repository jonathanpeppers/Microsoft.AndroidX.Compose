using System.Collections.Concurrent;
using Android.Runtime;
using AndroidX.Activity;
using AndroidX.Compose;
using AndroidX.Compose.Runtime;
using Composable = AndroidX.Compose.ComposableAttribute;
using Measurable = AndroidX.Compose.Measurable;
using MeasureScope = AndroidX.Compose.MeasureScope;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Exercises intercepted conditional calls against the real Compose slot table.</summary>
[Activity(Theme = "@android:style/Theme.Material.Light.NoActionBar")]
[Register("net/compose/devicetests/CompositionIdentityTestActivity")]
public class CompositionIdentityTestActivity : ComponentActivity
{
    static CompositionIdentityTestActivity? s_current;
    internal static CompositionIdentityTestActivity? Current => Volatile.Read(ref s_current);
    internal static ConcurrentDictionary<string, CompositionIdentityProbe> Probes { get; } = new();
    internal static MutableNumberState<int> Phase { get; private set; } = new(0);
    internal static MutableNumberState<int> Count { get; private set; } = new(3);
    internal static string Scenario { get; private set; } = "";
    internal static int ParentPasses;
    internal static bool CheckNodeOrder { get; private set; }
    internal static bool DirectContent { get; private set; }
    internal static int[] NodeOrder = [];
    internal static Action? Committed;
    internal static ConcurrentQueue<(long Bytes, long Ticks)> FootprintSamples { get; } = new();
    static Java.Lang.String? s_footprintKey;

    internal static void Reset(string scenario, bool checkNodeOrder = false, bool directContent = false)
    {
        Volatile.Write(ref s_current, null);
        Probes.Clear();
        Phase = new(0);
        Count = new(scenario.StartsWith("footprint-", StringComparison.Ordinal) ? 100 : 3);
        Scenario = scenario;
        ParentPasses = 0;
        CheckNodeOrder = checkNodeOrder;
        DirectContent = directContent;
        Volatile.Write(ref NodeOrder, []);
        Committed = null;
        FootprintSamples.Clear();
    }

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        var view = new global::AndroidX.Compose.UI.Platform.ComposeView(this) { Id = 350001 };
        if (DirectContent)
        {
            // Call through the bound API without a managed ambient-composer root frame.
            view.SetContent(global::AndroidX.Compose.Runtime.Internal.ComposableLambdaKt.ComposableLambdaInstance(
                350002, false, new ComposableLambda2(c => Screen(c))));
        }
        else
        {
            view.SetContent(c => Screen(c));
        }
        SetContentView(view);
        Volatile.Write(ref s_current, this);
    }

    protected override void OnDestroy()
    {
        if (ReferenceEquals(Current, this))
            Volatile.Write(ref s_current, null);
        base.OnDestroy();
    }

    /// <summary>Composes independent call sites, including repeated targets and loop occurrences.</summary>
    [Composable]
    public static void Screen(IComposer composer)
    {
        int phase = Phase.Value;
        int count = Count.Value;
        void Content(IComposer c)
        {
            switch (Scenario)
            {
                case "same":
                    if (phase != 1)
                        Counter(c, "optional");
                    Counter(c, "permanent");
                    break;
                case "different":
                    if (phase != 1)
                        AlternateCounter(c, "optional");
                    Counter(c, "permanent");
                    break;
                case "branches":
                    if (phase == 0)
                        Counter(c, "optional");
                    else
                        Counter(c, "alternative");
                    Counter(c, "permanent");
                    break;
                case "nested":
                    if (phase != 1)
                    {
                        if (phase != 2)
                            Counter(c, "optional");
                        Counter(c, "inner-permanent");
                    }
                    Counter(c, "permanent");
                    break;
                case "loop":
                    for (int i = 0; i < count; i++)
                    {
                        if (phase != 1)
                            Counter(c, $"optional-{i}");
                        Counter(c, $"loop-{i}");
                    }
                    Counter(c, "permanent");
                    break;
                case "selective":
                    for (int i = 0; i < 2; i++)
                        RepeatedParent(c, i, (phase & (1 << i)) != 0);
                    break;
                case "selective-nested":
                    for (int i = 0; i < 2; i++)
                        RepeatedOuter(c, i, phase);
                    break;
                case "footprint-baseline":
                case "footprint-ordinal":
                    RenderFootprint(c, count, phase, Scenario == "footprint-ordinal");
                    break;
                default:
                    throw new InvalidOperationException($"Unknown identity scenario '{Scenario}'.");
            }
            if (phase != 1)
                Counter(c, "trailing");
            c.SideEffect(() =>
            {
                Interlocked.Increment(ref ParentPasses);
                Committed?.Invoke();
            });
        }
        if (DirectContent)
            Content(composer);
        else if (CheckNodeOrder)
            Composables.Layout(composer, MeasureOrder, Content);
        else
            Composables.Column(composer, Content);
    }

    /// <summary>Owns remembered and saveable state at one target method.</summary>
    [Composable]
    public static void Counter(IComposer composer, string id) => Observe(composer, id);

    /// <summary>Owns the same state shape at a different target method.</summary>
    [Composable]
    public static void AlternateCounter(IComposer composer, string id) => Observe(composer, id);

    /// <summary>Provides an unchanged loop occurrence with a selectively inserted child.</summary>
    [Composable]
    public static void RepeatedParent(IComposer composer, int index, bool visible)
    {
        if (visible)
            Counter(composer, $"loop-{index}");
    }

    /// <summary>Repeats parents within independently invoked Kotlin content callbacks.</summary>
    [Composable]
    public static void RepeatedOuter(IComposer composer, int outer, int phase)
    {
        Composables.Column(composer, c =>
        {
            for (int i = 0; i < 2; i++)
            {
                int index = outer * 2 + i;
                RepeatedParent(c, index, (phase & (1 << index)) != 0);
            }
        });
    }

    static void Observe(IComposer composer, string id)
    {
        var probe = composer.Remember(() => new CompositionIdentityProbe());
        var saved = composer.RememberSaveable(() => new MutableNumberState<int>(0));
        int value = probe.Ordinary.Value;
        int savedValue = saved.Value;
        composer.DisposableEffect(0, () =>
        {
            Interlocked.Increment(ref probe.Setups);
            return () => Interlocked.Increment(ref probe.Disposals);
        });
        composer.SideEffect(() =>
        {
            probe.Saved = saved;
            Volatile.Write(ref probe.Observed, value);
            Volatile.Write(ref probe.ObservedSaved, savedValue);
            Interlocked.Increment(ref probe.Renders);
            Probes[id] = probe;
        });
        if (CheckNodeOrder)
        {
            Composables.Layout(composer,
                (scope, _, _) => scope.Layout(NodeCode(id), 20, _ => { }),
                _ => { });
        }
        else
        {
            Composables.Text(composer, $"{id}: {value}/{savedValue}");
        }
    }

    internal static int NodeCode(string id) => id switch
    {
        "permanent" => 100,
        "trailing" => 101,
        "optional" => 102,
        "alternative" => 103,
        "inner-permanent" => 104,
        _ when id.StartsWith("loop-", StringComparison.Ordinal) =>
            200 + int.Parse(id.AsSpan(5), System.Globalization.CultureInfo.InvariantCulture),
        _ when id.StartsWith("optional-", StringComparison.Ordinal) =>
            300 + int.Parse(id.AsSpan(9), System.Globalization.CultureInfo.InvariantCulture),
        _ => throw new InvalidOperationException($"Unknown identity node '{id}'."),
    };

    static void RenderFootprint(IComposer composer, int count, int phase, bool ordinal)
    {
        var key = s_footprintKey ??= new Java.Lang.String("identity-footprint-row");
        long bytes = GC.GetAllocatedBytesForCurrentThread();
        long ticks = System.Diagnostics.Stopwatch.GetTimestamp();
        Composables.Column(composer, c =>
        {
            for (int i = 0; i < count; i++)
            {
                if (ordinal)
                    ComposableCallSite.Start(c, 350100, key);
                else
                    c.StartMovableGroup(350100, key);
                var child = c.StartRestartGroup(350101);
                new Text($"Row {i}: {phase}").Render(child);
                child.EndRestartGroup();
                if (ordinal)
                    ComposableCallSite.End(c);
                else
                    c.EndMovableGroup();
            }
        });
        bytes = GC.GetAllocatedBytesForCurrentThread() - bytes;
        ticks = System.Diagnostics.Stopwatch.GetTimestamp() - ticks;
        composer.SideEffect(() => FootprintSamples.Enqueue((bytes, ticks)));
    }

    static MeasureResult MeasureOrder(MeasureScope scope, IReadOnlyList<Measurable> children,
        Constraints constraints)
    {
        var childConstraints = Constraints.Create(0, 400, 0, 2000);
        var measured = children.Select(child => child.Measure(childConstraints)).ToArray();
        return scope.Layout(400, measured.Sum(child => child.Height), placement =>
            {
                int y = 0;
                foreach (var child in measured)
                {
                    placement.Place(child, 0, y);
                    y += child.Height;
                }
                // Read the real applier's child order, not the order of C# callback execution.
                Volatile.Write(ref NodeOrder, measured.Select(child => child.Width).ToArray());
            });
    }
}
