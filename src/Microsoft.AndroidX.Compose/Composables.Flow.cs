using Xamarin.KotlinX.Coroutines.Flow;

namespace AndroidX.Compose;

public static partial class Composables
{
    /// <summary>Collects a Kotlin state flow while the host lifecycle is active.</summary>
    public static CollectedState<T> CollectAsStateWithLifecycle<T>(IStateFlow stateFlow) =>
        ComposeExtensions.CollectAsStateWithLifecycle<T>(
            stateFlow, ComposableContext.Current);

    /// <summary>Collects a Kotlin flow while the host lifecycle is active.</summary>
    public static CollectedState<T> CollectAsStateWithLifecycle<T>(
        IFlow flow,
        T initialValue) =>
        ComposeExtensions.CollectAsStateWithLifecycle(
            flow, initialValue, ComposableContext.Current);
}
