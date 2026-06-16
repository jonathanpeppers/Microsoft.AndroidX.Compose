using Android.Runtime;
using AndroidX.Compose.Material3;

namespace AndroidX.Compose;

/// <summary>
/// <c>Function1&lt;SheetValue, Boolean&gt;</c> adapter used as the
/// <c>confirmValueChange</c> callback when constructing a
/// <c>SheetState</c> via <c>rememberModalBottomSheetState</c> /
/// <c>rememberStandardBottomSheetState</c>.
/// </summary>
/// <remarks>
/// See <see cref="ConfirmStateChangeAdapter{T}"/> for the lifecycle
/// rules (one instance per <see cref="ComposableNode"/>, mutate
/// <c>Callback</c> freely between renders). The class name follows
/// the source generator's convention
/// <c>&lt;TName&gt;ConfirmStateChange</c> so a
/// <c>[ConfirmStateChange(typeof(SheetValue))]</c> attribute on a
/// Remember bridge parameter resolves to this adapter automatically.
/// Public so generated facade classes can declare it as a
/// <c>readonly</c> field; not part of the developer-facing API and
/// should not be constructed directly.
/// </remarks>
[Register("net/compose/SheetValueConfirmStateChange")]
public sealed class SheetValueConfirmStateChange : ConfirmStateChangeAdapter<SheetValue>
{
}
