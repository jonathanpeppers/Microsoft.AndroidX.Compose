using Android.Runtime;
using Android.Views;
using AndroidX.Activity;
using AndroidX.Compose;
using AndroidX.Compose.Runtime;
using AndroidX.Compose.Samples.Reply;
using Modifier = AndroidX.Compose.Modifier;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Hosts the real Reply search and destination-disposal boundary.</summary>
[Activity(Theme = "@android:style/Theme.Material.Light.NoActionBar",
    WindowSoftInputMode = SoftInput.AdjustResize)]
[Register("net/compose/devicetests/ReplySearchTestActivity")]
public class ReplySearchTestActivity : ComponentActivity
{
    internal static ReplySearchTestActivity? Current;
    internal readonly TaskCompletionSource Destroyed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal MutableState<bool> InInbox { get; } = new(true);
    internal MutableState<int> Tick { get; } = new(0);
    internal long? SelectedId;
    internal int SelectionCalls;
    internal int Passes;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        Window?.AddFlags(WindowManagerFlags.KeepScreenOn);
        this.SetContent(c => new Composed(Build));
        Volatile.Write(ref Current, this);
    }

    ComposableNode Build(IComposer composer)
    {
        var tick = Tick.Value;
        return new MaterialTheme
        {
            new Column
            {
                Modifier.FillMaxSize().SafeDrawingPadding(),
                InInbox.Value
                    ? new ReplySearchBar(LocalEmailsDataProvider.AllEmails, id =>
                    {
                        SelectedId = id;
                        SelectionCalls++;
                        InInbox.Value = false;
                    })
                    : new Text($"Selected email {SelectedId}"),
                new Text($"Unrelated tick {tick}"),
                new SideEffect(() => Passes++),
            },
        };
    }

    protected override void OnDestroy()
    {
        Window?.ClearFlags(WindowManagerFlags.KeepScreenOn);
        base.OnDestroy();
        if (ReferenceEquals(Current, this))
            Volatile.Write(ref Current, null);
        Destroyed.TrySetResult();
    }
}
