using AndroidX.Compose.Gallery.Registry;
using Xamarin.KotlinX.Coroutines.Flow;

namespace AndroidX.Compose.Gallery.Demos.StateEffectsAnimation;

/// <summary>
/// Real <c>kotlinx.coroutines.flow.MutableStateFlow&lt;int&gt;</c> driven from a
/// background <see cref="LaunchedEffect"/> tick loop and rendered via
/// <see cref="ComposeExtensions.CollectAsStateWithLifecycle{T}(IStateFlow, AndroidX.Compose.Runtime.IComposer)"/>.
/// </summary>
public static class KotlinStateFlowDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "state-kotlin-stateflow",
        CategoryId:  "state-effects",
        Title:       "Kotlin StateFlow",
        Description: "A real kotlinx.coroutines.flow.MutableStateFlow<int> driven by a 1Hz LaunchedEffect tick loop, " +
                     "rendered via the lifecycle-aware Flow<T>.CollectAsStateWithLifecycle bridge. " +
                     "Background updates pause while the activity is below STARTED and resume on return.",
        Build:       c =>
        {
            // Cache the real Kotlin MutableStateFlow across recompositions —
            // its identity is what the lifecycle-aware collector keys on.
            var flow = c.Remember(() => StateFlowKt.MutableStateFlow(Java.Lang.Integer.ValueOf(0)));

            // Subscribe the surrounding composition scope to the flow's
            // emissions while LocalLifecycleOwner.Current is at least
            // Lifecycle.State.STARTED (the default).
            int tick = flow.CollectAsStateWithLifecycle<int>(c).Value;

            return new Column
            {
                Modifier.Padding(16),
                new Text($"Auto-tick: {tick}")
                {
                    FontSize   = 22,
                    FontWeight = FontWeight.SemiBold,
                },
                new Spacer { Modifier = Modifier.Height(12) },
                new LaunchedEffect(flow, async ct =>
                {
                    try
                    {
                        while (!ct.IsCancellationRequested)
                        {
                            await Task.Delay(1000, ct);
                            int current = ((Java.Lang.Integer)flow.Value!).IntValue();
                            flow.Value = Java.Lang.Integer.ValueOf(current + 1);
                        }
                    }
                    catch (OperationCanceledException) { }
                }),
                new Row(horizontalArrangement: Arrangement.SpacedBy(8))
                {
                    new Button(onClick: () =>
                    {
                        int current = ((Java.Lang.Integer)flow.Value!).IntValue();
                        flow.Value = Java.Lang.Integer.ValueOf(current + 1);
                    })
                    {
                        new Text("Increment"),
                    },
                    new Button(onClick: () => flow.Value = Java.Lang.Integer.ValueOf(0))
                    {
                        new Text("Reset"),
                    },
                },
            };
        });
}
