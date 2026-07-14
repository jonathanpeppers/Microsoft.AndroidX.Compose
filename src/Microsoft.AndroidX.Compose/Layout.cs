using Android.Runtime;
using AndroidX.Compose.Runtime;
using AndroidX.Compose.UI.Layout;

namespace AndroidX.Compose;

/// <summary>
/// Compose's low-level <c>Layout(content, modifier, measurePolicy)</c>
/// primitive — accepts a measure-policy callback that receives a list
/// of <see cref="Measurable"/> children plus the parent's
/// <see cref="Constraints"/>, and is responsible for measuring each
/// child and calling
/// <see cref="MeasureScope.Layout(int, int, Action{PlacementScope})"/>
/// to commit the chosen size and place the children at custom
/// positions.
/// </summary>
/// <remarks>
/// <para>
/// Use this when stock containers (<see cref="Column"/>, <see cref="Row"/>,
/// <see cref="Box"/>, <c>LazyColumn</c>, <see cref="FlowRow"/>) don't
/// describe the layout you need — e.g. balancing N items into M
/// columns based on available width, or placing children at
/// hand-computed positions.
/// </para>
/// <para>
/// <strong>Intrinsic measurements.</strong> The factory-built
/// <c>MeasurePolicy</c> stubs <c>minIntrinsicWidth</c> /
/// <c>maxIntrinsicWidth</c> / <c>minIntrinsicHeight</c> /
/// <c>maxIntrinsicHeight</c> to <c>0</c>. Avoid using this primitive
/// inside a parent that asks its children for intrinsic sizes (e.g.
/// the <c>Modifier.height(IntrinsicSize.Max)</c> or
/// <c>Row(verticalAlignment = Alignment.CenterVertically)</c> with
/// <c>Modifier.fillMaxHeight()</c> children) — that path will read 0.
/// </para>
/// <para>
/// <strong>Identity stability.</strong> The JNI proxy implementing
/// <c>MeasurePolicy</c> is cached via <see cref="ComposeExtensions.Remember{T}(IComposer, Func{T}, int, string)"/>
/// across recompositions of the parent. The user delegate is rewritten
/// on every render (closures may capture fresh state), but the JNI
/// identity is stable so Compose's measure cache survives.
/// </para>
/// </remarks>
public sealed class Layout : ComposableContainer
{
    readonly Func<MeasureScope, IReadOnlyList<Measurable>, Constraints, MeasureResult>
        _measurePolicy;

    /// <summary>
    /// Build a <see cref="Layout"/> whose children are measured and
    /// placed by the supplied <paramref name="measurePolicy"/>.
    /// </summary>
    /// <param name="measurePolicy">
    /// Measure-and-place callback. Receives a <see cref="MeasureScope"/>
    /// receiver, the list of <see cref="Measurable"/> children (in the
    /// order they were added to this <see cref="Layout"/>), and the
    /// parent's <see cref="Constraints"/>. Must call
    /// <see cref="MeasureScope.Layout(int, int, Action{PlacementScope})"/>
    /// exactly once and return its result.
    /// </param>
    public Layout(
        Func<MeasureScope, IReadOnlyList<Measurable>, Constraints, MeasureResult>
            measurePolicy)
    {
        ArgumentNullException.ThrowIfNull(measurePolicy);
        _measurePolicy = measurePolicy;
    }

    public override void Render(IComposer composer)
    {
        var modifier = BuildModifier();
        var content  = ComposableLambdas.Wrap2(composer, c => RenderChildren(c));
        var holder   = composer.Remember(() => MeasurePolicyHolder.Build());

        // Refresh the user delegate every pass — closures may have
        // captured new state since last composition. JNI identity is
        // stable because Holder.Lambda + Holder.Policy are cached.
        holder.Lambda.Body = _measurePolicy;

        int defaults = (int)LayoutDefault.All;
        if (modifier is not null) defaults &= ~(int)LayoutDefault.Modifier;

        LayoutKt.Layout(
            content:       content,
            modifier:      modifier,
            measurePolicy: holder.Policy,
            _composer:     composer,
            p4:            0,
            _changed:      defaults);
    }

    sealed class MeasurePolicyHolder
    {
        public MeasurePolicyLambda Lambda { get; }
        public IMeasurePolicy Policy { get; }

        MeasurePolicyHolder(MeasurePolicyLambda lambda, IMeasurePolicy policy)
        {
            Lambda = lambda;
            Policy = policy;
        }

        public static MeasurePolicyHolder Build()
        {
            var lambda = new MeasurePolicyLambda();
            IntPtr handle = ComposeBridges.MeasurePolicyFactoryCreate(lambda);
            // TransferLocalRef hands the local ref to the peer cache, which
            // promotes it to a global ref. JavaCast<IMeasurePolicy> creates
            // an invoker peer so the binding's [Register] dispatches work.
            var peer = Java.Lang.Object.GetObject<Java.Lang.Object>(
                handle, JniHandleOwnership.TransferLocalRef)!;
            return new MeasurePolicyHolder(lambda, peer.JavaCast<IMeasurePolicy>());
        }
    }
}
