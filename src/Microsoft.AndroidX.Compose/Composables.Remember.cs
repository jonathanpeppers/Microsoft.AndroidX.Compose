using System.Runtime.CompilerServices;

namespace AndroidX.Compose;

public static partial class Composables
{
    /// <summary>
    /// Remembers a value at the current implicit-composer call site.
    /// </summary>
    public static T Remember<T>(
        Func<T> factory,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "") =>
        ComposeExtensions.Remember(ComposableContext.Current, factory, line, file);
}
