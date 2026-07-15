using AndroidX.Compose.Gallery.Registry;
using Android.Util;
using static AndroidX.Compose.Composables;

namespace AndroidX.Compose.Gallery.Demos.Tier2;

/// <summary>
/// Compares an unchanged Reply-style card rendered through Tier 2 with
/// equivalent tree-style construction during forced recompositions.
/// </summary>
public static class Tier2RealAppBenchmarkDemo
{
    const int AutomaticRecompositions = 10;
    const string LogTag = "Tier2Benchmark";

    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "tier2-real-app-benchmark",
        CategoryId:  "tier2",
        Title:       "Real-app allocation benchmark",
        Description: "Forces recomposition of equivalent Reply-style cards and reports Tier 2 skip versus tree allocation costs.",
        Build:       static _ => new Tier2Adapter(() => Benchmark()));

    /// <summary>Runs and renders the interactive allocation comparison.</summary>
    [Composable]
    public static void Benchmark()
    {
        var tick = Remember(() => new MutableNumberState<int>(0));
        var executions = Remember<int[]>(() => [0, 0]);
        var allocatedBytes = Remember<long[]>(() => [0L, 0L]);
        var completionLogged = Remember<bool[]>(() => [false]);

        Column(() =>
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            DirectReplyCard("A practical guide to Compose", executions);
            allocatedBytes[0] = GC.GetAllocatedBytesForCurrentThread() - before;

            before = GC.GetAllocatedBytesForCurrentThread();
            TreeReplyCard("A practical guide to Compose", executions);
            allocatedBytes[1] = GC.GetAllocatedBytesForCurrentThread() - before;

            Text($"Forced recompositions: {tick.Value}");
            Text($"Tier 2: {allocatedBytes[0]:N0} B, body executions: {executions[0]}");
            Text($"Tree: {allocatedBytes[1]:N0} B, body executions: {executions[1]}");
            if (tick.Value >= AutomaticRecompositions)
            {
                Button(
                    () => tick.Value++,
                    () => Text("Recompose unchanged cards"));
            }
            else
            {
                Text("Running automatic benchmark...");
            }
        });

        LaunchedEffect(nameof(Tier2RealAppBenchmarkDemo), async cancellationToken =>
        {
            for (int i = 0; i < AutomaticRecompositions; i++)
            {
                await Task.Delay(100, cancellationToken);
                tick.Value++;
            }
        });

        int completedRecompositions = tick.Value;
        SideEffect(() =>
        {
            if (completedRecompositions >= AutomaticRecompositions &&
                !completionLogged[0])
            {
                completionLogged[0] = true;
                Log.Info(
                    LogTag,
                    $"recompositions={completedRecompositions} " +
                    $"tier2Bytes={allocatedBytes[0]} tier2Executions={executions[0]} " +
                    $"treeBytes={allocatedBytes[1]} treeExecutions={executions[1]}");
            }
        });
    }

    [Composable]
    internal static void DirectReplyCard(string subject, int[] executions)
    {
        executions[0]++;
        Card(() => Text(subject));
    }

    static void TreeReplyCard(string subject, int[] executions)
    {
        executions[1]++;
        new Card
        {
            new Text(subject),
        }.Render(ComposableContext.Current);
    }
}
