using Android.Runtime;
using AndroidX.Activity;
using AndroidX.Compose;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Hosts a composition that reads managed state for device tests.</summary>
[Activity(Theme = "@android:style/Theme.Material.Light.NoActionBar")]
[Register("net/compose/devicetests/MutableManagedStateTestActivity")]
public class MutableManagedStateTestActivity : ComponentActivity
{
    static MutableManagedStateTestActivity? s_current;
    static int s_observedValue;
    static int s_renderCount;

    internal static MutableManagedStateTestActivity? Current => Volatile.Read(ref s_current);

    internal static MutableManagedState<int> State { get; private set; } = new(0);

    internal static int ObservedValue => Volatile.Read(ref s_observedValue);

    internal static int RenderCount => Volatile.Read(ref s_renderCount);

    internal static void Reset()
    {
        Volatile.Write(ref s_current, null);
        Volatile.Write(ref s_observedValue, 0);
        Volatile.Write(ref s_renderCount, 0);
        State = new MutableManagedState<int>(0);
    }

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        var state = State;
        this.SetContent(_ =>
        {
            int value = state.Value;
            Volatile.Write(ref s_observedValue, value);
            Interlocked.Increment(ref s_renderCount);
            return new Text($"Value: {value}");
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
