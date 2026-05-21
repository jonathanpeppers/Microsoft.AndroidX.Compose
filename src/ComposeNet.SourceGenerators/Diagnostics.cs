using Microsoft.CodeAnalysis;

namespace ComposeNet.SourceGenerators;

internal static class Diagnostics
{
    public static readonly DiagnosticDescriptor MethodNotFound = new(
        id: "CN1001",
        title: "Compose method not found",
        messageFormat: "No static method '{0}' found on type '{1}'",
        category: "ComposeNet",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NotComposable = new(
        id: "CN1002",
        title: "Method is not @Composable",
        messageFormat: "Method '{0}' has no IComposer parameter — not a @Composable signature",
        category: "ComposeNet",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MalformedAttribute = new(
        id: "CN1003",
        title: "Malformed [ComposeDefaults] attribute",
        messageFormat: "Could not read [ComposeDefaults<T>(string)] on enum '{0}'",
        category: "ComposeNet",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
