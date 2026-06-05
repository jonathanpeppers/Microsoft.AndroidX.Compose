using Android.Runtime;
using Kotlin.Jvm.Functions;

namespace ComposeNet;

/// <summary>
/// Function0&lt;Integer&gt; — Kotlin's
/// <c>rememberCarouselState(itemCount: () -&gt; Int)</c> takes a
/// no-arg lambda returning a boxed <see cref="Java.Lang.Integer"/>.
/// This adapter wraps a <see cref="System.Func{Int32}"/> so the
/// carousel can re-read the current item count on every measure pass
/// without us having to invalidate the carousel state when the
/// underlying list grows or shrinks.
///
/// Separate from <see cref="ComposableLambda0"/>, which returns
/// <c>Kotlin.Unit</c> (the JNI shape for onClick-style callbacks).
/// </summary>
[Register("composenet/compose/CarouselItemCountLambda")]
internal sealed class CarouselItemCountLambda : Java.Lang.Object, IFunction0
{
    readonly System.Func<int> _count;
    public CarouselItemCountLambda(System.Func<int> count) => _count = count;

    public Java.Lang.Object Invoke() => Java.Lang.Integer.ValueOf(_count());
}
