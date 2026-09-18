using AndroidX.Compose;
using AndroidX.Compose.Runtime;
using Android.Runtime;
using ComposeLayoutHandler = Microsoft.AndroidX.Compose.Maui.Handlers.LayoutHandler;
using MauiLayout = Microsoft.Maui.Controls.Layout;
using Snapshot = AndroidX.Compose.Runtime.Snapshots.Snapshot;

namespace Microsoft.AndroidX.Compose.Maui.DeviceTests;

/// <summary>Exercises MAUI stack child commands against an already composed child sequence.</summary>
[TestClass]
[DoNotParallelize]
public class LayoutHandlerMutationTests
{
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void ChildMutations_RecomposeInOrderAndPreserveSurvivorState(bool vertical) =>
        Microsoft.Maui.ApplicationModel.MainThread
            .InvokeOnMainThreadAsync(() => RunChildMutations(vertical))
            .GetAwaiter()
            .GetResult();

    static void RunChildMutations(bool vertical)
    {
        var layout = vertical
            ? (MauiLayout)new Microsoft.Maui.Controls.VerticalStackLayout()
            : new Microsoft.Maui.Controls.HorizontalStackLayout();
        var observed = new Dictionary<string, object>();
        var order = new List<string>();
        var disposals = new Dictionary<string, int>();
        var androidContext = global::Android.App.Application.Context
            ?? throw new InvalidOperationException("Android application context is unavailable.");
        var a = Child(androidContext, "A", observed, order, disposals);
        var b = Child(androidContext, "B", observed, order, disposals);
        var c = Child(androidContext, "C", observed, order, disposals);
        var x = Child(androidContext, "X", observed, order, disposals);
        var replacement = Child(androidContext, "R", observed, order, disposals);
        Microsoft.Maui.Controls.Label[] children = [a, b, c, x, replacement];
        layout.Add(a);
        layout.Add(b);

        using var services = new ServiceCollection().BuildServiceProvider();
        var handler = new ComposeLayoutHandler();
        handler.SetMauiContext(new MauiContext(services, androidContext));
        layout.Handler = handler;

        using var applier = new LayoutTestApplier();
        using var recomposer = new Recomposer(Kotlin.Coroutines.EmptyCoroutineContext.Instance
            ?? throw new InvalidOperationException("Empty coroutine context is unavailable."));
        using var snapshots = GetSnapshotCompanion();
        var composition = CompositionKt.ControlledComposition(applier, recomposer);
        var unrelatedState = new MutableState<int>(0);
        var unrelatedStatePeer = (Java.Lang.Object)unrelatedState._state;
        using var content = new ComposableLambda2(composer => RenderLayout(handler, vertical, composer));

        try
        {
            ObserveComposition(snapshots, composition, () => composition.ComposeContent(content));
            composition.ApplyChanges();
            AssertOrder(order, "A", "B");
            Assert.IsTrue(
                composition.ObservesAnyOf([handler.ChildrenVersionState]),
                "The layout composition did not observe its children-version state.");
            Assert.IsFalse(
                composition.ObservesAnyOf([unrelatedStatePeer]),
                "The layout composition unexpectedly observed the unrelated control state.");
            composition.RecordModificationsOf([unrelatedStatePeer]);
            Assert.IsFalse(
                composition.HasInvalidations,
                "Reporting an unobserved state modification invalidated the layout composition.");
            var aState = observed["A"];
            var bState = observed["B"];

            AssertQuiescent(composition, snapshots);
            Mutate(handler, composition, "Add", () => layout.Add(c));
            Recompose(composition, snapshots, observed, order);
            AssertOrder(order, "A", "B", "C");
            Assert.AreSame(aState, observed["A"]);
            Assert.AreSame(bState, observed["B"]);
            var cState = observed["C"];

            AssertQuiescent(composition, snapshots);
            Mutate(handler, composition, "Insert", () => layout.Insert(1, x));
            Recompose(composition, snapshots, observed, order);
            AssertOrder(order, "A", "X", "B", "C");
            Assert.AreSame(aState, observed["A"]);
            Assert.AreSame(bState, observed["B"]);
            Assert.AreSame(cState, observed["C"]);
            var xState = observed["X"];

            AssertQuiescent(composition, snapshots);
            Mutate(handler, composition, "Remove", () => layout.Remove(b));
            Recompose(composition, snapshots, observed, order);
            AssertOrder(order, "A", "X", "C");
            Assert.AreSame(aState, observed["A"]);
            Assert.AreSame(xState, observed["X"]);
            Assert.AreSame(cState, observed["C"]);
            Assert.AreEqual(1, disposals["B"]);

            AssertQuiescent(composition, snapshots);
            Mutate(handler, composition, "Update", () => layout[1] = replacement);
            Recompose(composition, snapshots, observed, order);
            AssertOrder(order, "A", "R", "C");
            Assert.AreSame(aState, observed["A"]);
            Assert.AreSame(cState, observed["C"]);
            Assert.AreNotSame(xState, observed["R"]);
            Assert.AreEqual(1, disposals["X"]);

            AssertQuiescent(composition, snapshots);
            Mutate(handler, composition, "Clear", layout.Clear);
            Recompose(composition, snapshots, observed, order);
            Assert.AreEqual(0, order.Count);
            Assert.AreEqual(1, disposals["A"]);
            Assert.AreEqual(1, disposals["C"]);
            Assert.AreEqual(1, disposals["R"]);
        }
        finally
        {
            composition.Dispose();
            ((IElementHandler)handler).DisconnectHandler();
            foreach (var child in children)
                child.Handler?.DisconnectHandler();
        }
    }

    static Microsoft.Maui.Controls.Label Child(
        global::Android.Content.Context context,
        string id,
        IDictionary<string, object> observed,
        IList<string> order,
        IDictionary<string, int> disposals)
    {
        var child = new Microsoft.Maui.Controls.Label { Text = id };
        child.Handler = new MovableStateProbeHandler(context, id, observed, order, disposals);
        return child;
    }

    static void RenderLayout(ComposeLayoutHandler handler, bool vertical, IComposer composer)
    {
        var node = handler.BuildNode(composer);
        bool expectedType = vertical ? node is Column : node is Row;
        Assert.IsTrue(expectedType, $"LayoutHandler returned '{node.GetType().Name}' for a {(vertical ? "vertical" : "horizontal")} stack.");
        node.Render(composer);
    }

    static void Mutate(
        ComposeLayoutHandler handler,
        IControlledComposition composition,
        string command,
        Action mutation)
    {
        int previousVersion = handler.ChildrenVersion;
        mutation();
        Assert.AreEqual(
            previousVersion + 1,
            handler.ChildrenVersion,
            $"Layout mutation '{command}' did not dispatch through the handler command mapper.");
        composition.RecordModificationsOf([handler.ChildrenVersionState]);
    }

    static void AssertQuiescent(
        IControlledComposition composition,
        Snapshot.Companion snapshots)
    {
        snapshots.SendApplyNotifications();
        Assert.IsFalse(
            snapshots.IsApplyObserverNotificationPending,
            "An unrelated snapshot-state notification was pending before the layout mutation.");
        Assert.IsFalse(
            composition.HasInvalidations,
            "The composition was already invalidated before the layout mutation.");
    }

    static void Recompose(
        IControlledComposition composition,
        Snapshot.Companion snapshots,
        IDictionary<string, object> observed,
        IList<string> order)
    {
        snapshots.SendApplyNotifications();
        Assert.IsTrue(composition.HasInvalidations, "The layout command did not invalidate its composed child snapshot.");
        observed.Clear();
        order.Clear();
        bool recomposed = false;
        ObserveComposition(snapshots, composition, () => recomposed = composition.Recompose());
        Assert.IsTrue(recomposed, "The invalidated layout did not recompose.");
        composition.ApplyChanges();
    }

    static void ObserveComposition(
        Snapshot.Companion snapshots,
        IControlledComposition composition,
        Action action)
    {
        using var readObserver = new ComposableLambda1(value =>
            composition.RecordReadOf(value
                ?? throw new InvalidOperationException("Snapshot read observer received a null state.")));
        using var writeObserver = new ComposableLambda1(value =>
            composition.RecordWriteOf(value
                ?? throw new InvalidOperationException("Snapshot write observer received a null state.")));
        using var block = new ComposableLambda0(action);
        _ = snapshots.Observe(readObserver, writeObserver, block);
    }

    static void AssertOrder(IList<string> actual, params string[] expected) =>
        CollectionAssert.AreEqual(expected, actual.ToArray());

    static Snapshot.Companion GetSnapshotCompanion()
    {
        using var snapshotClass = Java.Lang.Class.FromType(typeof(Snapshot));
        using var field = snapshotClass.GetField("Companion")
            ?? throw new InvalidOperationException("Snapshot.Companion field is unavailable.");
        var singleton = field.Get(null)
            ?? throw new InvalidOperationException("Snapshot.Companion singleton is unavailable.");
        return singleton.JavaCast<Snapshot.Companion>()
            ?? throw new InvalidOperationException("Snapshot.Companion cannot be projected.");
    }
}
