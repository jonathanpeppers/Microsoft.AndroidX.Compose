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
        messageFormat: "Could not read [ComposeDefaults] attribute arguments for enum '{0}'",
        category: "ComposeNet",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor BridgeMissingDefaults = new(
        id: "CN2001",
        title: "[ComposeBridge] missing or unmatched Defaults",
        messageFormat: "Could not resolve a [ComposeDefaults] attribute matching '{0}' for bridge '{1}'",
        category: "ComposeNet",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor BridgeSignatureMismatch = new(
        id: "CN2002",
        title: "[ComposeBridge] signature/Defaults mismatch",
        messageFormat: "Bridge '{0}' signature has {1} user-parameter slot(s) before composer but Defaults '{2}' declares {3}",
        category: "ComposeNet",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor BridgeUnknownParameter = new(
        id: "CN2003",
        title: "[ComposeBridge] partial method parameter not in Defaults",
        messageFormat: "Bridge '{0}' has parameter '{1}' that does not match any Kotlin parameter name in Defaults '{2}'",
        category: "ComposeNet",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor BridgeMalformedSignature = new(
        id: "CN2004",
        title: "[ComposeBridge] malformed JNI signature",
        messageFormat: "Bridge '{0}' has malformed JNI signature: {1}",
        category: "ComposeNet",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
