using AndroidX.Compose;
using AndroidX.Compose.Runtime;
using Microsoft.Maui.Handlers;
using ComposeLayoutHandler = Microsoft.AndroidX.Compose.Maui.Handlers.LayoutHandler;
using MauiLayout = Microsoft.Maui.Controls.Layout;

namespace Microsoft.AndroidX.Compose.Maui.DeviceTests;

/// <summary>Exercises MAUI stack child commands against an already composed child sequence.</summary>
[TestClass]
[DoNotParallelize]
public class LayoutHandlerMutationTests
{
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void ChildMutations_RecomposeInOrderAndPreserveSurvivorState(bool vertical)
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
        var composition = CompositionKt.ControlledComposition(applier, recomposer);
        var observed = new Dictionary<string, object>();
        var order = new List<string>();
        var disposals = new Dictionary<string, int>();
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
            composition.ComposeContent(content);
            composition.ApplyChanges();
            AssertOrder(order, "A", "B");
            var aState = observed["A"];
            var bState = observed["B"];

            layout.Add(c);
            Invoke(handler, layout, "Add", new LayoutHandlerUpdate(2, c));
            Recompose(composition, observed, order);
            AssertOrder(order, "A", "B", "C");
            Assert.AreSame(aState, observed["A"]);
            Assert.AreSame(bState, observed["B"]);
            var cState = observed["C"];

            layout.Insert(1, x);
            Invoke(handler, layout, "Insert", new LayoutHandlerUpdate(1, x));
            Recompose(composition, observed, order);
            AssertOrder(order, "A", "X", "B", "C");
            Assert.AreSame(aState, observed["A"]);
            Assert.AreSame(bState, observed["B"]);
            Assert.AreSame(cState, observed["C"]);
            var xState = observed["X"];

            layout.Remove(b);
            Invoke(handler, layout, "Remove", new LayoutHandlerUpdate(2, b));
            Recompose(composition, observed, order);
            AssertOrder(order, "A", "X", "C");
            Assert.AreSame(aState, observed["A"]);
            Assert.AreSame(xState, observed["X"]);
            Assert.AreSame(cState, observed["C"]);
            Assert.AreEqual(1, disposals["B"]);

            layout[1] = replacement;
            Invoke(handler, layout, "Update", new LayoutHandlerUpdate(1, replacement));
            Recompose(composition, observed, order);
            AssertOrder(order, "A", "R", "C");
            Assert.AreSame(aState, observed["A"]);
            Assert.AreSame(cState, observed["C"]);
            Assert.AreNotSame(xState, observed["R"]);
            Assert.AreEqual(1, disposals["X"]);

            layout.Clear();
            Invoke(handler, layout, "Clear", null);
            Recompose(composition, observed, order);
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
        string command,
        object? args)
    {
        var action = ComposeLayoutHandler.CommandMapper.GetCommand(command)
            ?? throw new InvalidOperationException($"Layout command '{command}' is not registered.");
        action(handler, layout, args);
    }

    static void Recompose(
        IControlledComposition composition,
        IDictionary<string, object> observed,
        IList<string> order)
    {
        Assert.IsTrue(composition.HasInvalidations, "The layout command did not invalidate its composed child snapshot.");
        observed.Clear();
        order.Clear();
        Assert.IsTrue(composition.Recompose(), "The invalidated layout did not recompose.");
        composition.ApplyChanges();
    }

    static void AssertOrder(IList<string> actual, params string[] expected) =>
        CollectionAssert.AreEqual(expected, actual.ToArray());
}
