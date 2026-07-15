using AndroidX.Compose.Gallery.Registry;
using Android.Util;
using System.Diagnostics;
using static AndroidX.Compose.Composables;

namespace AndroidX.Compose.Gallery.Demos.Tier2;

/// <summary>
/// Compares an unchanged Reply-style card rendered through the current
/// adapter-based Tier 2 path with equivalent tree-style construction.
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
        Description: "Reports initial and recomposition costs for adapter-based Tier 2 versus tree construction; direct lowering awaits #302.",
        Build:       static _ => new Tier2Adapter(() => Benchmark()));

    /// <summary>Runs and renders the interactive allocation comparison.</summary>
    [Composable]
    public static void Benchmark()
    {
        var tick = Remember(() => new MutableNumberState<int>(0));
        var executions = Remember<int[]>(() => [0, 0]);
        var initialBytes = Remember<long[]>(() => [-1L, -1L]);
        var recompositionBytes = Remember<long[]>(() => [0L, 0L]);
        var initialNanoseconds = Remember<long[]>(() => [0L, 0L]);
        var recompositionNanoseconds = Remember<long[]>(() => [0L, 0L]);
        var completionLogged = Remember<bool[]>(() => [false]);
        bool adapterFirst = Remember(() => Random.Shared.Next(2) == 0);

        Column(() =>
        {
            if (adapterFirst)
            {
                long beforeBytes = GC.GetAllocatedBytesForCurrentThread();
                long started = Stopwatch.GetTimestamp();
                AdapterTier2ReplyCard("A practical guide to Compose", executions);
                RecordMeasurement(
                    lane: 0,
                    GC.GetAllocatedBytesForCurrentThread() - beforeBytes,
                    (long)Stopwatch.GetElapsedTime(started).TotalNanoseconds,
                    initialBytes,
                    recompositionBytes,
                    initialNanoseconds,
                    recompositionNanoseconds);

                beforeBytes = GC.GetAllocatedBytesForCurrentThread();
                started = Stopwatch.GetTimestamp();
                TreeReplyCard("A practical guide to Compose", executions);
                RecordMeasurement(
                    lane: 1,
                    GC.GetAllocatedBytesForCurrentThread() - beforeBytes,
                    (long)Stopwatch.GetElapsedTime(started).TotalNanoseconds,
                    initialBytes,
                    recompositionBytes,
                    initialNanoseconds,
                    recompositionNanoseconds);
            }
            else
            {
                long beforeBytes = GC.GetAllocatedBytesForCurrentThread();
                long started = Stopwatch.GetTimestamp();
                TreeReplyCard("A practical guide to Compose", executions);
                RecordMeasurement(
                    lane: 1,
                    GC.GetAllocatedBytesForCurrentThread() - beforeBytes,
                    (long)Stopwatch.GetElapsedTime(started).TotalNanoseconds,
                    initialBytes,
                    recompositionBytes,
                    initialNanoseconds,
                    recompositionNanoseconds);

                beforeBytes = GC.GetAllocatedBytesForCurrentThread();
                started = Stopwatch.GetTimestamp();
                AdapterTier2ReplyCard("A practical guide to Compose", executions);
                RecordMeasurement(
                    lane: 0,
                    GC.GetAllocatedBytesForCurrentThread() - beforeBytes,
                    (long)Stopwatch.GetElapsedTime(started).TotalNanoseconds,
                    initialBytes,
                    recompositionBytes,
                    initialNanoseconds,
                    recompositionNanoseconds);
            }

            Text($"Forced recompositions: {tick.Value}");
            Text($"Cold-start order: {(adapterFirst ? "adapter then tree" : "tree then adapter")}");
            Text($"Adapter Tier 2 initial: {initialBytes[0]:N0} B / {initialNanoseconds[0] / 1_000d:N1} us");
            Text($"Adapter Tier 2 recompose: {recompositionBytes[0]:N0} B / {recompositionNanoseconds[0] / 1_000d:N1} us");
            Text($"Tree initial: {initialBytes[1]:N0} B / {initialNanoseconds[1] / 1_000d:N1} us");
            Text($"Tree recompose: {recompositionBytes[1]:N0} B / {recompositionNanoseconds[1] / 1_000d:N1} us");
            Text($"Body executions: adapter={executions[0]}, tree={executions[1]}");
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
                    $"order={(adapterFirst ? "adapter-tree" : "tree-adapter")} " +
                    $"adapterInitialBytes={initialBytes[0]} adapterInitialNs={initialNanoseconds[0]} " +
                    $"adapterRecomposeBytes={recompositionBytes[0]} adapterRecomposeNs={recompositionNanoseconds[0]} " +
                    $"adapterExecutions={executions[0]} " +
                    $"treeInitialBytes={initialBytes[1]} treeInitialNs={initialNanoseconds[1]} " +
                    $"treeRecomposeBytes={recompositionBytes[1]} treeRecomposeNs={recompositionNanoseconds[1]} " +
                    $"treeExecutions={executions[1]}");
            }
        });
    }

    static void RecordMeasurement(
        int lane,
        long allocatedBytes,
        long elapsedNanoseconds,
        long[] initialBytes,
        long[] recompositionBytes,
        long[] initialNanoseconds,
        long[] recompositionNanoseconds)
    {
        if (initialBytes[lane] < 0)
        {
            initialBytes[lane] = allocatedBytes;
            initialNanoseconds[lane] = elapsedNanoseconds;
        }
        else
        {
            recompositionBytes[lane] = allocatedBytes;
            recompositionNanoseconds[lane] = elapsedNanoseconds;
        }
    }

    [Composable]
    internal static void AdapterTier2ReplyCard(string subject, int[] executions)
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
