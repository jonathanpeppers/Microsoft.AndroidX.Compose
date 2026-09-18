using AndroidX.Compose;
using AndroidX.Compose.Runtime;
using Android.Runtime;
using Microsoft.Maui.Handlers;
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
        var handler = new ComposeLayoutHandler();
        var a = Child("A");
        var b = Child("B");
        var c = Child("C");
        var x = Child("X");
        var replacement = Child("R");
        layout.Add(a);
        layout.Add(b);

        using var applier = new StateOnlyApplier();
        using var recomposer = new Recomposer(Kotlin.Coroutines.EmptyCoroutineContext.Instance
            ?? throw new InvalidOperationException("Empty coroutine context is unavailable."));
        using var snapshots = GetSnapshotCompanion();
        var composition = CompositionKt.ControlledComposition(applier, recomposer);
        var observed = new Dictionary<string, object>();
        var order = new List<string>();
        var disposals = new Dictionary<string, int>();
        var unrelatedState = new MutableState<int>(0);
        var unrelatedStatePeer = (Java.Lang.Object)unrelatedState._state;
        using var content = new ComposableLambda2(composer =>
        {
            var container = new MovableTestContainer();
            foreach (var child in handler.SnapshotChildren(layout))
            {
                var label = child as Microsoft.Maui.Controls.Label
                    ?? throw new InvalidOperationException("Layout mutation test child is not a Label.");
                container.AddMovable(
                    handler.GetChildIdentity(child),
                    new MovableStateProbeNode(label.Text, observed, order, disposals));
            }
            container.Render(composer);
        });

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
            layout.Add(c);
            Invoke(handler, layout, composition, "Add", new LayoutHandlerUpdate(2, c));
            Recompose(composition, snapshots, observed, order);
            AssertOrder(order, "A", "B", "C");
            Assert.AreSame(aState, observed["A"]);
            Assert.AreSame(bState, observed["B"]);
            var cState = observed["C"];

            AssertQuiescent(composition, snapshots);
            layout.Insert(1, x);
            Invoke(handler, layout, composition, "Insert", new LayoutHandlerUpdate(1, x));
            Recompose(composition, snapshots, observed, order);
            AssertOrder(order, "A", "X", "B", "C");
            Assert.AreSame(aState, observed["A"]);
            Assert.AreSame(bState, observed["B"]);
            Assert.AreSame(cState, observed["C"]);
            var xState = observed["X"];

            AssertQuiescent(composition, snapshots);
            layout.Remove(b);
            Invoke(handler, layout, composition, "Remove", new LayoutHandlerUpdate(2, b));
            Recompose(composition, snapshots, observed, order);
            AssertOrder(order, "A", "X", "C");
            Assert.AreSame(aState, observed["A"]);
            Assert.AreSame(xState, observed["X"]);
            Assert.AreSame(cState, observed["C"]);
            Assert.AreEqual(1, disposals["B"]);

            AssertQuiescent(composition, snapshots);
            layout[1] = replacement;
            Invoke(handler, layout, composition, "Update", new LayoutHandlerUpdate(1, replacement));
            Recompose(composition, snapshots, observed, order);
            AssertOrder(order, "A", "R", "C");
            Assert.AreSame(aState, observed["A"]);
            Assert.AreSame(cState, observed["C"]);
            Assert.AreNotSame(xState, observed["R"]);
            Assert.AreEqual(1, disposals["X"]);

            AssertQuiescent(composition, snapshots);
            layout.Clear();
            Invoke(handler, layout, composition, "Clear", null);
            Recompose(composition, snapshots, observed, order);
            Assert.AreEqual(0, order.Count);
            Assert.AreEqual(1, disposals["A"]);
            Assert.AreEqual(1, disposals["C"]);
            Assert.AreEqual(1, disposals["R"]);
        }
        finally
        {
            composition.Dispose();
        }
    }

    static Microsoft.Maui.Controls.Label Child(string id) => new() { Text = id };

    static void Invoke(
        ComposeLayoutHandler handler,
        MauiLayout layout,
        IControlledComposition composition,
        string command,
        object? args)
    {
        var action = ComposeLayoutHandler.CommandMapper.GetCommand(command)
            ?? throw new InvalidOperationException($"Layout command '{command}' is not registered.");
        int previousVersion = handler.ChildrenVersion;
        action(handler, layout, args);
        Assert.AreEqual(
            previousVersion + 1,
            handler.ChildrenVersion,
            $"Layout command '{command}' did not increment the subscribed children version.");
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
