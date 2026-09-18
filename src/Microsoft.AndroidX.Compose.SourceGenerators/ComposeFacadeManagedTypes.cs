using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace AndroidX.Compose.SourceGenerators;

/// <summary>
/// Managed types that wrapper-passthrough facades can surface as optional
/// properties or required constructor parameters while their handwritten
/// wrapper owns platform lowering.
/// </summary>
internal static class ComposeFacadeManagedTypes
{
    static readonly HashSet<string> OptionalRecognized =
    [
        "AndroidX.Compose.FloatRange",
        "AndroidX.Compose.NavigationSuiteType",
        "AndroidX.Compose.FlowRowOverflow",
        "AndroidX.Compose.FlowColumnOverflow",
    ];

    static readonly HashSet<string> RequiredRecognized =
    [
        "AndroidX.Compose.SnackbarHostState",
    ];

    public static bool IsRecognized(ITypeSymbol type, NullableAnnotation annotation)
    {
        if (type is not INamedTypeSymbol managed)
            return false;
        if (managed.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T &&
            managed.TypeArguments.Length == 1 && managed.TypeArguments[0] is INamedTypeSymbol value)
            managed = value;
        else if (!managed.IsReferenceType || annotation != NullableAnnotation.Annotated)
            return false;

        var ns = managed.ContainingNamespace?.IsGlobalNamespace == true
            ? string.Empty
            : managed.ContainingNamespace?.ToDisplayString() ?? string.Empty;
        var name = ns.Length == 0 ? managed.Name : ns + "." + managed.Name;
        return OptionalRecognized.Contains(name);
    }

    public static bool IsRequiredRecognized(ITypeSymbol type)
    {
        if (type is not INamedTypeSymbol managed || !managed.IsReferenceType)
            return false;
        var ns = managed.ContainingNamespace?.IsGlobalNamespace == true
            ? string.Empty
            : managed.ContainingNamespace?.ToDisplayString() ?? string.Empty;
        var name = ns.Length == 0 ? managed.Name : ns + "." + managed.Name;
        return RequiredRecognized.Contains(name);
    }
}
