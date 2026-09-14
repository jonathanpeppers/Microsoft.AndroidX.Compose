using Android.Runtime;
using Android.Views;
using AndroidX.Activity;
using AndroidX.Compose;
using AndroidX.Compose.Runtime;
using AndroidX.Compose.UI.Focus;
using FocusRequester = AndroidX.Compose.FocusRequester;
using FocusState = AndroidX.Compose.FocusState;
using Modifier = AndroidX.Compose.Modifier;
using Button = AndroidX.Compose.Button;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Real focus owner, text input session, and conditionally attached selector.</summary>
[Activity(Theme = "@android:style/Theme.Material.Light.NoActionBar",
    WindowSoftInputMode = SoftInput.AdjustResize)]
[Register("net/compose/devicetests/FocusTestActivity")]
public class FocusTestActivity : ComponentActivity
{
    internal static FocusTestActivity? Current;
    internal readonly TaskCompletionSource Destroyed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal MutableState<int> Selector { get; } = new(0);
    internal MutableState<int> Tick { get; } = new(0);
    internal FocusRequester? Editor;
    internal FocusRequester? Panel;
    internal FocusRequester? Child;
    internal IFocusManager? Manager;
    internal IFocusManager? ImplicitManager;
    internal IFocusManager? ProvidedManager;
    internal View? Owner;
    internal FocusState EditorState;
    internal FocusState PanelState;
    internal FocusState ChildState;
    internal readonly List<FocusState> EditorEvents = [];
    internal readonly List<FocusState> PanelEvents = [];
    internal int Requests;
    internal int Passes;
    internal bool Resumed;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        Window?.AddFlags(WindowManagerFlags.KeepScreenOn);
        this.SetContent(c => new Composed(Build));
        Volatile.Write(ref Current, this);
    }

    protected override void OnResume()
    {
        base.OnResume();
        Resumed = true;
    }

    protected override void OnPause()
    {
        Resumed = false;
        base.OnPause();
    }

    protected override void OnDestroy()
    {
        Window?.ClearFlags(WindowManagerFlags.KeepScreenOn);
        base.OnDestroy();
        if (ReferenceEquals(Current, this))
            Volatile.Write(ref Current, null);
        Destroyed.TrySetResult();
    }

    ComposableNode Build(IComposer composer)
    {
        var editor = composer.Remember(() => new FocusRequester());
        var panel = composer.Remember(() => new FocusRequester());
        var child = composer.Remember(() => new FocusRequester());
        Editor = editor;
        Panel = panel;
        Child = child;
        Manager = LocalFocusManager.Current(composer);
        Owner = LocalView.Current(composer);
        ObserveImplicit(this);
        var value = composer.MutableStateOf("");
        int selector = Selector.Value;
        int tick = Tick.Value;
        composer.LaunchedEffect(selector, _ =>
        {
            if (selector == 1 && Selector.Value == selector)
            {
                panel.RequestFocus();
                Requests++;
            }
            return Task.CompletedTask;
        });
        return new MaterialTheme
        {
            new Column
            {
                Modifier.FillMaxSize().SafeDrawingPadding(),
                new TextField(value, singleLine: true)
                {
                    Modifier = Modifier.FocusRequester(editor).OnFocusChanged(state =>
                    {
                        EditorState = state;
                        EditorEvents.Add(state);
                        if (state.IsFocused)
                            Selector.Value = 0;
                    }).Semantics("Native focus editor"),
                },
                new Button(() => Selector.Value = 1) { new Text("Open focus panel") },
                new Text($"Unrelated tick {tick}"),
                selector == 1 ? new Column
                {
                    Modifier.FocusRequester(panel).OnFocusChanged(state =>
                    {
                        PanelState = state;
                        PanelEvents.Add(state);
                    }).FocusTarget().Semantics("Native focus panel"),
                    new Text("A low-level focus target"),
                    new Text("Accessible child")
                    {
                        Modifier = Modifier.FocusRequester(child)
                            .OnFocusChanged(state => ChildState = state)
                            .Focusable().Semantics("Native focus child"),
                    },
                } : new Text($"Other selector {selector}"),
                new BackHandler(() => Selector.Value = 0, enabled: selector != 0),
                new CompositionLocalProvider
                {
                    LocalFocusManager.Provides(Manager),
                    new Composed(c =>
                    {
                        ProvidedManager = LocalFocusManager.Current(c);
                        return null;
                    }),
                },
                new SideEffect(() => Passes++),
            },
        };
    }

    /// <summary>Exercises the analyzer/interceptor-protected implicit local reader.</summary>
    [global::AndroidX.Compose.Composable]
    public static void ObserveImplicit(FocusTestActivity owner) =>
        owner.ImplicitManager = LocalFocusManager.Current();
}
