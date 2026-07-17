namespace AndroidX.Compose;

/// <summary>
/// Caller-supplied state holder for <see cref="SnackbarHost"/>. Wraps
/// the bound <c>androidx.compose.material3.SnackbarHostState</c> — the
/// Kotlin class has a public no-arg constructor and is plain enough
/// for the binder to expose, so no JNI bridge is needed.
/// </summary>
public sealed class SnackbarHostState
{
    internal AndroidX.Compose.Material3.SnackbarHostState Jvm { get; } = new();

    /// <summary>
    /// Shows or queues a snackbar and completes after it is dismissed or its
    /// action is selected.
    /// </summary>
    /// <param name="message">Text displayed by the snackbar.</param>
    /// <param name="actionLabel">Optional action button label.</param>
    /// <param name="withDismissAction">Whether to show an explicit dismiss action.</param>
    /// <param name="duration">
    /// Explicit duration, or <c>null</c> to use Compose's contextual default:
    /// <see cref="SnackbarDuration.Short"/> without an action and
    /// <see cref="SnackbarDuration.Indefinite"/> with one.
    /// </param>
    /// <param name="cancellationToken">
    /// Cancels the returned task and removes the snackbar from the active
    /// display or pending queue.
    /// </param>
    public Task<SnackbarResult> ShowSnackbarAsync(
        string message,
        string? actionLabel = null,
        bool withDismissAction = false,
        SnackbarDuration? duration = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        var effectiveDuration = duration
            ?? (actionLabel is null
                ? SnackbarDuration.Short
                : SnackbarDuration.Indefinite);
        var jvmDuration = ToJvmDuration(effectiveDuration);
        return SuspendBridge.Invoke(
            cont => ComposeBridges.SnackbarHostStateShowSnackbar(
                Jvm.Handle,
                message,
                actionLabel,
                withDismissAction,
                jvmDuration,
                cont),
            FromJvmResult,
            cancellationToken);
    }

    static AndroidX.Compose.Material3.SnackbarDuration ToJvmDuration(
        SnackbarDuration duration) =>
        duration switch
        {
            SnackbarDuration.Short =>
                AndroidX.Compose.Material3.SnackbarDuration.Short
                ?? throw new InvalidOperationException(
                    "Material SnackbarDuration.Short was unavailable."),
            SnackbarDuration.Long =>
                AndroidX.Compose.Material3.SnackbarDuration.Long
                ?? throw new InvalidOperationException(
                    "Material SnackbarDuration.Long was unavailable."),
            SnackbarDuration.Indefinite =>
                AndroidX.Compose.Material3.SnackbarDuration.Indefinite
                ?? throw new InvalidOperationException(
                    "Material SnackbarDuration.Indefinite was unavailable."),
            _ => throw new ArgumentOutOfRangeException(
                nameof(duration), duration, "Unknown snackbar duration."),
        };

    static SnackbarResult FromJvmResult(Java.Lang.Object? boxed)
    {
        var result = boxed
            ?? throw new InvalidCastException(
                "SnackbarHostState.showSnackbar returned null.");
        return result.ToString() switch
        {
            "Dismissed" => SnackbarResult.Dismissed,
            "ActionPerformed" => SnackbarResult.ActionPerformed,
            var value => throw new InvalidCastException(
                $"Unknown Material SnackbarResult '{value}'."),
        };
    }
}
