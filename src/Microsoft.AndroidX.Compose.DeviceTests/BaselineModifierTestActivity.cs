using Android.Runtime;
using AndroidX.Activity;
using AndroidX.Compose;
using AndroidX.Compose.UI.Layout;
using System.Collections.Concurrent;
using Baselines = AndroidX.Compose.UI.Layout.AlignmentLineKt;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Records real layout coordinates and baselines after Compose placement.</summary>
[Activity(Theme = "@android:style/Theme.Material.Light.NoActionBar")]
[Register("net/compose/devicetests/BaselineModifierTestActivity")]
public class BaselineModifierTestActivity : ComponentActivity
{
    internal static BaselineModifierTestActivity? Current { get; private set; }
    internal ConcurrentDictionary<string, Bounds> Measurements { get; } = new();
    internal ConcurrentDictionary<string, ScopeKind> Scopes { get; } = new();
    internal MutableState<int> Cycle { get; } = new(0);
    internal int RenderPasses;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        string scenario = Intent?.GetStringExtra("scenario")
            ?? throw new InvalidOperationException("Baseline test scenario missing.");
        Current = this;
        if (scenario == "direct")
            this.SetContent(_ => RenderDirect());
        else
            this.SetContent(composer => new Composed(currentComposer =>
            {
                _ = Cycle.Value;
                Interlocked.Increment(ref RenderPasses);
                return scenario switch
                {
                    "alignment" => AlignmentContent(),
                    "padding" => PaddingContent(),
                    "clipping" => ClippingContent(),
                    "column" => ColumnContent(),
                    _ => throw new InvalidOperationException($"Unknown baseline scenario: {scenario}."),
                };
            }));
    }

    protected override void OnDestroy()
    {
        if (ReferenceEquals(Current, this))
            Current = null;
        base.OnDestroy();
    }

    ComposableNode AlignmentContent() => new Column
    {
        Modifier.FillMaxSize(),
        new Row
        {
            Label("first-small", "Small", 16, Modifier.AlignByBaseline()),
            Label("first-large", "Two\nlines", 32, Modifier.AlignBy(Baselines.FirstBaseline)),
        },
        new Row
        {
            Label("last-small", "Small", 16, Modifier.AlignBy(Baselines.LastBaseline)),
            Label("last-large", "Two\nlines", 32, Modifier.AlignBy(Baselines.LastBaseline)),
        },
        new FlowRow
        {
            Label("flow-small", "Small", 16, Modifier.AlignByBaseline()),
            Label("flow-large", "Large", 32, Modifier.AlignByBaseline()),
        },
        new Row
        {
            Label("lone", "Lone", 16, Modifier.AlignByBaseline()),
            new Box { Observe("missing", Modifier.Size(20).AlignByBaseline()) },
        },
    };

    [Composable]
    internal static void RenderDirect()
    {
        var activity = Current ?? throw new InvalidOperationException("Baseline activity missing during composition.");
        _ = activity.Cycle.Value;
        Interlocked.Increment(ref activity.RenderPasses);
        Composables.Column(() =>
        {
            Composables.Row(() =>
            {
                Composables.Text("Small", fontSize: 16, modifier: activity.Observe("first-small", Modifier.AlignByBaseline()));
                Composables.Text("Two\nlines", fontSize: 32, modifier: activity.Observe("first-large", Modifier.AlignBy(Baselines.FirstBaseline)));
            });
            Composables.Row(() =>
            {
                Composables.Text("Small", fontSize: 16, modifier: activity.Observe("last-small", Modifier.AlignBy(Baselines.LastBaseline)));
                Composables.Text("Two\nlines", fontSize: 32, modifier: activity.Observe("last-large", Modifier.AlignBy(Baselines.LastBaseline)));
            });
        });
    }

    ComposableNode ColumnContent()
    {
        var line = new VerticalAlignmentLine(new BaselineLineMerger());
        return new Column
        {
            Modifier.FillMaxSize(),
            VerticalLineTile("column-small", line, 10),
            VerticalLineTile("column-large", line, 30),
            Label("column-missing-small", "Small", 16, Modifier.AlignBy(line)),
            Label("column-missing-large", "Large", 32, Modifier.AlignBy(line)),
            new FlowColumn
            {
                Modifier.Height(160),
                VerticalLineTile("flow-column-small", line, 10),
                VerticalLineTile("flow-column-large", line, 30),
            },
        };
    }

    Layout VerticalLineTile(string id, VerticalAlignmentLine line, int lineDp) => new((scope, _, _) =>
    {
        var nativeScope = Java.Lang.Object.GetObject<IMeasureScope>(scope.Handle, JniHandleOwnership.DoNotTransfer)
            ?? throw new InvalidOperationException("Native vertical-line measure scope missing.");
        using var position = Java.Lang.Integer.ValueOf(scope.RoundToPx(new Dp(lineDp)))
            ?? throw new InvalidOperationException("Boxed vertical-line position missing.");
        // The bound overload publishes test-only lines without expanding the public Layout facade.
        var result = nativeScope.Layout(scope.RoundToPx(new Dp(60)), scope.RoundToPx(new Dp(24)),
            new Dictionary<AlignmentLine, Java.Lang.Integer> { [line] = position },
            new ComposableLambda1(_ => { }));
        return new MeasureResult(((Java.Lang.Object)result).Handle, JniHandleOwnership.DoNotTransfer);
    })
    {
        Modifier = Observe(id, Modifier.AlignBy(line), line),
    };

    ComposableNode PaddingContent() => new Column
    {
        Modifier.FillMaxSize(),
        Label("natural", "A\nB", 12, Modifier.Companion),
        Label("null", "A\nB", 12, Modifier.PaddingFrom(Baselines.FirstBaseline)),
        Label("zero", "A\nB", 12, Modifier.PaddingFrom(Baselines.FirstBaseline, before: 0, after: 0)),
        Label("before", "A\nB", 12, Modifier.PaddingFrom(Baselines.FirstBaseline, before: 32)),
        Label("after", "A\nB", 12, Modifier.PaddingFrom(Baselines.LastBaseline, after: 24)),
        Label("both", "A\nB", 12, Modifier.PaddingFromBaseline(top: 32, bottom: 24)),
        Label("baseline-null", "A\nB", 12, Modifier.PaddingFromBaseline()),
        Label("baseline-zero", "A\nB", 12, Modifier.PaddingFromBaseline(top: 0, bottom: 0)),
        Label("constrained", "A\nB", 12, Modifier.Height(40).PaddingFromBaseline(top: 32, bottom: 24)),
        Label("min-null", "A", 12, Modifier.Height(64).PaddingFrom(Baselines.FirstBaseline, after: 24)),
        Label("min-zero", "A", 12, Modifier.Height(64).PaddingFrom(Baselines.FirstBaseline, before: 0, after: 24)),
        new Box { Observe("absent-line", Modifier.PaddingFrom(Baselines.FirstBaseline, before: 32).Size(12)) },
    };

    ComposableNode ClippingContent() => new Column
    {
        Modifier.FillMaxSize().Background(Color.White).Padding(24.Dp()),
        ClipTile("unclipped", false),
        Spacer.Height(24),
        ClipTile("clipped", true),
    };

    Box ClipTile(string id, bool clip)
    {
        var modifier = Observe(id, Modifier.Size(100, 48));
        if (clip)
            modifier = modifier.ClipToBounds();
        return new Box
        {
            modifier,
            new Box { Modifier.Size(100, 48).Offset(x: 40).Background(Color.Red) },
        };
    }

    Text Label(string id, string text, int size, Modifier modifier) => new(text)
    {
        FontSize = new Sp(size),
        Modifier = Observe(id, modifier),
    };

    Modifier Observe(string id, Modifier modifier, AlignmentLine? line = null)
    {
        var callback = new ComposableLambda1(arg =>
        {
            var coordinates = arg?.JavaCast<ILayoutCoordinates>()
                ?? throw new InvalidOperationException("Baseline measurement did not receive LayoutCoordinates.");
            if (!coordinates.IsAttached)
                throw new InvalidOperationException("Baseline measurement received detached coordinates.");
            var position = Offset.FromPacked(LayoutCoordinatesKt.PositionInWindow(coordinates));
            long size = coordinates.Size;
            Measurements[id] = new Bounds(
                position.X, position.Y, (int)(size >> 32), (int)size,
                coordinates.Get(Baselines.FirstBaseline), coordinates.Get(Baselines.LastBaseline),
                line is null ? AlignmentLine.Unspecified : coordinates.Get(line));
        });
        // Put the observer outside padding so distances are measured from the padded bounds.
        return Modifier.Companion.AppendBound(current =>
        {
            Scopes[id] = RenderContext.CurrentScopeKind;
            return OnGloballyPositionedModifierKt.OnGloballyPositioned(current, callback);
        }, ModifierOpKey.Opaque).Then(modifier);
    }

    internal readonly record struct Bounds(float X, float Y, int Width, int Height, int First, int Last, int Line);
}
