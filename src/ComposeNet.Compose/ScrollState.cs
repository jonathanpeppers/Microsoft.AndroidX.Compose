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
/// Programmatic scrolling (<c>scrollTo</c> / <c>animateScrollTo</c>) is
/// a Kotlin suspending function and isn't yet wired up in this binding
/// — read-only inspection of the current position via
/// <see cref="Value"/> / <see cref="MaxValue"/> is the supported
/// surface today.
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
}
