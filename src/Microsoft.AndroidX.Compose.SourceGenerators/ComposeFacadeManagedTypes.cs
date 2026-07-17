using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace AndroidX.Compose.SourceGenerators;

/// <summary>
/// Managed value types that wrapper-passthrough facades can surface as
/// nullable properties while their handwritten wrapper owns platform lowering.
/// </summary>
internal static class ComposeFacadeManagedTypes
{
    static readonly HashSet<string> Recognized =
    [
        "AndroidX.Compose.FloatRange",
        "AndroidX.Compose.NavigationSuiteType",
    ];

    public static bool IsRecognized(ITypeSymbol type)
    {
        if (type is not INamedTypeSymbol nullable ||
            nullable.OriginalDefinition.SpecialType != SpecialType.System_Nullable_T ||
            nullable.TypeArguments.Length != 1 ||
            nullable.TypeArguments[0] is not INamedTypeSymbol managed)
            return false;

        var ns = managed.ContainingNamespace?.IsGlobalNamespace == true
            ? string.Empty
            : managed.ContainingNamespace?.ToDisplayString() ?? string.Empty;
        var name = ns.Length == 0 ? managed.Name : ns + "." + managed.Name;
        return Recognized.Contains(name);
    }
}
