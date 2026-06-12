using Android.Runtime;
using Kotlin.Jvm.Functions;

namespace AndroidX.Compose;

/// <summary>
/// JCW <c>Function1&lt;Placeable.PlacementScope, Unit&gt;</c> passed as
/// the placement block to
/// <c>androidx.compose.ui.layout.MeasureScope.layout</c>. Wraps a
/// developer <see cref="Action{T}"/> taking a
/// <see cref="PlacementScope"/>.
/// </summary>
/// <remarks>
/// One instance per call to
/// <see cref="MeasureScope.Layout(int, int, Action{PlacementScope})"/>.
/// The placement block is invoked synchronously inside Compose's layout
/// pass (no remember caching), so a fresh JCW each call is cheap and
/// correct; the parent measure-policy lambda already pins the user
/// delegate identity.
/// </remarks>
[Register("composenet/compose/PlacementBlockLambda")]
internal sealed class PlacementBlockLambda : Java.Lang.Object, IFunction1
{
    readonly Action<PlacementScope> _body;

    public PlacementBlockLambda(Action<PlacementScope> body) => _body = body;

    public Java.Lang.Object? Invoke(Java.Lang.Object? p0)
    {
        if (p0 is null)
            throw new InvalidOperationException(
                "PlacementBlockLambda.Invoke received a null Placeable.PlacementScope.");
        try
        {
            _body(new PlacementScope(p0.Handle));
            return Kotlin.Unit.Instance!;
        }
        finally
        {
            GC.KeepAlive(p0);
        }
    }
}
