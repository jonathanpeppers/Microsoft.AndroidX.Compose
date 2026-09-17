using Android.Runtime;
using AndroidX.Activity;
using AndroidX.Compose;
using AndroidX.Compose.Foundation.Layout;
using AndroidX.Compose.Runtime;
using Color = AndroidX.Compose.Color;
using FlowColumn = AndroidX.Compose.FlowColumn;
using FlowColumnOverflow = AndroidX.Compose.FlowColumnOverflow;
using FlowRow = AndroidX.Compose.FlowRow;
using FlowRowOverflow = AndroidX.Compose.FlowRowOverflow;
using Composable = AndroidX.Compose.ComposableAttribute;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Fixed-cell flows with managed indicators and a bound-native compatibility control.</summary>
[Activity(Theme = "@android:style/Theme.Material.Light.NoActionBar")]
[Register("net/compose/devicetests/FlowOverflowTestActivity")]
public class FlowOverflowTestActivity : ComponentActivity
{
    internal static TaskCompletionSource<FlowOverflowTestActivity> Ready = NewReady();
    internal static TaskCompletionSource<FlowOverflowTestActivity> NewReady() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal TaskCompletionSource Destroyed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal MutableState<int> Lines { get; } = new(1);
    internal MutableState<int> Total { get; } = new(8);
    internal MutableState<int> Generation { get; } = new(0);
    internal MutableState<int> Tick { get; } = new(0);
    internal MutableState<int> ThresholdPhase { get; } = new(0);
    internal FlowOverflowSnapshot? Last;
    internal readonly Dictionary<int, HashSet<int>> DrawnItems = [];
    internal readonly Dictionary<int, Dictionary<int, object>> ComposedItems = [];
    internal readonly Dictionary<string, (int Total, int Shown)> NestedCounts = [];
    internal readonly Dictionary<string, ScopeKind> ScopeChecks = [];
    internal readonly Dictionary<int, (bool Expand, int Counter, ScopeKind Kind)> NodeIndicators = [];
    internal bool PrematureReadRejected;
    internal bool OuterRestored;
    internal int RootPasses;
    internal int Clicks;
    internal FlowTestAdmission Admission { get; } = new();
    internal global::Android.Views.View? Owner;
    int _style;
    int _policy;
    bool _horizontal;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        _style = Intent?.GetIntExtra("style", 0) ?? 0;
        _policy = Intent?.GetIntExtra("policy", 3) ?? 3;
        _horizontal = Intent?.GetBooleanExtra("horizontal", true) ?? true;
        ThresholdPhase.Value = Intent?.GetIntExtra("thresholdPhase", 0) ?? 0;
        this.SetContent(c => Root(c, this));
        Ready.TrySetResult(this);
    }

    protected override void OnResume()
    {
        base.OnResume();
        Admission.Resumed = true;
    }

    protected override void OnPause()
    {
        Admission.Paused();
        base.OnPause();
    }

    /// <summary>Records loss of native fixture focus without throwing across a lifecycle callback.</summary>
    public override void OnWindowFocusChanged(bool hasFocus)
    {
        base.OnWindowFocusChanged(hasFocus);
        Admission.FocusChanged(hasFocus, Owner?.HasWindowFocus == true);
    }

    [Composable]
    internal static void Root(IComposer composer, FlowOverflowTestActivity activity)
    {
        activity.RootPasses++;
        activity.Owner = LocalView.Current(composer);
        int lines = activity.Lines.Value;
        int total = activity.Total.Value;
        int generation = activity.Generation.Value;
        _ = activity.ThresholdPhase.Value;
        var modifier = activity._horizontal ? Modifier.Width(144) : Modifier.Height(144).Width(220);
        if (activity._style == 2)
        {
            activity.Native(composer, modifier, lines, total, generation);
        }
        else if (activity._horizontal)
        {
            if (activity._style is 0 or 3)
            {
                var flow = new FlowRow(3, lines) { Overflow = activity.RowOption(generation) };
                flow.Add(modifier);
                for (int i = 0; i < total; i++)
                    flow.Add(activity.Item(i, generation));
                flow.Render(composer);
            }
            else if (activity._policy == 0)
                Composables.FlowRow(() => activity.Items(total, generation), 3, lines, modifier);
            else
                Composables.FlowRow(() => activity.Items(total, generation), 3, lines, modifier,
                    activity.RowOption(generation));
        }
        else
        {
            if (activity._style is 0 or 3)
            {
                var flow = new FlowColumn(3, lines) { Overflow = activity.ColumnOption(generation) };
                flow.Add(modifier);
                for (int i = 0; i < total; i++)
                    flow.Add(activity.Item(i, generation));
                flow.Render(composer);
            }
            else if (activity._policy == 0)
                Composables.FlowColumn(() => activity.Items(total, generation), 3, lines, modifier);
            else
                Composables.FlowColumn(() => activity.Items(total, generation), 3, lines, modifier,
                    activity.ColumnOption(generation));
        }
        activity.OuterRestored = RenderContext.CurrentScopeKind == ScopeKind.None;
    }

    FlowRowOverflow? RowOption(int generation) => _policy switch
    {
        0 => null,
        1 => FlowRowOverflow.Clip,
        2 when _style is 3 or 4 => FlowRowOverflow.ExpandIndicator(NodeIndicator(generation, true)),
        2 when _style == 1 => FlowRowOverflow.ExpandIndicator(scope =>
        {
            Indicator(scope, generation, true).Render(ComposableContext.Current);
        }),
        2 => FlowRowOverflow.ExpandIndicator(scope => Indicator(scope, generation, true)),
        _ when _style is 3 or 4 => FlowRowOverflow.ExpandOrCollapseIndicator(
            NodeIndicator(generation, true), NodeIndicator(generation, false),
            minRowsToShowCollapse: MinimumLines, minHeightToShowCollapse: MinimumCrossAxisSize),
        _ when _style == 1 => FlowRowOverflow.ExpandOrCollapseIndicator(
            scope => { Indicator(scope, generation, true).Render(ComposableContext.Current); },
            scope => { Indicator(scope, generation, false).Render(ComposableContext.Current); }),
        _ => FlowRowOverflow.ExpandOrCollapseIndicator(
            scope => Indicator(scope, generation, true),
            scope => Indicator(scope, generation, false)),
    };

    FlowColumnOverflow? ColumnOption(int generation) => _policy switch
    {
        0 => null,
        1 => FlowColumnOverflow.Clip,
        2 when _style is 3 or 4 => FlowColumnOverflow.ExpandIndicator(NodeIndicator(generation, true)),
        2 when _style == 1 => FlowColumnOverflow.ExpandIndicator(scope =>
        {
            Indicator(scope, generation, true).Render(ComposableContext.Current);
        }),
        2 => FlowColumnOverflow.ExpandIndicator(scope => Indicator(scope, generation, true)),
        _ when _style is 3 or 4 => FlowColumnOverflow.ExpandOrCollapseIndicator(
            NodeIndicator(generation, true), NodeIndicator(generation, false),
            minColumnsToShowCollapse: MinimumLines, minWidthToShowCollapse: MinimumCrossAxisSize),
        _ when _style == 1 => FlowColumnOverflow.ExpandOrCollapseIndicator(
            scope => { Indicator(scope, generation, true).Render(ComposableContext.Current); },
            scope => { Indicator(scope, generation, false).Render(ComposableContext.Current); }),
        _ => FlowColumnOverflow.ExpandOrCollapseIndicator(
            scope => Indicator(scope, generation, true),
            scope => Indicator(scope, generation, false)),
    };

    int? MinimumLines => ThresholdPhase.Value switch { 0 => null, 1 => 4, _ => 2 };
    Dp? MinimumCrossAxisSize => ThresholdPhase.Value switch
    {
        0 => null,
        2 => new Dp(200),
        _ => new Dp(96),
    };

    ComposableNode NodeIndicator(int generation, bool expand) => new Composed(composer =>
    {
        var counter = composer.Remember(() => new MutableState<int>(0));
        int value = counter.Value;
        var kind = RenderContext.CurrentScopeKind;
        return new Box
        {
            Modifier.Size(48).Background(Color.Blue)
                .Semantics(expand ? "flow-expand" : "flow-collapse")
                .Clickable(() =>
                {
                    Clicks++;
                    counter.Value++;
                    Lines.Value = expand ? 4 : 1;
                    Generation.Value++;
                })
                .DrawWithContent(draw =>
                {
                    draw.DrawContent();
                    NodeIndicators[generation] = (expand, value, kind);
                }),
            new Text(expand ? "+" : "-") { Color = Color.White },
        };
    });

    void Items(int total, int generation)
    {
        var composer = ComposableContext.Current;
        for (int i = 0; i < total; i++)
        {
            composer.StartReplaceableGroup(CompositionGroupKey.Compute(i, typeof(Box)));
            try { Item(i, generation).Render(composer); }
            finally { composer.EndReplaceableGroup(); }
        }
    }

    ComposableNode Item(int index, int generation) => new Composed(composer =>
    {
        if (!ComposedItems.TryGetValue(generation, out var items))
            ComposedItems[generation] = items = [];
        items[index] = composer.Remember(() => new object());
        return new Box
        {
            Modifier.Size(48).Background(Color.LightGray).DrawWithContent(draw =>
            {
                draw.DrawContent();
                if (!DrawnItems.TryGetValue(generation, out var drawn))
                    DrawnItems[generation] = drawn = [];
                drawn.Add(index);
            }),
            new Text(index.ToString()) { Color = Color.Black },
        };
    });

    ComposableNode Indicator(FlowOverflowScope scope, int generation, bool expand) =>
        Indicator(() => scope.TotalItemCount, () => scope.ShownItemCount, generation, expand);

    ComposableNode Indicator(Func<int> total, Func<int> shown, int generation, bool expand) =>
        new Composed(composer =>
        {
            if (generation == 0 && !PrematureReadRejected)
            {
                try { _ = shown(); }
                catch (Java.Lang.IllegalStateException ex) when (ex.Message?.Contains("before it is set") == true)
                {
                    PrematureReadRejected = true;
                }
            }
            int tick = Tick.Value;
            var identity = composer.Remember(() => new object());
            var counter = composer.Remember(() => new MutableState<int>(0));
            int counterValue = counter.Value;
            var kind = RenderContext.CurrentScopeKind;
            var key = expand ? "expand" : "collapse";
            return new Box
            {
                Modifier.Size(48).Background(Color.Blue).Semantics("flow-" + key)
                    .Clickable(() =>
                    {
                        Clicks++;
                        counter.Value++;
                        Lines.Value = expand ? 4 : 1;
                        Generation.Value++;
                    })
                    .DrawWithContent(draw =>
                    {
                        draw.DrawContent();
                        Last = new(Admission.InstanceId, global::Android.OS.Process.MyPid(),
                            generation, tick, expand, total(), shown(), identity, counterValue, kind);
                    }),
                new Text(expand ? "+" : "-") { Color = Color.White },
                new Composed(c =>
                {
                    ScopeChecks[key + "-before"] = RenderContext.CurrentScopeKind;
                    if (_horizontal)
                    {
                        var nested = new FlowColumn(2, 1)
                        {
                            Overflow = FlowColumnOverflow.ExpandIndicator(scope => new Box
                            {
                                Modifier.Size(8).DrawBehind(_ =>
                                    NestedCounts[key] = (scope.TotalItemCount, scope.ShownItemCount)),
                            }),
                        };
                        nested.Add(Modifier.Size(24));
                        for (int i = 0; i < 4; i++) nested.Add(new Box { Modifier.Size(8) });
                        nested.Render(c);
                    }
                    else
                    {
                        var nested = new FlowRow(2, 1)
                        {
                            Overflow = FlowRowOverflow.ExpandIndicator(scope => new Box
                            {
                                Modifier.Size(8).DrawBehind(_ =>
                                    NestedCounts[key] = (scope.TotalItemCount, scope.ShownItemCount)),
                            }),
                        };
                        nested.Add(Modifier.Size(24));
                        for (int i = 0; i < 4; i++) nested.Add(new Box { Modifier.Size(8) });
                        nested.Render(c);
                    }
                    ScopeChecks[key + "-after"] = RenderContext.CurrentScopeKind;
                    return null;
                }),
            };
        });

#pragma warning disable CS0618 // Direct bound-native control for the pinned deprecated API.
    void Native(IComposer composer, Modifier modifier, int lines, int total, int generation)
    {
        var expand = ComposableLambdas.Wrap3(composer,
            (handle, current) => NativeIndicator(handle, current, generation, true));
        var collapse = ComposableLambdas.Wrap3(composer,
            (handle, current) => NativeIndicator(handle, current, generation, false));
        var content = ComposableLambdas.Wrap3(composer, _ => Items(total, generation));
        if (_horizontal)
        {
            var overflow = FlowOverflowNative.Row.ExpandOrCollapseIndicator__jt2gSs(
                expand, collapse, 0, 0, composer, 0, (int)FlowRowIndicatorDefault.All);
            FlowLayoutKt.FlowRow(modifier.Build(), null, null, null, 3, lines,
                overflow, content, composer, 0,
                (int)(FlowRowDefault.HorizontalArrangement | FlowRowDefault.VerticalArrangement |
                      FlowRowDefault.ItemVerticalAlignment));
        }
        else
        {
            var overflow = FlowOverflowNative.Column.ExpandOrCollapseIndicator__jt2gSs(
                expand, collapse, 0, 0, composer, 0, (int)FlowColumnIndicatorDefault.All);
            FlowLayoutKt.FlowColumn(modifier.Build(), null, null, null, 3, lines,
                overflow, content, composer, 0,
                (int)(FlowColumnDefault.HorizontalArrangement | FlowColumnDefault.VerticalArrangement |
                      FlowColumnDefault.ItemHorizontalAlignment));
        }
    }
#pragma warning restore CS0618

    void NativeIndicator(IntPtr handle, IComposer composer, int generation, bool expand)
    {
        using var scope = RenderContext.PushScope(handle, _horizontal ? ScopeKind.Row : ScopeKind.Column);
        if (_horizontal)
        {
            var peer = Java.Lang.Object.GetObject<IFlowRowOverflowScope>(handle, JniHandleOwnership.DoNotTransfer)
                ?? throw new InvalidOperationException("Native row control supplied no scope.");
            Indicator(() => peer.TotalItemCount, () => peer.ShownItemCount, generation, expand).Render(composer);
        }
        else
        {
            var peer = Java.Lang.Object.GetObject<IFlowColumnOverflowScope>(handle, JniHandleOwnership.DoNotTransfer)
                ?? throw new InvalidOperationException("Native column control supplied no scope.");
            Indicator(() => peer.TotalItemCount, () => peer.ShownItemCount, generation, expand).Render(composer);
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Destroyed.TrySetResult();
    }
}
