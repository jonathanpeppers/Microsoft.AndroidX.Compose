using Android.Runtime;
using AndroidX.Compose.Material3;

namespace AndroidX.Compose;

/// <summary>
/// <c>Function1&lt;DrawerValue, Boolean&gt;</c> adapter used as the
/// <c>confirmStateChange</c> callback when constructing a
/// <c>DrawerState</c> via <c>rememberDrawerState</c>.
/// </summary>
/// <remarks>
/// See <see cref="ConfirmStateChangeAdapter{T}"/> for the lifecycle
/// rules (one instance per <see cref="ComposableNode"/>, mutate
/// <c>Callback</c> freely between renders). The class name follows
/// the source generator's convention
/// <c>&lt;TName&gt;ConfirmStateChange</c> so a
/// <c>[ConfirmStateChange(typeof(DrawerValue))]</c> attribute on a
/// Remember bridge parameter resolves to this adapter automatically.
/// Public so generated facade classes can declare it as a
/// <c>readonly</c> field; not part of the developer-facing API and
/// should not be constructed directly.
/// </remarks>
[Register("net/compose/DrawerValueConfirmStateChange")]
public sealed class DrawerValueConfirmStateChange : ConfirmStateChangeAdapter<DrawerValue>
{
}
