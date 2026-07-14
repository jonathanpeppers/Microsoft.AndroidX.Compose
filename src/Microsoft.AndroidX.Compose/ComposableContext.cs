using System.ComponentModel;
using AndroidX.Compose.Runtime;

namespace AndroidX.Compose;

/// <summary>
/// Provides the composer active for an implicitly threaded
/// <see cref="ComposableAttribute"/> call.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ComposableContext
{
    [ThreadStatic]
    static IComposer? s_current;

    /// <summary>Gets the composer active on the current thread.</summary>
    public static IComposer Current =>
        s_current
        ?? throw new InvalidOperationException(
            "No composer is active. Call this API from a [Composable] method or composable content callback.");

    /// <summary>Installs <paramref name="composer"/> for the current dynamic scope.</summary>
    public static ComposableContextScope Enter(IComposer composer)
    {
        ArgumentNullException.ThrowIfNull(composer);
        var scope = new ComposableContextScope(s_current);
        s_current = composer;
        return scope;
    }

    internal static void Restore(IComposer? composer) => s_current = composer;
}
