using AndroidX.Compose.UI.Focus;

namespace AndroidX.Compose;

/// <summary>Conveniences for the official Compose focus-manager binding.</summary>
public static class FocusManagerExtensions
{
    /// <summary>
    /// Clears input focus, returning it to the root focus target. By default,
    /// captured focus is retained; pass <c>force: true</c> to release it too.
    /// This is not an accessibility-focus operation or a keyboard visibility
    /// command. Call on the UI thread, using a manager captured in composition.
    /// </summary>
    public static void ClearFocus(this IFocusManager manager, bool force = false)
    {
        ArgumentNullException.ThrowIfNull(manager);
        manager.ClearFocus(force);
    }
}
