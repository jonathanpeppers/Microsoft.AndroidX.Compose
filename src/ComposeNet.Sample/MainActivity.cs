using Android.OS;
using Android.Runtime;
using AndroidX.Activity;
using Androidx.Compose.Foundation;
using Androidx.Compose.Foundation.Layout;
using Androidx.Compose.Runtime;
using Androidx.Compose.Runtime.Internal;
using Androidx.Compose.UI;
using Androidx.Compose.UI.Platform;
using Kotlin.Jvm.Functions;

namespace ComposeNet.Sample;

[Activity(Label = "@string/app_name", MainLauncher = true)]
public class MainActivity : ComponentActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Persist counter state across recompositions by holding it on the host:
        // we don't have a C#-callable `remember`, so the activity IS the remember slot.
        var count = SnapshotStateKt.MutableStateOf(
            Java.Lang.Integer.ValueOf(0),
            SnapshotStateKt.StructuralEqualityPolicy());

        var composeView = new ComposeView(this);
        composeView.SetContent(ComposableLambdaKt.ComposableLambdaInstance(
            key: -1,
            tracked: false,
            block: new AppContent(count)));
        SetContentView(composeView);
    }
}

[Register("composenet/sample/AppContent")]
public sealed class AppContent : Java.Lang.Object, IFunction2
{
    readonly IMutableState _count;
    readonly ColumnContent _content;

    public AppContent(IMutableState count)
    {
        _count = count;
        _content = new ColumnContent(_count);
    }

    public Java.Lang.Object? Invoke(Java.Lang.Object? p0, Java.Lang.Object? p1)
    {
        var composer = Android.Runtime.Extensions.JavaCast<IComposer>(p0!);

        // Column(Modifier, Arrangement.Vertical, Alignment.Horizontal, content, composer, $changed, $default)
        // $default = 0b1110 means: use defaults for modifier (bit 1), verticalArrangement (bit 2),
        // horizontalAlignment (bit 3); content (bit 0) is provided.
        ColumnKt.Column(
            modifier: null,
            verticalArrangement: null,
            horizontalAlignment: null,
            content: _content,
            _composer: composer,
            p5: 0,
            _changed: 0b1110);
        return null;
    }
}

[Register("composenet/sample/ColumnContent")]
public sealed class ColumnContent : Java.Lang.Object, IFunction3
{
    readonly IMutableState _count;
    readonly ClickHandler _click;

    public ColumnContent(IMutableState count)
    {
        _count = count;
        _click = new ClickHandler(count);
    }

    public Java.Lang.Object? Invoke(Java.Lang.Object? p0, Java.Lang.Object? p1, Java.Lang.Object? p2)
    {
        // p0 = ColumnScope receiver (unused for now)
        var composer = Android.Runtime.Extensions.JavaCast<IComposer>(p1!);

        // BasicText("Hello from .NET")
        ComposeApi.BasicText("Hello from .NET", modifier: null, composer);

        // BasicText("Count: N") — re-reads MutableState.value, so Compose
        // recomposes this lambda when the count changes.
        int n = ((Java.Lang.Integer)_count.Value!).IntValue();
        ComposeApi.BasicText("Count: " + n, modifier: null, composer);

        // BasicText("Tap to increment") wrapped in a clickable Modifier.
        var clickable = ClickableKt.Clickable(
            obj: ComposeApi.ModifierCompanion,
            enabled: true,
            onClickLabel: null,
            role: null,
            interactionSource: null,
            onClick: _click);
        ComposeApi.BasicText("Tap to increment", modifier: clickable, composer);

        return null;
    }
}

[Register("composenet/sample/ClickHandler")]
public sealed class ClickHandler : Java.Lang.Object, IFunction0
{
    readonly IMutableState _count;
    public ClickHandler(IMutableState count) => _count = count;

    public Java.Lang.Object? Invoke()
    {
        int current = ((Java.Lang.Integer)_count.Value!).IntValue();
        _count.Value = Java.Lang.Integer.ValueOf(current + 1);
        return null;
    }
}
