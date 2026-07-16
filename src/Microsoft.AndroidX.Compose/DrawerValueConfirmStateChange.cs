using Android.Runtime;
using AndroidX.Compose.Material3;

namespace AndroidX.Compose;

/// <summary>
/// <c>Function1&lt;DrawerValue, Boolean&gt;</c> adapter used as the
/// <c>confirmStateChange</c> callback when constructing a
/// <c>DrawerState</c> via <c>rememberDrawerState</c>.
/// </summary>
[Register("net/compose/DrawerValueConfirmStateChange")]
internal sealed class DrawerValueConfirmStateChange : ConfirmStateChangeAdapter<DrawerValue>
{
}
