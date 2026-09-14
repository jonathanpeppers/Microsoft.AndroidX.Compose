using Android.Runtime;
using AndroidX.Compose;
using AndroidX.Compose.Foundation.Layout;
using AndroidX.Compose.Runtime;
using AndroidX.Compose.UI.Layout;
using Edges = (float Left, float Top, float Right, float Bottom);
using LayoutDirection = AndroidX.Compose.UI.Unit.LayoutDirection;
using PaddingValues = AndroidX.Compose.PaddingValues;
using DensityKt = AndroidX.Compose.UI.Unit.DensityKt;
using WindowInsets = AndroidX.Compose.WindowInsets;

namespace Microsoft.AndroidX.Compose.DeviceTests;

// A frame owns UI-thread measurements; only its immutable snapshot crosses threads.
internal sealed class ScaffoldInsetsFrame
{
    internal const int Root = 0;
    internal const int Body = 1;
    internal const int TopBar = 2;
    internal const int BottomBar = 3;
    internal const int LayoutAck = 4;

    static int s_nextRenderId;
    readonly int _renderId = Interlocked.Increment(ref s_nextRenderId);
    readonly ScaffoldInsetsTestActivity _owner;
    readonly int _generation;
    readonly int _mode;
    readonly bool _bars;
    readonly float _density;
    readonly IPaddingValues _boundDefault;
    readonly PaddingValues _explicitReader;
    readonly PaddingValues _implicitReader;
    readonly PaddingValues _navigationBars;
    readonly PaddingValues _ime;
    readonly Edges?[] _bounds = new Edges?[5];
    readonly ILayoutCoordinates?[] _coordinates = new ILayoutCoordinates?[5];
    PaddingValues? _paddingPeer;
    WindowInsets? _nativeInsets;
    Edges? _nativeAtCall;
    (object Sentinel, MutableNumberState<int> Counter, int Value)? _bodyState;
    IntPtr _contentLambda;
    int _contentVersion;
    int _bodyVersion = -1;
    int _markerVersion = -1;
    int _sequence;
    int _nativeSequence;
    int _contentSequence;
    int _bodySequence;
    int _markerSequence;
    int _bodyCompositions;
    int _bodyObservationVersion;
    int _pendingDraws;
    bool? _pendingAtPlacement;
    IntPtr _nativeHandle;
    string _admission = "";

    internal Dp LayoutToken => new(_generation + 1);

    internal ScaffoldInsetsFrame(
        ScaffoldInsetsTestActivity owner,
        IComposer composer,
        int generation,
        int mode,
        bool bars,
        PaddingValues implicitReader)
    {
        _owner = owner;
        _generation = generation;
        _mode = mode;
        _bars = bars;
        _density = owner.Resources?.DisplayMetrics?.Density
            ?? throw new InvalidOperationException("Display density not set on ScaffoldInsetsTestActivity.");
        _boundDefault = WindowInsetsKt.AsPaddingValues(
            global::AndroidX.Compose.Material3.ScaffoldDefaults.Instance
                .GetContentWindowInsets(composer, 0),
            composer,
            0);
        _explicitReader = composer.ScaffoldContentWindowInsets().AsPaddingValues(composer);
        _implicitReader = implicitReader;
        _navigationBars = composer.NavigationBarsInsets().AsPaddingValues(composer);
        _ime = composer.ImeInsets().AsPaddingValues(composer);
    }

    internal Modifier Measure(int slot)
    {
        int version = _contentVersion;
        var callback = new ComposableLambda1(arg =>
        {
            if (!_owner.IsCurrentFrame(this))
                return;
            if ((slot == Body || slot == LayoutAck) && version != _contentVersion)
                return;
            var coordinates = arg?.JavaCast<ILayoutCoordinates>()
                ?? throw new InvalidOperationException(
                    "Scaffold insets measurement did not receive LayoutCoordinates.");
            _coordinates[slot] = coordinates;
            _bounds[slot] = ReadBounds(coordinates);
            if (slot == Body)
            {
                _bodyVersion = version;
                _bodySequence = ++_sequence;
            }
            else if (slot == LayoutAck)
            {
                _markerVersion = version;
                _markerSequence = ++_sequence;
            }
            _owner.ObserveDraw();
            if (_bodyVersion == _contentVersion && _markerVersion == _contentVersion)
                _pendingAtPlacement ??= _owner.HasPendingMeasureOrLayout;
            _owner.SignalProgress();
        });
        return Modifier.Companion.AppendBound(
            modifier => OnGloballyPositionedModifierKt.OnGloballyPositioned(modifier, callback),
            ModifierOpKey.Opaque);
    }

    internal void RecordPadding(PaddingValues padding)
    {
        // Take an independent JNI reference, not the callback's borrowed facade wrapper.
        var retained = PaddingValues.Wrap(padding.Jvm);
        var previous = _paddingPeer;
        _paddingPeer = retained;
        previous?.Dispose();
        _contentSequence = ++_sequence;
        _contentVersion++;
        _pendingAtPlacement = null;
        _pendingDraws = 0;
        _bodyVersion = -1;
        _markerVersion = -1;
        _bounds[Body] = null;
        _bounds[LayoutAck] = null;
        _coordinates[Body] = null;
        _coordinates[LayoutAck] = null;
    }

    internal void RecordBodyState(object sentinel, MutableNumberState<int> counter)
    {
        _bodyCompositions++;
        _bodyState = (sentinel, counter, counter.Value);
        _bodyObservationVersion = _owner.RecordBodyObservation();
        _owner.SignalProgress();
    }

    internal void RecordNativeArguments(IntPtr ownedGlobalReference, IWindowInsets nativeInsets)
    {
        _contentLambda = ownedGlobalReference;
        _nativeHandle = ((Java.Lang.Object)nativeInsets).Handle;
        var retained = WindowInsets.Wrap(nativeInsets);
        var previous = _nativeInsets;
        _nativeInsets = retained;
        previous?.Dispose();
        _nativeAtCall = ReadNative(nativeInsets);
        _nativeSequence = ++_sequence;
    }

    internal void CompleteIdleLayout(bool hasPendingWork, string admission)
    {
        _admission = admission;
        if (hasPendingWork)
        {
            _pendingDraws++;
            return;
        }
        CompletePlacement();
    }

    void CompletePlacement()
    {
        if (_bounds[Root] is not { } root || _bounds[Body] is not { } body
            || _bounds[LayoutAck] is not { } marker
            || _bodyVersion != _contentVersion || _markerVersion != _contentVersion
            || _bodyState is not { } bodyState
#if DEBUG
            || _contentLambda == IntPtr.Zero
#endif
            || (_bars && (_bounds[TopBar] is null || _bounds[BottomBar] is null)))
            return;

        var decor = _owner.Window?.DecorView
            ?? throw new InvalidOperationException("Window not set on ScaffoldInsetsTestActivity.");
        if (decor.RootWindowInsets is null)
            return;
        var padding = _paddingPeer
            ?? throw new InvalidOperationException("Scaffold body was placed before padding was observed.");
        var bodyAtCallback = body;
        for (int slot = 0; slot < _coordinates.Length; slot++)
        {
            if (_coordinates[slot] is { } coordinates)
            {
                if (!coordinates.IsAttached)
                    throw new InvalidOperationException($"Coordinates for Scaffold slot {slot} detached before draw.");
                _bounds[slot] = ReadBounds(coordinates);
            }
        }
        root = _bounds[Root] ?? throw new InvalidOperationException("Root coordinates were lost.");
        body = _bounds[Body] ?? throw new InvalidOperationException("Body coordinates were lost.");
        marker = _bounds[LayoutAck] ?? throw new InvalidOperationException("Marker coordinates were lost.");
        // Placement callbacks can precede more Compose work. Sample live peers only
        // after both acknowledgements and native composition/snapshot/layout idleness.
        var forwarded = Read(padding.Jvm);
        var boundDefault = Read(_boundDefault);
        var explicitReader = Read(_explicitReader.Jvm);
        var implicitReader = Read(_implicitReader.Jvm);
        var navigation = Read(_navigationBars.Jvm);
        var ime = Read(_ime.Jvm);
        Edges? nativeAtPlacement = _nativeInsets is { } native ? ReadNative(native.Jvm) : null;
        int expectedMarker = (int)Math.Round(LayoutToken.Value * _density,
            MidpointRounding.AwayFromZero);
        int actualMarker = (int)(marker.Right - marker.Left);
        string trace = $"render={_renderId} requested={_generation}/{_mode} " +
            $"native#{_nativeSequence}={_nativeAtCall} handle={_nativeHandle:x} " +
            $"contentPeerCaptured#{_contentSequence}/v{_contentVersion} " +
            $"bodyPlaced#{_bodySequence}/v{_bodyVersion}={bodyAtCallback} bodyCurrent={body} paddingDp={forwarded} " +
            $"nativeAtPlacementPx={nativeAtPlacement} " +
            $"markerPlaced#{_markerSequence}/v{_markerVersion}={actualMarker}px expected={expectedMarker}px " +
            $"bodyCompositions={_bodyCompositions} pendingAtPlacement={_pendingAtPlacement} " +
            $"pendingSamples={_pendingDraws} ownerPendingAtSample=false sampleAfterAck#{++_sequence}; {_admission}";
        var snapshot = new ScaffoldInsetsSnapshot(
            _generation, _mode, _density, decor.Width, decor.Height,
            root, body, _bounds[TopBar], _bounds[BottomBar], forwarded,
            boundDefault, explicitReader, implicitReader, navigation, ime,
            bodyState.Sentinel, _contentLambda, bodyState.Counter, bodyState.Value,
            expectedMarker, actualMarker, nativeAtPlacement, trace, _bodyObservationVersion);
        _owner.Publish(this, snapshot);
    }

    static Edges ReadBounds(ILayoutCoordinates coordinates)
    {
        var position = Offset.FromPacked(LayoutCoordinatesKt.PositionInRoot(coordinates));
        int width = (int)((ulong)coordinates.Size >> 32);
        int height = (int)(coordinates.Size & 0xFFFFFFFFL);
        return (position.X, position.Y, position.X + width, position.Y + height);
    }

    static Edges Read(IPaddingValues padding)
    {
        var direction = LayoutDirection.Ltr
            ?? throw new InvalidOperationException("Compose LayoutDirection.Ltr is unavailable.");
        return (
            padding.CalculateLeftPadding(direction),
            padding.CalculateTopPadding(),
            padding.CalculateRightPadding(direction),
            padding.CalculateBottomPadding());
    }

    Edges ReadNative(IWindowInsets insets)
    {
        using var density = DensityKt.Density(_density, 1f);
        var direction = LayoutDirection.Ltr
            ?? throw new InvalidOperationException("Compose LayoutDirection.Ltr is unavailable.");
        return (
            insets.GetLeft(density, direction),
            insets.GetTop(density),
            insets.GetRight(density, direction),
            insets.GetBottom(density));
    }
}
