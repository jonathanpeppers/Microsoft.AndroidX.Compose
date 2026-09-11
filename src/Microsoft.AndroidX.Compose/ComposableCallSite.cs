using System.ComponentModel;
using AndroidX.Compose.Runtime;

namespace AndroidX.Compose;

/// <summary>Structural entry protocol used by generated composable interceptors.</summary>
/// <remarks>Compiler infrastructure, not an application-level keyed-content API.</remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ComposableCallSite
{
    internal static CompositionOccurrenceRegistry<IControlledComposition> Occurrences { get; } = new();

    /// <summary>Opens a lexical envelope and its retained, positional occurrence group.</summary>
    public static void Start(IComposer composer, int key, Java.Lang.String identity)
    {
        ArgumentNullException.ThrowIfNull(composer);
        ArgumentNullException.ThrowIfNull(identity);
        long parent = composer.CompositeKeyHashCode;
        composer.StartMovableGroup(key, identity);
        if (composer.RememberedValue() is not ComposableCallSiteOccurrence occurrence)
        {
            occurrence = new(composer.Composition, parent, identity.ToString());
            try
            {
                composer.UpdateRememberedValue(occurrence);
            }
            catch
            {
                occurrence.Release();
                throw;
            }
        }
        composer.StartReplaceableGroup(occurrence.Ordinal);
    }

    /// <summary>Closes the occurrence group and lexical envelope after a normal entry completes.</summary>
    public static void End(IComposer composer)
    {
        ArgumentNullException.ThrowIfNull(composer);
        composer.EndReplaceableGroup();
        composer.EndMovableGroup();
    }
}
