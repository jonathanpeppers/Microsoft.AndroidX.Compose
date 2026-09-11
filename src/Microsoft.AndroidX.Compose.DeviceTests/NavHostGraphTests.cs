using System.Runtime.CompilerServices;
using AndroidX.Compose;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Verifies the fixed graph topology and managed-content ownership contracts.</summary>
[TestClass]
[DoNotParallelize]
public class NavHostGraphTests
{
    [TestMethod]
    public void ContentReplacement_KeepsBuilderAndAcceptsEitherShape()
    {
        var graph = new NavHostGraph([new NavDestination("home"), new NavDestination("detail")]);
        var builder = graph.Builder;
        graph.Publish(graph.Capture([
            new NavDestination("home", _ => new Text("New home")),
            new NavDestination("detail") { new Text("New detail") },
        ]));
        Assert.AreSame(builder, graph.Builder);
        graph.Clear();
    }

    [TestMethod]
    public void StaticChildren_AreCapturedUntilTheNextHostPublication()
    {
        var destination = new NavDestination("home") { new Text("First") };
        var first = destination.Content;
        Assert.AreSame(first, destination.Content);
        destination.Add(new Text("Second"));
        Assert.AreNotSame(first, destination.Content);
        Assert.AreSame(destination.Content, destination.Content);
    }

    [TestMethod]
    public void TopologyEdits_DoNotReplaceTheBuilder()
    {
        var graph = new NavHostGraph([new NavDestination("home"), new NavDestination("detail")]);
        var builder = graph.Builder;
        graph.Publish(graph.Capture([new NavDestination("home")]));
        graph.Publish(graph.Capture([
            new NavDestination("detail"), new NavDestination("home"),
            new NavDestination("home"), new NavDestination("renamed"),
        ]));
        Assert.AreSame(builder, graph.Builder);
        graph.Clear();
    }

    [TestMethod]
    public async Task ReplacingAndClearingContent_ReleasesCapturedObjects()
    {
        var graph = CreateGraph(out var original);
        PublishReplacement(graph, out var replacement);
        await NavContentTests.AssertCollected(original, "Initial factory capture was retained by the stable graph.");
        graph.Clear();
        await NavContentTests.AssertCollected(replacement, "Latest factory capture was retained after clearing the graph.");
        GC.KeepAlive(graph);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static NavHostGraph CreateGraph(out WeakReference reference)
    {
        var captured = new object();
        reference = new(captured);
        return new NavHostGraph([new NavDestination("home", _ =>
        {
            GC.KeepAlive(captured);
            return new Text("Initial");
        })]);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void PublishReplacement(NavHostGraph graph, out WeakReference reference)
    {
        var captured = new object();
        reference = new(captured);
        graph.Publish(graph.Capture([new NavDestination("home", _ =>
        {
            GC.KeepAlive(captured);
            return new Text("Replacement");
        })]));
    }
}
