using Android.Runtime;
using AndroidX.Activity;
using AndroidX.Compose;
using AndroidX.Compose.Material3;
using AndroidX.Compose.Runtime;
using NavigationValue = AndroidX.Compose.Material3.Adaptive.NavigationSuite.NavigationSuiteScaffoldValue;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Hosts settled drawer, sheet, and navigation state without layout-dependent animations.</summary>
[Activity(Theme = "@android:style/Theme.Material.Light.NoActionBar")]
[Register("net/compose/devicetests/SharedStateTransferTestActivity")]
public class SharedStateTransferTestActivity : ComponentActivity
{
    internal static SharedStateTransferTestActivity? Current { get; private set; }
    internal SheetStateHolder Sheet { get; } = new();
    internal DrawerStateHolder Drawer { get; } = new();
    internal global::AndroidX.Compose.NavigationSuiteScaffoldState Navigation { get; } = new();
    internal MutableState<bool> Visible { get; } = new(true);
    internal MutableState<int> Pass { get; } = new(0);
    internal int CompletedPass { get; private set; } = -1;
    internal int LastConfirmPass { get; private set; } = -1;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        using var threshold = new ObjectFunction0(() => Java.Lang.Float.ValueOf(56f));
        using var velocity = new ObjectFunction0(() => Java.Lang.Float.ValueOf(125f));
        using var confirmSheet = new SheetValueConfirmStateChange();
        using var seedSheet = new SheetState(false, threshold, velocity,
            SheetValue.Expanded ?? throw new InvalidOperationException("SheetValue.Expanded unavailable."),
            confirmSheet, false);
        Sheet.Jvm = seedSheet;
        Sheet.UnbindJvm();
        using var confirmDrawer = new DrawerValueConfirmStateChange();
        using var seedDrawer = new DrawerState(
            DrawerValue.Open ?? throw new InvalidOperationException("DrawerValue.Open unavailable."), confirmDrawer);
        Drawer.Jvm = seedDrawer;
        Drawer.UnbindJvm();

        this.SetContent((IComposer composer) =>
        {
            int pass = Pass.Value;
            composer.StartReplaceableGroup(354201);
            if (Visible.Value)
            {
                composer.RememberSheetState(Sheet, _ =>
                {
                    LastConfirmPass = pass;
                    return false;
                });
                composer.RememberDrawerState(Drawer);
                composer.RememberNavigationSuiteScaffoldState(Navigation);
            }
            composer.EndReplaceableGroup();
            composer.SideEffect(() => CompletedPass = pass);
        });
        Current = this;
    }

    internal Task HideNavigation() => Navigation.SnapToAsync(
        NavigationValue.Hidden ?? throw new InvalidOperationException("NavigationSuiteScaffoldValue.Hidden unavailable."));

    protected override void OnDestroy()
    {
        if (ReferenceEquals(Current, this))
            Current = null;
        base.OnDestroy();
    }
}
