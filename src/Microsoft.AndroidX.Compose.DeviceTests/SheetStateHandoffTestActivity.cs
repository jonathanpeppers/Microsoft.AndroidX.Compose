using Android.Runtime;
using AndroidX.Activity;
using AndroidX.Compose;
using AndroidX.Compose.Material3;
using AndroidX.Compose.Runtime;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Renders real modal and standard sheets with a shared holder across retired owners.</summary>
[Activity(Theme = "@android:style/Theme.Material.Light.NoActionBar")]
[Register("net/compose/devicetests/SheetStateHandoffTestActivity")]
public class SheetStateHandoffTestActivity : ComponentActivity
{
    static SheetStateHandoffTestActivity? s_current;
    int _completedPass = -1;

    internal static SheetStateHandoffTestActivity? Current => Volatile.Read(ref s_current);
    internal SheetStateHolder Sheet { get; private set; } = new();
    internal MutableState<bool> Standard { get; } = new(false);
    internal MutableState<bool> Visible { get; } = new(true);
    internal MutableState<bool> AllowTransitions { get; } = new(false);
    internal MutableState<int> Pass { get; } = new(0);
    internal int CompletedPass => Volatile.Read(ref _completedPass);

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        var window = Window ?? throw new InvalidOperationException("Sheet handoff activity has no window.");
        window.AddFlags(global::Android.Views.WindowManagerFlags.KeepScreenOn);
        var metrics = Resources?.DisplayMetrics
            ?? throw new InvalidOperationException("Sheet handoff display metrics were unavailable.");
        // Modal partial expansion requires content taller than half the available height.
        float sheetHeight = metrics.HeightPixels / metrics.Density * 0.75f;
        Sheet = new SheetStateHolder(Intent?.GetBooleanExtra("skipPartial", false) == true);
        Standard.Value = Intent?.GetBooleanExtra("standard", false) == true;
        bool direct = Intent?.GetBooleanExtra("direct", false) == true;
        if (Intent?.GetBooleanExtra("expanded", false) == true)
        {
            using var threshold = new ObjectFunction0(() => Java.Lang.Float.ValueOf(56f));
            using var velocity = new ObjectFunction0(() => Java.Lang.Float.ValueOf(125f));
            using var confirm = new SheetValueConfirmStateChange();
            using var seed = new SheetState(
                Standard.Value ? false : Sheet.SkipPartiallyExpanded,
                threshold, velocity,
                SheetValue.Expanded ?? throw new InvalidOperationException("SheetValue.Expanded was unavailable."),
                confirm, Standard.Value);
            Sheet.Jvm = seed;
            Sheet.UnbindJvm();
        }

        this.SetContent((IComposer composer) =>
        {
            int pass = Pass.Value;
            composer.StartReplaceableGroup(354801);
            if (Visible.Value)
            {
                bool standard = Standard.Value;
                bool allowTransitions = AllowTransitions.Value;
                Func<SheetValue, bool> confirm = _ => allowTransitions;
                if (direct)
                {
                    using var context = ComposableContext.Enter(composer);
                    if (standard)
                        Composables.BottomSheetScaffold(
                            sheetContent: () => Composables.Box(
                                content: () => Composables.Text("Standard sheet"),
                                modifier: Modifier.Height(sheetHeight)),
                            content: () => Composables.Text("Scaffold content"),
                            sheetState: Sheet,
                            confirmValueChange: confirm);
                    else
                        Composables.ModalBottomSheet(
                            onDismissRequest: static () => { },
                            content: () => Composables.Box(
                                content: () => Composables.Text("Modal sheet"),
                                modifier: Modifier.Height(sheetHeight)),
                            sheetState: Sheet,
                            confirmValueChange: confirm);
                }
                else if (standard)
                {
                    var scaffold = new BottomSheetScaffold(Sheet)
                    {
                        SheetContent = new Box
                        {
                            Modifier.Height(sheetHeight),
                            new Text("Standard sheet"),
                        },
                        ConfirmValueChange = confirm,
                    };
                    scaffold.Add(new Text("Scaffold content"));
                    scaffold.Render(composer);
                }
                else
                {
                    var modal = new ModalBottomSheet(static () => { }, Sheet)
                    {
                        ConfirmValueChange = confirm,
                    };
                    modal.Add(new Box { Modifier.Height(sheetHeight), new Text("Modal sheet") });
                    modal.Render(composer);
                }
            }
            composer.EndReplaceableGroup();
            composer.SideEffect(() => Volatile.Write(ref _completedPass, pass));
        });
        Volatile.Write(ref s_current, this);
    }

    protected override void OnDestroy()
    {
        if (ReferenceEquals(Current, this))
            Volatile.Write(ref s_current, null);
        base.OnDestroy();
    }
}
