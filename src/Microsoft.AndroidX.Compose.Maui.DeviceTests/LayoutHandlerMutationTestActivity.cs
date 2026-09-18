using Android.OS;
using Android.Runtime;
using AndroidX.Activity;
using AndroidX.Compose;
using AndroidX.Compose.Runtime;
using ComposeLayoutHandler = Microsoft.AndroidX.Compose.Maui.Handlers.LayoutHandler;
using MauiLayout = Microsoft.Maui.Controls.Layout;

namespace Microsoft.AndroidX.Compose.Maui.DeviceTests;

/// <summary>Attached Compose host for dynamic MAUI stack child mutation regressions.</summary>
[global::Android.App.Activity(Theme = "@android:style/Theme.Material.Light.NoActionBar")]
[Register("net/compose/maui/devicetests/LayoutHandlerMutationTestActivity")]
public sealed class LayoutHandlerMutationTestActivity : ComponentActivity
{
    static bool s_vertical;

    ServiceProvider? _services;
    ComposeLayoutHandler? _handler;
    MauiLayout? _layout;
    Microsoft.Maui.Controls.Label[] _children = [];
    Microsoft.Maui.Controls.Label? _c;
    Microsoft.Maui.Controls.Label? _x;
    Microsoft.Maui.Controls.Label? _b;
    Microsoft.Maui.Controls.Label? _replacement;
    MutableState<int>? _unrelatedState;
    global::AndroidX.Compose.UI.Platform.ComposeView? _composeView;
    TaskCompletionSource<int> _initialApplied = NewSignal<int>();
    TaskCompletionSource<int>? _pendingApplied;
    int _pendingPass;
    int _appliedPasses;

    internal static void Reset(bool vertical) => s_vertical = vertical;

    internal IDictionary<string, object> Observed { get; } = new Dictionary<string, object>();
    internal IList<string> Order { get; } = new List<string>();
    internal IDictionary<string, int> Disposals { get; } = new Dictionary<string, int>();
    internal Task<int> InitialApplied => _initialApplied.Task;
    internal TaskCompletionSource Destroyed { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal int AppliedPasses => Volatile.Read(ref _appliedPasses);

    /// <inheritdoc/>
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        var layout = s_vertical
            ? (MauiLayout)new Microsoft.Maui.Controls.VerticalStackLayout()
            : new Microsoft.Maui.Controls.HorizontalStackLayout();
        var a = Child("A");
        var b = Child("B");
        var c = Child("C");
        var x = Child("X");
        var replacement = Child("R");
        _children = [a, b, c, x, replacement];
        _b = b;
        _c = c;
        _x = x;
        _replacement = replacement;
        layout.Add(a);
        layout.Add(b);

        var services = new ServiceCollection().BuildServiceProvider();
        _services = services;
        var handler = new ComposeLayoutHandler();
        handler.SetMauiContext(new MauiContext(services, this));
        layout.Handler = handler;
        _handler = handler;
        _layout = layout;
        _unrelatedState = new MutableState<int>(0);

        var composeView = new global::AndroidX.Compose.UI.Platform.ComposeView(this);
        composeView.SetContent(_ => new Composed(RenderLayout));
        SetContentView(composeView);
        _composeView = composeView;
    }

    internal (int PreviousVersion, int CurrentVersion, int ExpectedPass, Task<int> Applied)
        Mutate(string command)
    {
        var handler = _handler
            ?? throw new InvalidOperationException("LayoutHandler not set on mutation activity.");
        var layout = _layout
            ?? throw new InvalidOperationException("Layout not set on mutation activity.");
        if (_pendingApplied is { Task.IsCompleted: false })
            throw new InvalidOperationException("A prior layout mutation is still awaiting its applied pass.");

        int previousVersion = handler.ChildrenVersion;
        int expectedPass = AppliedPasses + 1;
        var applied = NewSignal<int>();
        _pendingPass = expectedPass;
        _pendingApplied = applied;

        switch (command)
        {
            case "Add":
                layout.Add(_c
                    ?? throw new InvalidOperationException("Add child not set on mutation activity."));
                break;
            case "Insert":
                layout.Insert(1, _x
                    ?? throw new InvalidOperationException("Insert child not set on mutation activity."));
                break;
            case "Remove":
                if (!layout.Remove(_b
                    ?? throw new InvalidOperationException("Remove child not set on mutation activity.")))
                    throw new InvalidOperationException("Expected layout child was not removed.");
                break;
            case "Update":
                layout[1] = _replacement
                    ?? throw new InvalidOperationException("Replacement child not set on mutation activity.");
                break;
            case "Clear":
                layout.Clear();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(command), command, "Unknown layout mutation.");
        }

        return (previousVersion, handler.ChildrenVersion, expectedPass, applied.Task);
    }

    internal void WriteUnrelatedState()
    {
        var state = _unrelatedState
            ?? throw new InvalidOperationException("Unrelated state not set on mutation activity.");
        state.Value++;
    }

    ComposableNode? RenderLayout(IComposer composer)
    {
        var handler = _handler
            ?? throw new InvalidOperationException("LayoutHandler not set during composition.");
        Observed.Clear();
        Order.Clear();
        var node = handler.BuildNode(composer);
        bool expectedType = s_vertical ? node is Column : node is Row;
        if (!expectedType)
        {
            throw new InvalidOperationException(
                $"LayoutHandler returned '{node.GetType().Name}' for a {(s_vertical ? "vertical" : "horizontal")} stack.");
        }
        node.Render(composer);
        composer.SideEffect(CompleteAppliedPass);
        return null;
    }

    void CompleteAppliedPass()
    {
        int pass = Interlocked.Increment(ref _appliedPasses);
        _initialApplied.TrySetResult(pass);
        if (_pendingApplied is { } pending && pass >= _pendingPass)
        {
            _pendingApplied = null;
            pending.TrySetResult(pass);
        }
    }

    Microsoft.Maui.Controls.Label Child(string id)
    {
        var child = new Microsoft.Maui.Controls.Label { Text = id };
        child.Handler = new MovableStateProbeHandler(this, id, Observed, Order, Disposals);
        return child;
    }

    /// <inheritdoc/>
    protected override void OnDestroy()
    {
        try
        {
            _composeView?.DisposeComposition();
            _composeView = null;
            _initialApplied.TrySetCanceled();
            _pendingApplied?.TrySetCanceled();
            if (_layout is { } layout)
                layout.Handler = null;
            foreach (var child in _children)
                child.Handler = null;
            _services?.Dispose();
        }
        finally
        {
            base.OnDestroy();
            Destroyed.TrySetResult();
        }
    }

    static TaskCompletionSource<T> NewSignal<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);
}
