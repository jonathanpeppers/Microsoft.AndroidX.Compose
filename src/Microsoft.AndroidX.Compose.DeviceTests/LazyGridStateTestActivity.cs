using Android.Runtime;
using AndroidX.Activity;
using AndroidX.Compose;
using AndroidX.Compose.Runtime;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Hosts real lazy grids for managed-state and default-mask device tests.</summary>
[Activity(Theme = "@android:style/Theme.Material.Light.NoActionBar")]
[Register("net/compose/devicetests/LazyGridStateTestActivity")]
public class LazyGridStateTestActivity : ComponentActivity
{
    internal const int TreeGridExplicitState = 1;
    internal const int StaticStaggeredExplicitState = 2;
    internal const int StaticDefaults = 3;
    internal const int RememberIdentity = 4;
    internal const int InitialIndex = 40;

    static readonly IReadOnlyList<int> Items = Enumerable.Range(0, 100).ToList();
    static int s_scenario;
    static int s_renderedItems;

    internal static LazyGridState GridState { get; private set; } = new(InitialIndex);

    internal static LazyStaggeredGridState StaggeredState { get; private set; } =
        new(InitialIndex);

    internal static LazyGridStateTestActivity? Current { get; private set; }

    internal static int RenderedItems => Volatile.Read(ref s_renderedItems);

    internal static MutableNumberState<int>? RecompositionTrigger { get; private set; }

    internal static LazyGridState? FirstRememberedGridState { get; private set; }

    internal static LazyGridState? LastRememberedGridState { get; private set; }

    internal static LazyStaggeredGridState? FirstRememberedStaggeredState { get; private set; }

    internal static LazyStaggeredGridState? LastRememberedStaggeredState { get; private set; }

    internal static int RememberCompositionCount { get; private set; }

    internal static void Reset(int scenario)
    {
        Current = null;
        GridState = new LazyGridState(InitialIndex);
        StaggeredState = new LazyStaggeredGridState(InitialIndex);
        RecompositionTrigger = null;
        FirstRememberedGridState = null;
        LastRememberedGridState = null;
        FirstRememberedStaggeredState = null;
        LastRememberedStaggeredState = null;
        RememberCompositionCount = 0;
        Volatile.Write(ref s_renderedItems, 0);
        Volatile.Write(ref s_scenario, scenario);
    }

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        switch (Volatile.Read(ref s_scenario))
        {
            case TreeGridExplicitState:
                var gridState = GridState;
                this.SetContent(_ => new LazyVerticalGrid<int>(
                    GridCells.Adaptive(80.Dp()),
                    Items,
                    RenderItem)
                {
                    Modifier = Modifier.FillMaxSize(),
                    State = gridState,
                });
                break;
            case StaticStaggeredExplicitState:
                this.SetContent(RenderStaticStaggered);
                break;
            case StaticDefaults:
                this.SetContent(RenderStaticDefaults);
                break;
            case RememberIdentity:
                this.SetContent(BuildRememberIdentity);
                break;
            default:
                throw new InvalidOperationException("Lazy-grid test scenario was not configured.");
        }

        Current = this;
    }

    protected override void OnDestroy()
    {
        if (ReferenceEquals(Current, this))
            Current = null;
        base.OnDestroy();
    }

    static void RenderStaticStaggered(IComposer composer) =>
        Composables.LazyVerticalStaggeredGrid(
            composer,
            StaggeredGridCells.FixedSize(80.Dp()),
            Items,
            static (item, itemComposer) => RenderItem(item).Render(itemComposer),
            modifier: Modifier.FillMaxSize(),
            state: StaggeredState);

    static void RenderStaticDefaults(IComposer composer) =>
        Composables.LazyVerticalGrid(
            composer,
            GridCells.Fixed(2),
            Items,
            static (item, itemComposer) => RenderItem(item).Render(itemComposer),
            modifier: Modifier.FillMaxSize());

    static ComposableNode BuildRememberIdentity(IComposer composer)
    {
        var trigger = composer.MutableStateOf(0);
        var gridState = composer.RememberLazyGridState();
        var staggeredState = composer.RememberLazyStaggeredGridState();
        RecompositionTrigger = trigger;
        FirstRememberedGridState ??= gridState;
        LastRememberedGridState = gridState;
        FirstRememberedStaggeredState ??= staggeredState;
        LastRememberedStaggeredState = staggeredState;
        RememberCompositionCount++;
        return new Text($"Composition {trigger.Value}");
    }

    static Text RenderItem(int item)
    {
        Interlocked.Increment(ref s_renderedItems);
        return new Text($"Item {item}")
        {
            Modifier = Modifier.Height(400),
        };
    }
}
