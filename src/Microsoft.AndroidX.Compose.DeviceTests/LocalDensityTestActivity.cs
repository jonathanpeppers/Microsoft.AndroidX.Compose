using Android.Runtime;
using AndroidX.Activity;
using AndroidX.Compose;
using AndroidX.Compose.Runtime;
using DensityKt = AndroidX.Compose.UI.Unit.DensityKt;
using IDensity = AndroidX.Compose.UI.Unit.IDensity;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>
/// Reads and provides <see cref="LocalDensity"/> in a real composition, and
/// records the values native measurement receives.
/// </summary>
[Activity(Theme = "@android:style/Theme.Material.Light.NoActionBar")]
[Register("net/compose/devicetests/LocalDensityTestActivity")]
public class LocalDensityTestActivity : ComponentActivity
{
    internal const float ProvidedDensity = 2f;
    internal const float InitialFontScale = 1.5f;
    internal const float PeerDensity = 3f;
    internal const float PeerFontScale = 0.5f;
    internal const float LargeTextFontScale = 2f;

    static LocalDensityTestActivity? s_current;
    static LocalDensitySnapshot? s_inherited;
    static LocalDensitySnapshot? s_provided;
    static LocalDensitySnapshot? s_providedMeasure;
    static LocalDensitySnapshot? s_sibling;
    static LocalDensitySnapshot? s_peer;
    static LocalDensitySnapshot? s_peerMeasure;
    static int s_providedTenDpPx;
    static int s_peerIdentity;
    static int s_baselineTextHeight;
    static int s_largeTextHeight;

    internal static LocalDensityTestActivity? Current => Volatile.Read(ref s_current);
    internal static MutableNumberState<float>? ProvidedFontScale { get; private set; }
    internal static LocalDensitySnapshot? Inherited => Volatile.Read(ref s_inherited);
    internal static LocalDensitySnapshot? Provided => Volatile.Read(ref s_provided);
    internal static LocalDensitySnapshot? ProvidedMeasure => Volatile.Read(ref s_providedMeasure);
    internal static LocalDensitySnapshot? Sibling => Volatile.Read(ref s_sibling);
    internal static LocalDensitySnapshot? Peer => Volatile.Read(ref s_peer);
    internal static LocalDensitySnapshot? PeerMeasure => Volatile.Read(ref s_peerMeasure);
    internal static int ProvidedTenDpPx => Volatile.Read(ref s_providedTenDpPx);

    /// <summary>-1 before the read; 1 when the provided peer is the same Java object.</summary>
    internal static int PeerIdentity => Volatile.Read(ref s_peerIdentity);
    internal static int BaselineTextHeight => Volatile.Read(ref s_baselineTextHeight);
    internal static int LargeTextHeight => Volatile.Read(ref s_largeTextHeight);

    internal static void Reset()
    {
        Volatile.Write(ref s_current, null);
        ProvidedFontScale = null;
        Volatile.Write(ref s_inherited, null);
        Volatile.Write(ref s_provided, null);
        Volatile.Write(ref s_providedMeasure, null);
        Volatile.Write(ref s_sibling, null);
        Volatile.Write(ref s_peer, null);
        Volatile.Write(ref s_peerMeasure, null);
        Volatile.Write(ref s_providedTenDpPx, 0);
        Volatile.Write(ref s_peerIdentity, -1);
        Volatile.Write(ref s_baselineTextHeight, 0);
        Volatile.Write(ref s_largeTextHeight, 0);
    }

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        ProvidedFontScale = new MutableNumberState<float>(InitialFontScale);
        this.SetContent(_ => new Composed(BuildContent));
        Volatile.Write(ref s_current, this);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        ProvidedFontScale = null;
        if (ReferenceEquals(Current, this))
            Volatile.Write(ref s_current, null);
    }

    static ComposableNode BuildContent(IComposer composer)
    {
        var inherited = LocalDensity.Current(composer);
        float inheritedDensity = inherited.Density;
        Volatile.Write(ref s_inherited, Snapshot(inherited));
        var fontScale = ProvidedFontScale
            ?? throw new InvalidOperationException("Provided font scale was not initialized.");
        var peer = DensityKt.Density(PeerDensity, PeerFontScale);

        return new Column
        {
            new CompositionLocalProvider
            {
                LocalDensity.Provides(ProvidedDensity, fontScale.Value),
                new Composed(inner =>
                {
                    Volatile.Write(ref s_provided, Snapshot(LocalDensity.Current(inner)));
                    return null;
                }),
                new Layout((scope, _, _) =>
                {
                    Volatile.Write(ref s_providedMeasure, new LocalDensitySnapshot(scope.Density, scope.FontScale));
                    Volatile.Write(ref s_providedTenDpPx, scope.RoundToPx(10.Dp()));
                    return scope.Layout(0, 0, _ => { });
                }),
            },
            new Composed(sibling =>
            {
                Volatile.Write(ref s_sibling, Snapshot(LocalDensity.Current(sibling)));
                return null;
            }),
            new CompositionLocalProvider
            {
                LocalDensity.Provides(peer),
                new Composed(inner =>
                {
                    var read = LocalDensity.Current(inner);
                    bool same = JNIEnv.IsSameObject(
                        ((Java.Lang.Object)read).Handle,
                        ((Java.Lang.Object)peer).Handle);
                    Volatile.Write(ref s_peerIdentity, same ? 1 : 0);
                    Volatile.Write(ref s_peer, Snapshot(read));
                    return null;
                }),
                new Layout((scope, _, _) =>
                {
                    Volatile.Write(ref s_peerMeasure, new LocalDensitySnapshot(scope.Density, scope.FontScale));
                    return scope.Layout(0, 0, _ => { });
                }),
            },
            TextProbe(inheritedDensity, 1f, height => Volatile.Write(ref s_baselineTextHeight, height)),
            TextProbe(inheritedDensity, LargeTextFontScale, height => Volatile.Write(ref s_largeTextHeight, height)),
        };
    }

    // Measures the same 14sp text under a provided font scale and records its pixel height.
    static CompositionLocalProvider TextProbe(float density, float fontScale, Action<int> record) => new()
    {
        LocalDensity.Provides(density, fontScale),
        new Layout((scope, children, constraints) =>
        {
            var text = children[0].Measure(constraints);
            record(text.Height);
            return scope.Layout(text.Width, text.Height, placement => placement.Place(text, 0, 0));
        })
        {
            new Text("Ag") { FontSize = 14 },
        },
    };

    static LocalDensitySnapshot Snapshot(IDensity density) => new(density.Density, density.FontScale);
}
