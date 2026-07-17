using Android.Runtime;
using AndroidX.Activity;
using AndroidX.Compose;
using AndroidX.Compose.Runtime;
using AndroidX.Lifecycle;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>
/// Hosts disposable effects and lifecycle composition locals in a real
/// composition.
/// </summary>
[Activity(Theme = "@android:style/Theme.Material.Light.NoActionBar")]
[Register("net/compose/devicetests/EffectsAndLifecycleTestActivity")]
public class EffectsAndLifecycleTestActivity : ComponentActivity
{
    static int s_completedRenderPasses;
    static int s_setups;
    static int s_cleanups;
    static EffectsAndLifecycleTestActivity? s_current;

    internal static EffectsAndLifecycleTestActivity? Current =>
        Volatile.Read(ref s_current);
    internal static MutableState<int>? EffectKey { get; private set; }
    internal static MutableState<int>? RecompositionTick { get; private set; }
    internal static MutableState<bool>? EffectVisible { get; private set; }
    internal static ILifecycleOwner? DefaultOwner { get; private set; }
    internal static ILifecycleOwner? ProvidedOwner { get; private set; }
    internal static ILifecycleOwner? ExpectedProvidedOwner { get; private set; }
    internal static int CompletedRenderPasses => Volatile.Read(ref s_completedRenderPasses);
    internal static int Setups => Volatile.Read(ref s_setups);
    internal static int Cleanups => Volatile.Read(ref s_cleanups);

    internal static void Reset()
    {
        Volatile.Write(ref s_current, null);
        EffectKey = null;
        RecompositionTick = null;
        EffectVisible = null;
        DefaultOwner = null;
        ProvidedOwner = null;
        ExpectedProvidedOwner = ProcessLifecycleOwner.Get();
        Volatile.Write(ref s_completedRenderPasses, 0);
        Volatile.Write(ref s_setups, 0);
        Volatile.Write(ref s_cleanups, 0);
    }

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        EffectKey = new MutableState<int>(0);
        RecompositionTick = new MutableState<int>(0);
        EffectVisible = new MutableState<bool>(true);
        this.SetContent(_ => new Composed(BuildContent));
        Volatile.Write(ref s_current, this);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        EffectKey = null;
        RecompositionTick = null;
        EffectVisible = null;
        if (ReferenceEquals(Current, this))
            Volatile.Write(ref s_current, null);
    }

    static ComposableNode BuildContent(IComposer composer)
    {
        DefaultOwner = LocalLifecycleOwner.Current(composer);
        var expectedOwner = ExpectedProvidedOwner
            ?? throw new InvalidOperationException(
                "Expected lifecycle owner was not initialized.");
        var visible = EffectVisible
            ?? throw new InvalidOperationException(
                "Effect visibility was not initialized.");
        var key = EffectKey
            ?? throw new InvalidOperationException(
                "Effect key was not initialized.");
        var tick = RecompositionTick
            ?? throw new InvalidOperationException(
                "Recomposition tick was not initialized.");

        return new Column
        {
            visible.Value
                ? new DisposableEffect(key.Value, () =>
                {
                    Interlocked.Increment(ref s_setups);
                    return () => Interlocked.Increment(ref s_cleanups);
                })
                : null,
            new Text($"Recomposition tick: {tick.Value}"),
            new CompositionLocalProvider
            {
                LocalLifecycleOwner.Provides(expectedOwner),
                new Composed(innerComposer =>
                {
                    ProvidedOwner = LocalLifecycleOwner.Current(innerComposer);
                    return null;
                }),
            },
            new SideEffect(() =>
                Interlocked.Increment(ref s_completedRenderPasses)),
        };
    }
}
