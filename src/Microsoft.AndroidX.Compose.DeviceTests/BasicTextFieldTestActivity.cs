using Android.Runtime;
using Android.Views;
using Android.Views.InputMethods;
using AndroidX.Activity;
using AndroidX.Compose;
using AndroidX.Compose.Runtime;
using AndroidX.Compose.UI.Layout;
using AndroidX.Compose.UI.Text.Input;
using AndroidX.Compose.Samples.Jetchat;
using TextLayoutResult = AndroidX.Compose.UI.Text.TextLayoutResult;
using Composable = AndroidX.Compose.ComposableAttribute;
using Button = AndroidX.Compose.Button;
using KeyboardType = AndroidX.Compose.KeyboardType;
using Kotlin.Jvm.Functions;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>A focused native editor with frame-completion barriers for input regressions.</summary>
[Activity(Theme = "@android:style/Theme.Material.Light.NoActionBar", WindowSoftInputMode = SoftInput.AdjustResize)]
[Register("net/compose/devicetests/BasicTextFieldTestActivity")]
public class BasicTextFieldTestActivity : ComponentActivity
{
    internal static TaskCompletionSource<BasicTextFieldTestActivity> Ready { get; set; } = NewReady();
    internal static TaskCompletionSource<BasicTextFieldTestActivity> Created { get; set; } = NewReady();
    internal static TaskCompletionSource<BasicTextFieldTestActivity> NewReady() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal MutableState<TextFieldValue> Input { get; } = new(ComposeExtensions.NewTextFieldValue());
    internal MutableState<string> StringInput { get; } = new("");
    internal MutableState<int> Revision { get; } = new(0);
    internal Java.Lang.Integer BorrowedFlags { get; } = Java.Lang.Integer.ValueOf(0)
        ?? throw new InvalidOperationException("Shared boxed flags missing.");
    internal FocusRequester Requester { get; } = new();
    internal List<string> Sent { get; } = [];
    internal bool Focused;
    internal bool SingleLine;
    internal bool ReadOnly = false;
    internal bool Decorated = true;
    internal int MinLines = 1;
    internal int MaxLines = 3;
    internal int LayoutRevision = -1;
    internal int DecorationRevision = -1;
    internal TextLayoutResult? TextLayout;
    internal int ResetCount;
    internal int DismissCount;
    internal int EditorHeight;
    internal string Stage { get; private set; } = "launch";
    int _appliedRevision = -1;
    bool _resumed;
    int _route;
    int _tracedRevision = -1;
    IInputConnection? _testConnection;
    bool IsStringRoute => _route is 3 or 4 or 5 or 7;
    TaskCompletionSource? _frame;
    TaskCompletionSource<BasicTextFieldTestActivity>? _ready;
    internal TaskCompletionSource Destroyed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        _ready = Ready;
        Created.TrySetResult(this);
        _route = Intent?.GetIntExtra("route", 0) ?? 0;
        var decor = Window?.DecorView ?? throw new InvalidOperationException("Editor window missing.");
        var observer = decor.ViewTreeObserver ?? throw new InvalidOperationException("Editor view observer missing.");
        observer.PreDraw += OnPreDraw;
        this.SetContent((IComposer c) => Content(c, this));
    }

    protected override void OnResume()
    {
        base.OnResume();
        _resumed = true;
    }

    protected override void OnPause()
    {
        _resumed = false;
        base.OnPause();
    }

    void OnPreDraw(object? sender, ViewTreeObserver.PreDrawEventArgs e)
    {
        e.Handled = true;
        var decor = Window?.DecorView;
        if (!_resumed || !HasWindowFocus || decor?.RootWindowInsets is null ||
            decor.IsLayoutRequested || _appliedRevision != Revision.Value)
            return;
        // Completion is posted after the actual traversal, never a timer or value poll.
        var frame = _frame;
        var ready = _ready;
        decor.Post(() =>
        {
            if (_tracedRevision != _appliedRevision)
            {
                _tracedRevision = _appliedRevision;
                Trace("applied frame");
            }
            ready?.TrySetResult(this);
            frame?.TrySetResult();
        });
    }

    protected override void OnDestroy()
    {
        if (Window?.DecorView?.ViewTreeObserver is { IsAlive: true } observer)
            observer.PreDraw -= OnPreDraw;
        if (_testConnection is { } connection)
        {
            Trace("closing test-owned input connection", includeNative: false);
            connection.CloseConnection();
            connection.Dispose();
            _testConnection = null;
        }
        base.OnDestroy();
        Destroyed.TrySetResult();
    }

    internal Task MutateAsync(Action action,
        [System.Runtime.CompilerServices.CallerArgumentExpression(nameof(action))] string stage = "")
    {
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        RunOnUiThread(() =>
        {
            try
            {
                _frame = completed;
                Stage = stage;
                Trace("mutation");
                action();
                Revision.Value++;
            }
            catch (Exception ex)
            {
                completed.TrySetException(ex);
            }
        });
        return completed.Task.WaitAsync(TimeSpan.FromSeconds(15));
    }

    internal void Send()
    {
        if (!IsStringRoute)
            MessageInput.Send(Input, Sent.Add, () => ResetCount++, () => DismissCount++);
        else if (!string.IsNullOrWhiteSpace(StringInput.Value))
        {
            Sent.Add(StringInput.Value);
            StringInput.Value = "";
            ResetCount++;
            DismissCount++;
        }
    }

    internal IInputConnection OpenConnection(EditorInfo info)
    {
        _testConnection = TryOpenConnection(info)
            ?? throw new InvalidOperationException("Native Compose owner returned no input connection.");
        Trace($"opened test input connection imeOptions={info.ImeOptions}");
        return _testConnection;
    }

    void ChangeValue(TextFieldValue value)
    {
        Input.Value = value;
        Trace($"onValueChange {value}");
    }

    void ChangeValue(string value)
    {
        StringInput.Value = value;
        Trace($"onValueChange '{value}'");
    }

    void Trace(string message, bool includeNative = true)
    {
        var insets = Window?.DecorView?.RootWindowInsets;
        bool imeVisible = OperatingSystem.IsAndroidVersionAtLeast(30) &&
            insets?.IsVisible(global::Android.Views.WindowInsets.Type.Ime()) == true;
        string native = !includeNative || _testConnection is null ? "no connection snapshot" :
            TextFieldConnectionSnapshot.Read(_testConnection);
        global::Android.Util.Log.Info("EditorTrace",
            $"route={_route} rev={Revision.Value} focused={Focused} window={HasWindowFocus} ime={imeVisible} " +
            $"{message}; stage={Stage}; {native}");
    }

    internal IInputConnection? TryOpenConnection(EditorInfo info)
    {
        var root = Window?.DecorView ?? throw new InvalidOperationException("Editor decor missing.");
        var owner = FindOwner(root)
            ?? throw new InvalidOperationException("Native Compose owner missing:\n" + Describe(root));
        if (!HasWindowFocus || !owner.IsAttachedToWindow || !owner.HasFocus || owner.RootWindowInsets is null)
            throw new InvalidOperationException("Native editor owner is not attached, focused and inset-ready:\n" + Describe(root));
        return owner.OnCreateInputConnection(info);
    }

    static View? FindOwner(View view)
    {
        if (view.Class?.Name == "androidx.compose.ui.platform.AndroidComposeView")
            return view;
        if (view is ViewGroup group)
            for (int i = 0; i < group.ChildCount; i++)
                if (group.GetChildAt(i) is { } child && FindOwner(child) is { } owner)
                    return owner;
        return null;
    }

    static string Describe(View view, string indent = "")
    {
        var text = $"{indent}{view.Class?.Name} managed={view.GetType().FullName} focus={view.HasFocus} attached={view.IsAttachedToWindow}\n";
        if (view is ViewGroup group)
            for (int i = 0; i < group.ChildCount; i++)
                if (group.GetChildAt(i) is { } child)
                    text += Describe(child, indent + "  ");
        return text;
    }

    [Composable]
    internal static void Content(IComposer c, BasicTextFieldTestActivity activity)
    {
        int revision = activity.Revision.Value;
        activity.Trace("composition");
        var options = c.Remember(() =>
        {
            var d = KeyboardOptionsCompanion.Default;
            return d.Copy(d.Capitalization, d.AutoCorrectEnabled, KeyboardType.Text,
                global::AndroidX.Compose.ImeAction.Send, d.PlatformImeOptions, d.ShowKeyboardOnFocus, d.HintLocales);
        });
        var actions = c.Remember(() => KeyboardActionsHelper.Create(onSend: activity.Send));
        var positioned = c.RememberAction(peer =>
        {
            var coordinates = peer?.JavaCast<ILayoutCoordinates>()
                ?? throw new InvalidOperationException("Editor layout coordinates missing.");
            activity.EditorHeight = (int)coordinates.Size;
        });
        var modifier = Modifier.FillMaxWidth().FocusRequester(activity.Requester)
            .OnFocusChanged(state => activity.Focused = state.IsFocused)
            .AppendBound(m => OnGloballyPositionedModifierKt.OnGloballyPositioned(m, positioned),
                ModifierOpKey.Opaque);
        var style = new TextStyle { Color = Color.Black, FontSize = 20, LineHeight = 24 };
        var brush = c.Remember(() => Brush.SolidColor(Color.Red));
        Action<TextLayoutResult> layout = result =>
        {
            activity.TextLayout = result;
            activity.LayoutRevision = revision;
            activity.Trace($"onTextLayout lines={result.LineCount} height={activity.EditorHeight}");
        };
        new MaterialTheme
        {
            new Column
            {
                Modifier.FillMaxSize().Background(Color.White).StatusBarsPadding().Padding(16.Dp()),
                new Composed(composer =>
                {
                    if (activity.IsStringRoute)
                    {
                        StringEditor(composer, activity, modifier, style, brush, options, actions, layout, revision);
                        return null;
                    }
                    if (activity._route == 6)
                    {
                        NativeEditor(composer, activity, modifier, style, brush, options, actions, layout, revision);
                        return null;
                    }
                    if (activity._route == 0)
                        return new BasicTextField(activity.Input.Value, value => activity.ChangeValue(value),
                            readOnly: activity.ReadOnly, singleLine: activity.SingleLine,
                            maxLines: activity.MaxLines, minLines: activity.MinLines)
                        {
                            Modifier = modifier,
                            TextStyle = style,
                            CursorBrush = brush,
                            KeyboardOptions = options,
                            KeyboardActions = actions,
                            OnTextLayout = layout,
                            DecorationBox = activity.Decorated ? inner =>
                            {
                                activity.DecorationRevision = revision;
                                return new Column { new Text($"Decoration {revision}"), inner };
                            } : null,
                        };
                    if (activity._route == 1 && activity.Decorated)
                        Composables.BasicTextField(composer, activity.Input.Value, value => activity.ChangeValue(value),
                            readOnly: activity.ReadOnly, singleLine: activity.SingleLine,
                            maxLines: activity.MaxLines, minLines: activity.MinLines,
                            modifier: modifier, textStyle: style, cursorBrush: brush,
                            keyboardOptions: options, keyboardActions: actions, onTextLayout: layout,
                            decorationBox: (Action<IComposer> inner, IComposer innerComposer) =>
                            {
                                activity.DecorationRevision = revision;
                                new Column { new Text($"Decoration {revision}"),
                                    new Composed(c2 => { inner(c2); return null; }) }.Render(innerComposer);
                            });
                    else if (activity._route == 1)
                        Composables.BasicTextField(composer, activity.Input.Value, value => activity.ChangeValue(value),
                            readOnly: activity.ReadOnly, singleLine: activity.SingleLine,
                            maxLines: activity.MaxLines, minLines: activity.MinLines,
                            modifier: modifier, textStyle: style, cursorBrush: brush,
                            keyboardOptions: options, keyboardActions: actions, onTextLayout: layout);
                    else
                        ImplicitEditor(activity, modifier, style, brush, options, actions, layout, revision);
                    return null;
                }),
                new Button(activity.Send) { new Text("Send") },
            },
        }.Render(c);
        c.SideEffect(() => activity._appliedRevision = revision);
    }

    [Composable]
    internal static void ImplicitEditor(BasicTextFieldTestActivity activity, Modifier modifier,
        TextStyle style, global::AndroidX.Compose.UI.Graphics.Brush brush,
        global::AndroidX.Compose.Foundation.Text.KeyboardOptions options,
        global::AndroidX.Compose.Foundation.Text.KeyboardActions actions,
        Action<TextLayoutResult> layout, int revision)
    {
        if (!activity.Decorated)
        {
            Composables.BasicTextField(activity.Input.Value, value => activity.ChangeValue(value),
                readOnly: activity.ReadOnly, singleLine: activity.SingleLine,
                maxLines: activity.MaxLines, minLines: activity.MinLines,
                modifier: modifier, textStyle: style, cursorBrush: brush,
                keyboardOptions: options, keyboardActions: actions, onTextLayout: layout);
            return;
        }
        Composables.BasicTextField(activity.Input.Value, value => activity.ChangeValue(value),
            readOnly: activity.ReadOnly, singleLine: activity.SingleLine,
            maxLines: activity.MaxLines, minLines: activity.MinLines,
            modifier: modifier, textStyle: style, cursorBrush: brush,
            keyboardOptions: options, keyboardActions: actions, onTextLayout: layout,
            decorationBox: inner =>
            {
                activity.DecorationRevision = revision;
                Composables.Column(() =>
                {
                    Composables.Text($"Decoration {revision}");
                    inner();
                });
            });
    }

    [Composable]
    internal static void StringEditor(IComposer composer, BasicTextFieldTestActivity activity, Modifier modifier,
        TextStyle style, global::AndroidX.Compose.UI.Graphics.Brush brush,
        global::AndroidX.Compose.Foundation.Text.KeyboardOptions options,
        global::AndroidX.Compose.Foundation.Text.KeyboardActions actions,
        Action<TextLayoutResult> layout, int revision)
    {
        if (activity._route == 3)
        {
            new BasicTextField(activity.StringInput.Value, value => activity.ChangeValue(value))
            {
                Modifier = modifier,
                TextStyle = style,
                CursorBrush = brush,
                KeyboardOptions = options,
                KeyboardActions = actions,
                OnTextLayout = layout,
                DecorationBox = inner =>
                {
                    activity.DecorationRevision = revision;
                    return new Column { new Text($"String decoration {revision}"), inner };
                },
            }.Render(composer);
        }
        else if (activity._route == 4)
        {
            Composables.BasicTextField(composer, activity.StringInput.Value, value => activity.ChangeValue(value),
                modifier: modifier, textStyle: style, cursorBrush: brush,
                keyboardOptions: options, keyboardActions: actions, onTextLayout: layout,
                decorationBox: (inner, innerComposer) =>
                {
                    activity.DecorationRevision = revision;
                    new Column { new Text($"String decoration {revision}"),
                        new Composed(c => { inner(c); return null; }) }.Render(innerComposer);
                });
        }
        else if (activity._route == 7)
        {
            NativeEditor(composer, activity, modifier, style, brush, options, actions, layout, revision);
        }
        else
        {
            ImplicitStringEditor(activity, modifier, style, brush, options, actions, layout, revision);
        }
    }

    [Composable]
    internal static void ImplicitStringEditor(BasicTextFieldTestActivity activity, Modifier modifier,
        TextStyle style, global::AndroidX.Compose.UI.Graphics.Brush brush,
        global::AndroidX.Compose.Foundation.Text.KeyboardOptions options,
        global::AndroidX.Compose.Foundation.Text.KeyboardActions actions,
        Action<TextLayoutResult> layout, int revision) =>
        Composables.BasicTextField(activity.StringInput.Value, value => activity.ChangeValue(value),
            modifier: modifier, textStyle: style, cursorBrush: brush,
            keyboardOptions: options, keyboardActions: actions, onTextLayout: layout,
            decorationBox: inner =>
            {
                activity.DecorationRevision = revision;
                Composables.Column(() =>
                {
                    Composables.Text($"String decoration {revision}");
                    inner();
                });
            });

    [Composable]
    internal static void NativeEditor(IComposer composer, BasicTextFieldTestActivity activity,
        Modifier modifier, TextStyle style, global::AndroidX.Compose.UI.Graphics.Brush brush,
        global::AndroidX.Compose.Foundation.Text.KeyboardOptions options,
        global::AndroidX.Compose.Foundation.Text.KeyboardActions actions,
        Action<TextLayoutResult> layout, int revision)
    {
        var changed = composer.RememberAction(peer =>
        {
            if (activity.IsStringRoute)
                activity.ChangeValue(peer?.ToString() ?? throw new InvalidOperationException("Native string callback missing."));
            else
                activity.ChangeValue(peer?.JavaCast<TextFieldValue>()
                    ?? throw new InvalidOperationException("Native value callback missing."));
        });
        var onLayout = composer.RememberAction(peer => layout(peer?.JavaCast<TextLayoutResult>()
            ?? throw new InvalidOperationException("Native layout callback missing.")));
        var decoration = activity.Decorated ? ComposableLambdas.Wrap3WithValue(composer, (peer, c) =>
        {
            var inner = peer?.JavaCast<IFunction2>()
                ?? throw new InvalidOperationException("Native inner editor callback missing.");
            activity.DecorationRevision = revision;
            new Column
            {
                new Text(activity.IsStringRoute ? $"String decoration {revision}" : $"Decoration {revision}"),
                new Composed(innerComposer =>
                {
                    var flags = Java.Lang.Integer.ValueOf(0);
                    inner.Invoke((Java.Lang.Object)innerComposer, flags);
                    return null;
                }),
            }.Render(c);
        }) : null;
        if (activity.IsStringRoute)
        {
            var defaults = BasicTextFieldStringDefault.VisualTransformation | BasicTextFieldStringDefault.InteractionSource;
            if (decoration is null)
                defaults |= BasicTextFieldStringDefault.DecorationBox;
            global::AndroidX.Compose.Foundation.Text.BasicTextFieldKt.BasicTextField(
                activity.StringInput.Value, changed, modifier.Build(), true, activity.ReadOnly, style.Build(),
                options, actions, false, int.MaxValue, 1,
                null, onLayout, null, brush, decoration, composer, 0, 0, (int)defaults);
        }
        else
        {
            var defaults = BasicTextFieldDefault.VisualTransformation | BasicTextFieldDefault.InteractionSource;
            if (decoration is null)
                defaults |= BasicTextFieldDefault.DecorationBox;
            global::AndroidX.Compose.Foundation.Text.BasicTextFieldKt.BasicTextField(
                activity.Input.Value, changed, modifier.Build(), true, activity.ReadOnly, style.Build(),
                options, actions, activity.SingleLine, activity.MaxLines, activity.MinLines,
                null, onLayout, null, brush, decoration, composer, 0, 0, (int)defaults);
        }
    }
}
