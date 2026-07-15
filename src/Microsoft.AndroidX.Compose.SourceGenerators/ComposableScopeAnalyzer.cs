using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace AndroidX.Compose.SourceGenerators;

/// <summary>
/// Enforces the dynamic scope required by composerless composable APIs.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ComposableScopeAnalyzer : DiagnosticAnalyzer
{
    const string ComposableAttributeName = "AndroidX.Compose.ComposableAttribute";
    const string ComposableContentAttributeName = "AndroidX.Compose.ComposableContentAttribute";
    const string ComposablesTypeName = "AndroidX.Compose.Composables";
    const string ComposerTypeName = "AndroidX.Compose.Runtime.IComposer";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Diagnostics.ImplicitComposableOutsideScope);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
    }

    static void AnalyzeInvocation(OperationAnalysisContext context)
    {
        var invocation = (IInvocationOperation)context.Operation;
        var method = invocation.TargetMethod.ReducedFrom
            ?? invocation.TargetMethod.OriginalDefinition;

        if (!RequiresImplicitComposer(method) || IsInComposableScope(invocation, context.ContainingSymbol))
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            Diagnostics.ImplicitComposableOutsideScope,
            invocation.Syntax.GetLocation(),
            method.Name));
    }

    static bool RequiresImplicitComposer(IMethodSymbol method)
    {
        if (method.Parameters.Any(static p =>
                p.Type.ToDisplayString() == ComposerTypeName))
        {
            return false;
        }

        if (method.ContainingType.ToDisplayString() == ComposablesTypeName
            && method.Name == "DerivedStateOf")
        {
            return false;
        }

        return method.ContainingType.ToDisplayString() == ComposablesTypeName
            || HasAttribute(method, ComposableAttributeName)
            || IsImplicitCompositionLocalRead(method);
    }

    static bool IsImplicitCompositionLocalRead(IMethodSymbol method)
    {
        if (method.Name != "Current" || method.Parameters.Length != 0)
            return false;

        var type = method.ContainingType;
        return type.OriginalDefinition.ToDisplayString()
                == "AndroidX.Compose.CompositionLocal<T>"
            || type.ContainingNamespace.ToDisplayString() == "AndroidX.Compose"
                && type.Name is "LocalColorScheme"
                    or "LocalConfiguration"
                    or "LocalContext"
                    or "LocalLifecycleOwner"
                    or "LocalResources"
                    or "LocalView";
    }

    static bool IsInComposableScope(
        IInvocationOperation invocation,
        ISymbol containingSymbol)
    {
        for (IOperation? operation = invocation.Parent;
             operation is not null;
             operation = operation.Parent)
        {
            if (operation is IAnonymousFunctionOperation anonymous)
                return IsComposableContent(anonymous);

            if (operation is ILocalFunctionOperation)
                break;
        }

        return containingSymbol is IMethodSymbol method
            && (HasAttribute(method, ComposableAttributeName)
                || RequiresImplicitComposer(method));
    }

    static bool IsComposableContent(IAnonymousFunctionOperation anonymous)
    {
        IOperation current = anonymous;
        while (current.Parent is IDelegateCreationOperation
            or IConversionOperation
            or IParenthesizedOperation)
        {
            current = current.Parent;
        }

        return current.Parent is IArgumentOperation argument
            && argument.Parameter is { } parameter
            && HasAttribute(parameter, ComposableContentAttributeName);
    }

    static bool HasAttribute(ISymbol symbol, string metadataName) =>
        symbol.GetAttributes().Any(a =>
            a.AttributeClass?.ToDisplayString() == metadataName);
}
