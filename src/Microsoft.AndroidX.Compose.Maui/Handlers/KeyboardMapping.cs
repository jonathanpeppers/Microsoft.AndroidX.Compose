using System.Diagnostics;
using ComposeKeyboardType = AndroidX.Compose.KeyboardType;

namespace Microsoft.AndroidX.Compose.Maui.Handlers;

/// <summary>
/// Shared mapping helpers used by the text-input handlers
/// (<see cref="EntryHandler"/>, <see cref="EditorHandler"/>,
/// <see cref="SearchBarHandler"/>) so a new <see cref="Keyboard"/>
/// variant only has to be wired once.
/// </summary>
internal static class KeyboardMapping
{
    /// <summary>
    /// Map MAUI's <see cref="Keyboard"/> singletons to a Compose
    /// <c>androidx.compose.ui.text.input.KeyboardType</c> int.
    /// </summary>
    /// <remarks>
    /// Compose's <c>@JvmInline value class KeyboardType(Int)</c> lowers
    /// each constant to a stable sequential int; the named properties
    /// on <see cref="ComposeKeyboardType"/> read the same Companion
    /// getters the Kotlin compiler emits.
    ///
    /// Falls through to <see cref="ComposeKeyboardType.Text"/> for
    /// anything unrecognised (CustomKeyboard / Plain / new MAUI
    /// variants) so the user still gets a working IME instead of a
    /// blank surface. Logs the fallback through
    /// <paramref name="callerTag"/> so each handler's debug output
    /// stays distinguishable.
    /// </remarks>
    public static int Resolve(Keyboard? keyboard, string callerTag)
    {
        if (keyboard is null) return ComposeKeyboardType.Text;
        if (keyboard == Keyboard.Numeric)   return ComposeKeyboardType.Number;
        if (keyboard == Keyboard.Telephone) return ComposeKeyboardType.Phone;
        if (keyboard == Keyboard.Url)       return ComposeKeyboardType.Uri;
        if (keyboard == Keyboard.Email)     return ComposeKeyboardType.Email;
        if (keyboard == Keyboard.Default
            || keyboard == Keyboard.Text
            || keyboard == Keyboard.Chat) return ComposeKeyboardType.Text;

        Debug.WriteLine(
            $"[{callerTag}] Unmapped Keyboard '{keyboard}'; falling back to Text.");
        return ComposeKeyboardType.Text;
    }
}
