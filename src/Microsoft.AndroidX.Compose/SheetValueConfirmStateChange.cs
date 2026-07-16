using Android.Runtime;
using AndroidX.Compose.Material3;

namespace AndroidX.Compose;

/// <summary>
/// <c>Function1&lt;SheetValue, Boolean&gt;</c> adapter used as the
/// <c>confirmValueChange</c> callback when constructing a
/// <c>SheetState</c> via <c>rememberModalBottomSheetState</c> /
/// <c>rememberStandardBottomSheetState</c>.
/// </summary>
[Register("net/compose/SheetValueConfirmStateChange")]
internal sealed class SheetValueConfirmStateChange : ConfirmStateChangeAdapter<SheetValue>
{
}
