using Android.Runtime;
using AndroidX.Activity;
using AndroidX.Compose;
using AndroidX.Compose.Runtime;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Rebuilds navigation content with new render-local values and callbacks.</summary>
[Activity(Theme = "@android:style/Theme.Material.Light.NoActionBar")]
[Register("net/compose/devicetests/NavContentTestActivity")]
public class NavContentTestActivity : ComponentActivity
{
    static NavContentTestActivity? s_current;
    static NavContentObservation? s_home, s_detail;
    static string? s_clicked;
    static int s_parentPasses;
    static int s_unmounts;
    static int s_destinationMounts, s_destinationUnmounts;
    static bool s_staticChildren;

    internal static NavContentTestActivity? Current => Volatile.Read(ref s_current);
    internal static NavContentObservation? Home => Volatile.Read(ref s_home);
    internal static NavContentObservation? Detail => Volatile.Read(ref s_detail);
    internal static string? Clicked => Volatile.Read(ref s_clicked);
    internal static int ParentPasses => Volatile.Read(ref s_parentPasses);
    internal static int Unmounts => Volatile.Read(ref s_unmounts);
    internal static int DestinationMounts => Volatile.Read(ref s_destinationMounts);
    internal static int DestinationUnmounts => Volatile.Read(ref s_destinationUnmounts);
    internal static MutableNumberState<int> Generation { get; private set; } = new(0);
    internal static MutableState<bool> Visible { get; private set; } = new(true);
    internal static MutableState<string> StartDestination { get; private set; } = new("home");
    internal static NavController Controller { get; private set; } = new();
    internal static WeakReference? LatestHomeContent { get; private set; }
    internal static bool SwitchContentShape { get; set; }
    internal static bool IncludeExtraRoute { get; set; }
    internal static bool ReorderRoutes { get; set; }
    internal static bool OmitDetail { get; set; }
    internal static bool DuplicateHome { get; set; }
    internal static bool RawDuplicateGraph { get; set; }

    internal static void Reset(bool staticChildren)
    {
        Volatile.Write(ref s_current, null);
        Volatile.Write(ref s_home, null);
        Volatile.Write(ref s_detail, null);
        Volatile.Write(ref s_clicked, null);
        Volatile.Write(ref s_parentPasses, 0);
        Volatile.Write(ref s_unmounts, 0);
        Volatile.Write(ref s_destinationMounts, 0);
        Volatile.Write(ref s_destinationUnmounts, 0);
        s_staticChildren = staticChildren;
        Generation = new(0);
        Visible = new(true);
        StartDestination = new("home");
        Controller = new();
        LatestHomeContent = null;
        SwitchContentShape = false;
        IncludeExtraRoute = false;
        ReorderRoutes = false;
        OmitDetail = false;
        DuplicateHome = false;
        RawDuplicateGraph = false;
    }

    internal static void Observe(string route, NavContentObservation observation)
    {
        if (route == "home")
            Volatile.Write(ref s_home, observation);
        else
            Volatile.Write(ref s_detail, observation);
    }

    internal static void MarkUnmounted() => Interlocked.Increment(ref s_unmounts);

    internal static Action MarkDestinationMounted()
    {
        Interlocked.Increment(ref s_destinationMounts);
        return static () => Interlocked.Increment(ref s_destinationUnmounts);
    }

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        this.SetContent((IComposer composer) =>
        {
            if (RawDuplicateGraph)
                return composer.Remember(() => new NavDuplicateRegistrationProbe());
            int generation = Generation.Value;
            bool visible = Visible.Value;
            composer.SideEffect(() => Interlocked.Increment(ref s_parentPasses));
            if (!visible)
                return new Box { new Text("Host removed") };

            var home = CreateDestination("home", generation);
            LatestHomeContent = new WeakReference(home.Content);
            var host = new NavHost(StartDestination.Value, Controller);
            if (ReorderRoutes && !OmitDetail)
                host.Add(CreateDestination("detail", generation));
            host.Add(home);
            if (!ReorderRoutes && !OmitDetail)
                host.Add(CreateDestination("detail", generation));
            if (IncludeExtraRoute)
                host.Add(CreateDestination("extra", generation));
            if (DuplicateHome)
                host.Add(CreateDestination("home", generation + 10));
            return new Box { new NavHostLifetimeProbe(host) };
        });
        Volatile.Write(ref s_current, this);
    }

    static NavDestination CreateDestination(string route, int generation)
    {
        // These captures are replaced, not reads of enduring observable state.
        string label = $"{route}: Account {generation}";
        Action callback = () => Volatile.Write(ref s_clicked, label);
        bool staticChildren = SwitchContentShape ? generation % 2 == 1 : s_staticChildren;
        return staticChildren
            ? new NavDestination(route) { new NavContentProbe(route, label, callback) }
            : new NavDestination(route, _ => new NavContentProbe(route, label, callback));
    }

    protected override void OnDestroy()
    {
        if (ReferenceEquals(Current, this))
            Volatile.Write(ref s_current, null);
        base.OnDestroy();
    }
}
