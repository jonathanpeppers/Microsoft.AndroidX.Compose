using Android.Runtime;
using AndroidX.Activity;
using AndroidX.Compose;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>
/// Hosts a real Compose <see cref="LazyColumn{T}"/> for the suspend
/// cancellation device test.
/// </summary>
[Activity(Theme = "@android:style/Theme.Material.Light.NoActionBar")]
[Register("net/compose/devicetests/LazyListCancellationTestActivity")]
public class LazyListCancellationTestActivity : ComponentActivity
{
    internal static LazyListCancellationTestActivity? Current { get; private set; }

    internal static LazyListState State { get; private set; } = new();

    internal static CoroutineScope? Scope { get; private set; }

    internal static void Reset()
    {
        Current = null;
        State = new LazyListState();
        Scope = null;
    }

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        var state = State;
        this.SetContent(c =>
        {
            Scope = c.RememberCoroutineScope();
            return new LazyColumn<int>(
                Enumerable.Range(0, 10_000).ToList(),
                static value => new Text($"Row {value:D5}"))
            {
                Modifier = Modifier.FillMaxSize(),
                State = state,
            };
        });
        Current = this;
    }

    protected override void OnDestroy()
    {
        if (ReferenceEquals(Current, this))
            Current = null;
        Scope = null;
        base.OnDestroy();
    }
}
