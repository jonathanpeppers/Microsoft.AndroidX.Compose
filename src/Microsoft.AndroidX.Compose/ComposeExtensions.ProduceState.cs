using System.Runtime.CompilerServices;
using AndroidX.Compose.Runtime;

namespace AndroidX.Compose;

public static partial class ComposeExtensions
{
    /// <summary>
    /// C# parity for Kotlin's
    /// <c>produceState(initialValue, vararg keys) { producer }</c>:
    /// remembers a <see cref="MutableState{T}"/> seeded with
    /// <paramref name="initialValue"/>. Starts <paramref name="producer"/>
    /// after this call site is successfully applied to the composition. The
    /// producer receives the state to write to plus a
    /// <see cref="CancellationToken"/> that fires when this call site
    /// leaves the composition.
    /// </summary>
    public static MutableState<T> ProduceState<T>(
        this IComposer composer,
        T initialValue,
        Func<MutableState<T>, CancellationToken, Task> producer,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
        => ProduceStateCore(composer, initialValue, producer, keys: null, line, file);

    /// <summary>
    /// Keyed <c>produceState(initial, key1) { producer }</c>: cancels
    /// the running producer and starts the current delegate after a
    /// <paramref name="key1"/> change is successfully applied (structural
    /// equality). An abandoned recomposition leaves the committed producer
    /// running.
    /// </summary>
    public static MutableState<T> ProduceState<T>(
        this IComposer composer,
        T initialValue,
        object? key1,
        Func<MutableState<T>, CancellationToken, Task> producer,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
        => ProduceStateCore(composer, initialValue, producer, [key1], line, file);

    /// <summary>Keyed <c>produceState(initial, key1, key2) { producer }</c>.</summary>
    public static MutableState<T> ProduceState<T>(
        this IComposer composer,
        T initialValue,
        object? key1,
        object? key2,
        Func<MutableState<T>, CancellationToken, Task> producer,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
        => ProduceStateCore(composer, initialValue, producer, [key1, key2], line, file);

    /// <summary>Keyed <c>produceState(initial, key1, key2, key3) { producer }</c>.</summary>
    public static MutableState<T> ProduceState<T>(
        this IComposer composer,
        T initialValue,
        object? key1,
        object? key2,
        object? key3,
        Func<MutableState<T>, CancellationToken, Task> producer,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
        => ProduceStateCore(composer, initialValue, producer, [key1, key2, key3], line, file);

    /// <summary>
    /// Array-form keyed <c>produceState(initial, vararg keys) { producer }</c>.
    /// </summary>
    public static MutableState<T> ProduceStateKeyed<T>(
        this IComposer composer,
        T initialValue,
        object?[] keys,
        Func<MutableState<T>, CancellationToken, Task> producer,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
        => ProduceStateCore(composer, initialValue, producer, keys ?? throw new ArgumentNullException(nameof(keys)), line, file);

    static MutableState<T> ProduceStateCore<T>(
        IComposer composer,
        T initialValue,
        Func<MutableState<T>, CancellationToken, Task> producer,
        object?[]? keys,
        int line,
        string file)
    {
        ArgumentNullException.ThrowIfNull(composer);
        ArgumentNullException.ThrowIfNull(producer);

        composer.StartReplaceableGroup(SourceLocationKey.Compute(line, file));
        try
        {
            MutableState<T> state;
            if (composer.RememberedValue() is RememberHolder holder
                && holder.Value is MutableState<T> rememberedState)
            {
                state = rememberedState;
            }
            else
            {
                state = new MutableState<T>(initialValue);
                composer.UpdateRememberedValue(new RememberHolder(state));
            }

            // Keep each producer lifetime as a direct slot value so Compose
            // commits replacement before forgetting the old observer and
            // remembering the new one. An abandoned render therefore cannot
            // cancel committed work or start speculative work.
            if (composer.RememberedValue() is not ProduceStateScope<T> existing
                || !RememberHolder.KeysEqual(existing.Keys, keys))
            {
                composer.UpdateRememberedValue(
                    new ProduceStateScope<T>(state, producer, keys));
            }

            return state;
        }
        finally
        {
            composer.EndReplaceableGroup();
        }
    }
}
