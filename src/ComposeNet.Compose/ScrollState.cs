using System.Threading.Tasks;

namespace ComposeNet;

/// <summary>
/// Caller-supplied state holder for <see cref="Modifier.VerticalScroll"/>
/// and <see cref="Modifier.HorizontalScroll"/>. Wraps the bound
/// <c>androidx.compose.foundation.ScrollState</c> — the Kotlin class
/// has a public single-arg constructor and is plain enough for the
/// binder to expose, so no JNI bridge is needed.
/// </summary>
/// <remarks>
/// <para>
/// Construct one inside a <see cref="ComposeActivity.Remember{T}"/>
/// callback so the scroll position survives recompositions:
/// <code>
/// var scroll = Remember(() =&gt; new ScrollState());
///
/// new Column
/// {
///     Modifier.Companion.VerticalScroll(scroll),
///     // ... potentially-tall column children ...
/// }
/// </code>
/// </para>
/// <para>
/// Unlike Kotlin's <c>rememberScrollState()</c> (which uses
/// <c>rememberSaveable</c>), this state is held in the
/// <see cref="ComposeActivity"/>-scoped Remember cache, so it survives
/// recompositions but not process death / configuration changes. For
/// most apps that's fine; if you need savable scroll state, track it
/// yourself with the rest of your view-model state.
/// </para>
/// <para>
/// Programmatic scrolling is exposed via the <c>Async</c> methods —
/// <see cref="ScrollToAsync"/> jumps instantly, <see cref="AnimateScrollToAsync"/>
/// runs the default spring animation. Both bridge the underlying
/// Kotlin <c>suspend</c> function through <see cref="Task"/> so they
/// integrate with C# <c>async</c>/<c>await</c>. The returned task may
/// complete on the Compose main thread; awaiters resume on whatever
/// <see cref="System.Threading.SynchronizationContext"/> the
/// <c>await</c> captured.
/// </para>
/// </remarks>
public sealed class ScrollState
{
    internal AndroidX.Compose.Foundation.ScrollState Jvm { get; }

    /// <summary>
    /// Create a new <see cref="ScrollState"/> with the given initial
    /// pixel offset. Defaults to <c>0</c> (scrolled to the start).
    /// </summary>
    public ScrollState(int initial = 0)
    {
        Jvm = new AndroidX.Compose.Foundation.ScrollState(initial);
    }

    /// <summary>
    /// Current scroll position, in pixels. Mirrors Kotlin's
    /// <c>ScrollState.value</c>.
    /// </summary>
    public int Value => Jvm.Value;

    /// <summary>
    /// Maximum scrollable offset, in pixels, or
    /// <see cref="int.MaxValue"/> until the scrolling container has
    /// been laid out. Mirrors Kotlin's <c>ScrollState.maxValue</c>.
    /// </summary>
    public int MaxValue => Jvm.MaxValue;

    /// <summary>
    /// Size of the visible viewport along the scroll axis, in pixels.
    /// Mirrors Kotlin's <c>ScrollState.viewportSize</c>. Returns
    /// <c>0</c> until layout has run.
    /// </summary>
    public int ViewportSize => Jvm.ViewportSize;

    /// <summary>
    /// <c>true</c> while a fling or programmatic scroll is in flight.
    /// Mirrors Kotlin's <c>ScrollState.isScrollInProgress</c>.
    /// </summary>
    public bool IsScrollInProgress => Jvm.IsScrollInProgress;

    /// <summary>
    /// <c>true</c> when the content can scroll further forward (down
    /// for <see cref="Modifier.VerticalScroll"/>, right for
    /// <see cref="Modifier.HorizontalScroll"/> on LTR layouts).
    /// </summary>
    public bool CanScrollForward => Jvm.CanScrollForward;

    /// <summary>
    /// <c>true</c> when the content can scroll further backward (up
    /// for <see cref="Modifier.VerticalScroll"/>, left for
    /// <see cref="Modifier.HorizontalScroll"/> on LTR layouts).
    /// </summary>
    public bool CanScrollBackward => Jvm.CanScrollBackward;

    /// <summary>
    /// Jump instantly to the given pixel <paramref name="value"/>
    /// (clamped to <c>[0, MaxValue]</c>). Mirrors Kotlin's
    /// <c>ScrollState.scrollTo(value)</c>.
    /// </summary>
    /// <returns>
    /// A task whose result is the actual delta the scroll covered (the
    /// Kotlin return) — useful when the requested value was clamped.
    /// Faulted with the wrapped <c>Throwable</c> if Kotlin reports
    /// <c>Result.Failure</c>.
    /// </returns>
    public Task<float> ScrollToAsync(int value) =>
        SuspendBridge.Invoke<float>(
            cont => ComposeBridges.ScrollStateScrollTo(
                ((Java.Lang.Object)Jvm).Handle, value, cont),
            static boxed => boxed is Java.Lang.Float f
                ? f.FloatValue()
                : throw new System.InvalidCastException(
                    $"Expected java.lang.Float from ScrollState.scrollTo; got '{boxed?.Class?.Name ?? "null"}'"));

    /// <summary>
    /// Animate to the given pixel <paramref name="value"/> using the
    /// default Compose spring animation. Mirrors Kotlin's
    /// <c>ScrollState.animateScrollTo(value)</c>; the returned task
    /// completes when the animation lands.
    /// </summary>
    public Task AnimateScrollToAsync(int value) =>
        SuspendBridge.Invoke(cont =>
            ComposeBridges.ScrollStateAnimateScrollTo(
                ((Java.Lang.Object)Jvm).Handle, value, cont));
}
