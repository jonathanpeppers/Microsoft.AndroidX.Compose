using Android.Runtime;
using AndroidX.Activity;
using AndroidX.Compose;
using AndroidX.Compose.Runtime;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Hosts a saveable registry with stable view identity for activity recreation tests.</summary>
[Activity(Theme = "@android:style/Theme.Material.Light.NoActionBar")]
[Register("net/compose/devicetests/RememberSaveableTestActivity")]
public class RememberSaveableTestActivity : ComponentActivity
{
    static RememberSaveableTestActivity? s_current;
    internal static RememberSaveableTestActivity? Current => Volatile.Read(ref s_current);
    internal static Action<IComposer>? Content;
    internal static MutableNumberState<int> Revision { get; private set; } = new(0);
    internal static int Passes;
    internal bool Restored { get; private set; }

    internal static void Reset(Action<IComposer> content)
    {
        Volatile.Write(ref s_current, null);
        Content = content;
        Revision = new(0);
        Passes = 0;
    }

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        Restored = savedInstanceState is not null;
        var view = new global::AndroidX.Compose.UI.Platform.ComposeView(this) { Id = 355001 };
        view.SetContent(c =>
        {
            _ = Revision.Value;
            var content = Content ?? throw new InvalidOperationException("Saveable test content was not configured.");
            content(c);
            c.SideEffect(() => Interlocked.Increment(ref Passes));
        });
        SetContentView(view);
        Volatile.Write(ref s_current, this);
    }

    protected override void OnDestroy()
    {
        if (ReferenceEquals(Current, this))
            Volatile.Write(ref s_current, null);
        base.OnDestroy();
    }
}
