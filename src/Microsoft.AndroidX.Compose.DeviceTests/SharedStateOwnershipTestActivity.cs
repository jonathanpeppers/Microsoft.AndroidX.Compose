using Android.Runtime;
using AndroidX.Activity;
using AndroidX.Compose;
using AndroidX.Compose.Runtime;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Exercises native save registration after repeated shared-state renders.</summary>
[Activity(Theme = "@android:style/Theme.Material.Light.NoActionBar")]
[Register("net/compose/devicetests/SharedStateOwnershipTestActivity")]
public class SharedStateOwnershipTestActivity : ComponentActivity
{
    internal static SharedStateOwnershipTestActivity? Current { get; private set; }
    internal TimePickerState State { get; private set; } = new(initialHour: 7, initialMinute: 15);
    internal MutableState<int> Pass { get; } = new(0);
    internal MutableState<bool> ShowFirst { get; } = new(true);
    internal MutableState<bool> ShowSecond { get; } = new(true);
    internal int CompletedPass { get; private set; } = -1;
    internal bool SiblingsSharePeer { get; private set; }

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        this.SetContent((IComposer composer) =>
        {
            int pass = Pass.Value;
            string mode = Intent?.GetStringExtra("mode") ?? "tree";
            if (mode.StartsWith("owned", StringComparison.Ordinal))
            {
                var supplied = mode.Contains("omitted", StringComparison.Ordinal) ? null : State;
                if (mode.EndsWith("direct", StringComparison.Ordinal))
                {
                    using var context = ComposableContext.Enter(composer);
                    State = Composables.RememberTimePickerState(supplied);
                }
                else
                    State = composer.RememberTimePickerState(supplied);
            }
            composer.StartReplaceableGroup(354001);
            if (mode == "native")
            {
                var handle = ComposeBridges.RememberTimePickerStateJvm(7, 15, true, composer);
                try
                {
                    State.BindJvm(Java.Lang.Object.GetObject<global::AndroidX.Compose.Material3.ITimePickerState>(
                        handle, JniHandleOwnership.DoNotTransfer)
                        ?? throw new InvalidOperationException("Native time state was not returned."));
                }
                finally
                {
                    JNIEnv.DeleteLocalRef(handle);
                }
            }
            else if (ShowFirst.Value)
            {
                RenderPicker(composer, mode);
            }
            var owner = State.Jvm;
            composer.EndReplaceableGroup();

            composer.StartReplaceableGroup(354002);
            if (mode != "native" && ShowSecond.Value)
                RenderPicker(composer, mode);
            SiblingsSharePeer = !ShowFirst.Value || !ShowSecond.Value || ReferenceEquals(owner, State.Jvm);
            composer.EndReplaceableGroup();
            composer.SideEffect(() => CompletedPass = pass);
        });
        Current = this;
    }

    void RenderPicker(IComposer composer, string mode)
    {
        if (mode.EndsWith("direct", StringComparison.Ordinal))
        {
            using var context = ComposableContext.Enter(composer);
            Composables.TimeInput(state: State);
        }
        else
        {
            new TimeInput(State).Render(composer);
        }
    }

    protected override void OnDestroy()
    {
        if (ReferenceEquals(Current, this))
            Current = null;
        base.OnDestroy();
    }
}
