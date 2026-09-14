using Android.Runtime;
using AndroidX.Activity;
using AndroidX.Compose;
using AndroidX.Compose.Runtime;
using Kotlin.Jvm.Functions;
using Composable = AndroidX.Compose.ComposableAttribute;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Renders static masks, tracked content updates, and generated facades in a live composition.</summary>
[Activity(Theme = "@android:style/Theme.Material.Light.NoActionBar")]
[Register("net/compose/devicetests/ChangedBitsTestActivity")]
public class ChangedBitsTestActivity : ComponentActivity
{
    internal static ChangedBitsTestActivity? Current { get; private set; }
    internal MutableNumberState<int> Revision { get; } = new(0);
    internal MutableNumberState<int> ForceRevision { get; } = new(0);
    internal int CompletedForceRevision = -1;
    internal int ForcedExecutions;
    internal int CompletedRevision = -1;
    internal int ContentRevision = -1;
    internal int GeneratedContentRevision = -1;
    internal int DirectContentRevision = -1;
    internal int CallbackRevision = -1;
    internal int TypedCallbackRevision = -1;
    internal int StableExecutions;
    internal int ChangingExecutions;
    internal int BoundaryExecutions;
    internal int BoundarySkips;
    internal IFunction0? RetainedCallback;
    internal IFunction1? RetainedTypedCallback;
    internal IFunction2? InitialContent;
    internal bool StablePeers = true;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        this.SetContent((IComposer c) => Render(c));
        Current = this;
    }

    void Render(IComposer composer)
    {
        int revision = Revision.Value;
        var click = composer.RememberAction(() => CallbackRevision = revision);
        var typed = composer.RememberAction((Java.Lang.Object? value) =>
            TypedCallbackRevision = revision + ((Java.Lang.Integer)(value
                ?? throw new InvalidOperationException("Typed callback value is missing."))).IntValue());
        var content = ComposableLambdas.Wrap2(composer, c =>
        {
            new Text($"Tracked content {revision}").Render(c);
            c.SideEffect(() => ContentRevision = revision);
        });
        if (InitialContent is not null)
            StablePeers &= JNIEnv.IsSameObject(((Java.Lang.Object)InitialContent).Handle,
                ((Java.Lang.Object)content).Handle);
        InitialContent ??= content;
        if (RetainedCallback is not null && RetainedTypedCallback is not null)
        {
            StablePeers &= JNIEnv.IsSameObject(((Java.Lang.Object)RetainedCallback).Handle,
                ((Java.Lang.Object)click).Handle);
            StablePeers &= JNIEnv.IsSameObject(((Java.Lang.Object)RetainedTypedCallback).Handle,
                ((Java.Lang.Object)typed).Handle);
        }
        StaticBoundary(composer, click, typed, content, 0);

        StableSibling(composer, this, "fixed");
        ChangingSibling(composer, this, revision);
        ForcedSibling(composer, this);
        new global::AndroidX.Compose.Button(() => CallbackRevision = revision)
        {
            new Composed(c =>
            {
                c.SideEffect(() => GeneratedContentRevision = revision);
                return new Text($"Generated content {revision}");
            }),
        }.Render(composer);
        DirectContent(composer, this, revision);
        composer.SideEffect(() => Volatile.Write(ref CompletedRevision, revision));
    }

    void StaticBoundary(IComposer composer, IFunction0 click, IFunction1 typed, IFunction2 content, int force)
    {
        var c = composer.StartRestartGroup(35601);
        int dirty = force | ((int)ChangedBits.Static << 1) |
            ((int)ChangedBits.Static << 4) | ((int)ChangedBits.Static << 7);
        // The Kotlin three-parameter predicate, intentionally independent of
        // ChangedBits. SkipToGroupEnd must still visit invalidated descendants.
        if ((dirty & 0x2DB) != 0x92 || !c.Skipping)
        {
            BoundaryExecutions++;
            RetainedCallback = click;
            RetainedTypedCallback = typed;
            using var changed = Java.Lang.Integer.ValueOf(0);
            content.Invoke((Java.Lang.Object)c, changed);
        }
        else
        {
            BoundarySkips++;
            c.SkipToGroupEnd();
        }
        c.EndRestartGroup()?.UpdateScope(new ComposableLambda2(
            (next, changed) => StaticBoundary(next, click, typed, content, changed | 1)));
    }

    [Composable]
    internal static void StableSibling(IComposer c, ChangedBitsTestActivity activity, string text)
    {
        activity.StableExecutions++;
        new Text(text).Render(c);
    }

    [Composable]
    internal static void ChangingSibling(IComposer c, ChangedBitsTestActivity activity, int revision)
    {
        activity.ChangingExecutions++;
        new Text($"Changing input {revision}").Render(c);
    }

    [Composable]
    internal static void ForcedSibling(IComposer c, ChangedBitsTestActivity activity)
    {
        int revision = activity.ForceRevision.Value;
        activity.ForcedExecutions++;
        new Text($"Forced input {revision}").Render(c);
        c.SideEffect(() => Volatile.Write(ref activity.CompletedForceRevision, revision));
    }

    [Composable]
    internal static void DirectContent(IComposer c, ChangedBitsTestActivity activity, int revision) =>
        Composables.Button(() => activity.CallbackRevision = revision, () =>
        {
            Composables.Text($"Direct content {revision}");
            Composables.SideEffect(() => activity.DirectContentRevision = revision);
        });

    protected override void OnDestroy()
    {
        if (ReferenceEquals(Current, this))
            Current = null;
        base.OnDestroy();
    }
}
