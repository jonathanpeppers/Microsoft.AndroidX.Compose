package composenet.compose;

import androidx.compose.ui.layout.MeasurePolicy;

import kotlin.jvm.functions.Function3;

/**
 * Java-side adapter that turns a plain {@code Function3} into a
 * {@code MeasurePolicy}. {@code MeasurePolicy} is declared in Kotlin as a
 * {@code fun interface}, so it is SAM-convertible — a Java lambda
 * targeting it compiles to an {@code invokedynamic} call that hands the
 * lambda's {@code MethodType} to {@code LambdaMetafactory}, which
 * synthesizes a class implementing the interface's single abstract
 * method by its bytecode signature. The fact that {@code measure}'s
 * actual JVM name is {@code measure-3p2s80s} (mangled because
 * {@code Constraints} is an {@code @JvmInline value class}) never has
 * to be spelled in Java source.
 *
 * <p>Default interface methods on {@code MeasurePolicy} (the four
 * {@code IntrinsicMeasureScope.*Intrinsic*} helpers) are inherited
 * normally by the synthetic class, so {@code IntrinsicSize.Min}/{@code Max}
 * and any parent that asks children for intrinsic sizes get Compose's
 * correct fallback (which re-runs the measure block against a synthetic
 * {@code IntrinsicsMeasureScope}) — no manual stubbing required.</p>
 *
 * <p>The {@code constraints} arg arrives as a primitive {@code long} on
 * the synthetic SAM method, but is boxed at the {@code Function3.invoke}
 * boundary because Kotlin function types erase their generics to
 * {@code Object}. The C# side
 * ({@code AndroidX.Compose.MeasurePolicyLambda}) unboxes via
 * {@code java.lang.Long.longValue()}.</p>
 */
final class MeasurePolicyFactory {
    private MeasurePolicyFactory() { }

    /**
     * Build a {@code MeasurePolicy} that delegates its single abstract
     * measure method to {@code block}. The returned object's identity
     * is stable across calls only if the same {@code block} reference
     * is reused — the JVM may or may not cache lambda instances.
     */
    static MeasurePolicy create(
            final Function3<Object, Object, Long, Object> block) {
        return (scope, measurables, constraints) ->
                (androidx.compose.ui.layout.MeasureResult)
                        block.invoke(scope, measurables, Long.valueOf(constraints));
    }
}
