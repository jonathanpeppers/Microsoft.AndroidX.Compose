using AndroidX.Compose.Gallery.Registry;
using Android.Util;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using static AndroidX.Compose.Composables;

namespace AndroidX.Compose.Gallery.Demos.ComposableMethods;

/// <summary>
/// Compares equivalent Reply-style cards rendered through tree construction,
/// adapter-based composable rendering, and direct-lowered composable rendering.
/// </summary>
public static class ComposableMethodBenchmarkDemo
{
    const int AutomaticRecompositions = 10;
    const int TreeLane = 0;
    const int AdapterLane = 1;
    const int DirectLane = 2;
    const string LogTag = "ComposableMethodBenchmark";
    static int s_directContentExecutions;

    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "composable-real-app-benchmark",
        CategoryId:  "composable-methods",
        Title:       "Real-app allocation benchmark",
        Description: "Reports initial and recomposition costs for tree, adapter, and direct-lowered rendering.",
        Build:       static _ => new ComposableDemoAdapter(() => Benchmark()));

    /// <summary>Runs and renders the interactive allocation comparison.</summary>
    [Composable]
    public static void Benchmark()
    {
        var tick = Remember(() => new MutableNumberState<int>(0));
        var executions = Remember(() => new StrongBox<int[]>([0, 0]));
        var initialBytes = Remember<long[]>(() => [-1L, -1L, -1L]);
        var recompositionBytes = Remember<long[]>(() => [0L, 0L, 0L]);
        var initialNanoseconds = Remember<long[]>(() => [0L, 0L, 0L]);
        var recompositionNanoseconds = Remember<long[]>(() => [0L, 0L, 0L]);
        var completionLogged = Remember<bool[]>(() => [false]);
        var laneOrder = Remember(CreateRandomLaneOrder);
        int directExecutionBaseline = Remember(
            () => Volatile.Read(ref s_directContentExecutions));

        Column(() =>
        {
            var executionValues = ExecutionValues(executions);
            foreach (int lane in laneOrder)
            {
                switch (lane)
                {
                    case TreeLane:
                        MeasureTree(
                            tick.Value,
                            executions,
                            initialBytes,
                            recompositionBytes,
                            initialNanoseconds,
                            recompositionNanoseconds);
                        break;
                    case AdapterLane:
                        MeasureAdapterComposableMethod(
                            tick.Value,
                            executions,
                            initialBytes,
                            recompositionBytes,
                            initialNanoseconds,
                            recompositionNanoseconds);
                        break;
                    default:
                        MeasureDirectComposableMethod(
                            tick.Value,
                            initialBytes,
                            recompositionBytes,
                            initialNanoseconds,
                            recompositionNanoseconds);
                        break;
                }
            }

            Text($"Forced recompositions: {tick.Value}");
            Text($"Cold-start order: {FormatLaneOrder(laneOrder)}");
            Text($"Tree initial: {initialBytes[0]:N0} B / {initialNanoseconds[0] / 1_000d:N1} us");
            Text($"Tree recompose: {recompositionBytes[0]:N0} B / {recompositionNanoseconds[0] / 1_000d:N1} us");
            Text($"Adapter initial: {initialBytes[1]:N0} B / {initialNanoseconds[1] / 1_000d:N1} us");
            Text($"Adapter recompose: {recompositionBytes[1]:N0} B / {recompositionNanoseconds[1] / 1_000d:N1} us");
            Text($"Direct initial: {initialBytes[2]:N0} B / {initialNanoseconds[2] / 1_000d:N1} us");
            Text($"Direct recompose: {recompositionBytes[2]:N0} B / {recompositionNanoseconds[2] / 1_000d:N1} us");
            Text(
                $"Executions: tree={executionValues[0]}, adapter={executionValues[1]}, " +
                $"direct={Volatile.Read(ref s_directContentExecutions) - directExecutionBaseline}");
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

        LaunchedEffect(nameof(ComposableMethodBenchmarkDemo), async cancellationToken =>
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
                var executionValues = ExecutionValues(executions);
                Log.Info(
                    LogTag,
                    $"recompositions={completedRecompositions} " +
                    $"order={string.Join("-", laneOrder)} " +
                    $"treeInitialBytes={initialBytes[0]} treeInitialNs={initialNanoseconds[0]} " +
                    $"treeRecomposeBytes={recompositionBytes[0]} treeRecomposeNs={recompositionNanoseconds[0]} " +
                    $"treeExecutions={executionValues[0]} " +
                    $"adapterInitialBytes={initialBytes[1]} adapterInitialNs={initialNanoseconds[1]} " +
                    $"adapterRecomposeBytes={recompositionBytes[1]} adapterRecomposeNs={recompositionNanoseconds[1]} " +
                    $"adapterExecutions={executionValues[1]} " +
                    $"directInitialBytes={initialBytes[2]} directInitialNs={initialNanoseconds[2]} " +
                    $"directRecomposeBytes={recompositionBytes[2]} directRecomposeNs={recompositionNanoseconds[2]} " +
                    $"directExecutions={Volatile.Read(ref s_directContentExecutions) - directExecutionBaseline}");
            }
        });
    }

    static int[] CreateRandomLaneOrder()
    {
        int[] order = [TreeLane, AdapterLane, DirectLane];
        Random.Shared.Shuffle(order);
        return order;
    }

    static string FormatLaneOrder(int[] lanes) =>
        string.Join(" then ", lanes.Select(static lane => lane switch
        {
            TreeLane => "tree",
            AdapterLane => "adapter",
            _ => "direct",
        }));

    [Composable]
    internal static void MeasureTree(
        int recomposition,
        StrongBox<int[]> executions,
        long[] initialBytes,
        long[] recompositionBytes,
        long[] initialNanoseconds,
        long[] recompositionNanoseconds)
    {
        long beforeBytes = GC.GetAllocatedBytesForCurrentThread();
        long started = Stopwatch.GetTimestamp();
        BuildTreeReplyCard("A practical guide to Compose", executions).Render();
        RecordMeasurement(
            lane: 0,
            GC.GetAllocatedBytesForCurrentThread() - beforeBytes,
            (long)Stopwatch.GetElapsedTime(started).TotalNanoseconds,
            initialBytes,
            recompositionBytes,
            initialNanoseconds,
            recompositionNanoseconds);
    }

    [Composable]
    internal static void MeasureAdapterComposableMethod(
        int recomposition,
        StrongBox<int[]> executions,
        long[] initialBytes,
        long[] recompositionBytes,
        long[] initialNanoseconds,
        long[] recompositionNanoseconds)
    {
        long beforeBytes = GC.GetAllocatedBytesForCurrentThread();
        long started = Stopwatch.GetTimestamp();
        AdapterComposableMethodReplyCard("A practical guide to Compose", executions);
        RecordMeasurement(
            lane: 1,
            GC.GetAllocatedBytesForCurrentThread() - beforeBytes,
            (long)Stopwatch.GetElapsedTime(started).TotalNanoseconds,
            initialBytes,
            recompositionBytes,
            initialNanoseconds,
            recompositionNanoseconds);
    }

    [Composable]
    internal static void MeasureDirectComposableMethod(
        int recomposition,
        long[] initialBytes,
        long[] recompositionBytes,
        long[] initialNanoseconds,
        long[] recompositionNanoseconds)
    {
        long beforeBytes = GC.GetAllocatedBytesForCurrentThread();
        long started = Stopwatch.GetTimestamp();
        Card(DirectReplyCardContent);
        RecordMeasurement(
            lane: 2,
            GC.GetAllocatedBytesForCurrentThread() - beforeBytes,
            (long)Stopwatch.GetElapsedTime(started).TotalNanoseconds,
            initialBytes,
            recompositionBytes,
            initialNanoseconds,
            recompositionNanoseconds);
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
    internal static void AdapterComposableMethodReplyCard(
        string subject,
        StrongBox<int[]> executions)
    {
        ExecutionValues(executions)[1]++;
        new Card
        {
            new Text(subject),
        }.Render();
    }

    static Card BuildTreeReplyCard(
        string subject,
        StrongBox<int[]> executions)
    {
        ExecutionValues(executions)[0]++;
        return new Card
        {
            new Text(subject),
        };
    }

    static void DirectReplyCardContent()
    {
        Interlocked.Increment(ref s_directContentExecutions);
        Text("A practical guide to Compose");
    }

    static int[] ExecutionValues(StrongBox<int[]> executions) =>
        executions.Value
        ?? throw new InvalidOperationException("Benchmark execution counters are not initialized.");
}
