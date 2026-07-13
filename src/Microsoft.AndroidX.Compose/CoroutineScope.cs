using Kotlin.Coroutines;
using Xamarin.KotlinX.Coroutines;

namespace AndroidX.Compose;

/// <summary>
/// A composition-owned coroutine scope for launching asynchronous work from
/// event handlers.
/// </summary>
/// <remarks>
/// Instances are created by
/// <see cref="ComposeExtensions.RememberCoroutineScope(AndroidX.Compose.Runtime.IComposer)"/>.
/// Compose cancels the scope when its call site leaves the composition. Pass
/// the token supplied to <see cref="Launch"/> into Compose <c>*Async</c>
/// methods so their Kotlin jobs are cancelled with the composition.
/// </remarks>
public sealed class CoroutineScope
{
    readonly ICoroutineScope _jvm;

    internal CoroutineScope(ICoroutineScope jvm)
    {
        ArgumentNullException.ThrowIfNull(jvm);
        _jvm = jvm;
    }

    /// <summary>
    /// Launches <paramref name="body"/> as a child of this composition-owned
    /// scope and returns a task representing the managed body.
    /// </summary>
    /// <param name="body">
    /// Work to run. The supplied token is cancelled when the Kotlin child job
    /// is cancelled, including when this scope leaves the composition.
    /// </param>
    /// <returns>
    /// A task that completes, faults, or is cancelled with
    /// <paramref name="body"/>. Managed faults remain on this task instead of
    /// escaping through Kotlin's global coroutine exception handler.
    /// </returns>
    public Task Launch(Func<CancellationToken, Task> body)
    {
        ArgumentNullException.ThrowIfNull(body);

        var operation = new CoroutineScopeLaunchBody(body);
        // Both accessors return JVM-wide singleton peers. Explicit disposal
        // could invalidate a wrapper concurrently used by another launch.
        var context = EmptyCoroutineContext.Instance;
        var start = CoroutineStart.Default
            ?? throw new InvalidOperationException(
                "kotlinx.coroutines.CoroutineStart.DEFAULT was not available.");
        using var job = BuildersKt.Launch(_jvm, context, start, operation);
        operation.AttachJob(job);
        return operation.Task;
    }
}
