using System.Collections.Generic;
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
    const string ComposableNodeTypeName = "AndroidX.Compose.ComposableNode";
    const string ComposablesTypeName = "AndroidX.Compose.Composables";
    const string ComposerTypeName = "AndroidX.Compose.Runtime.IComposer";
    const string WindowInsetsTypeName = "AndroidX.Compose.WindowInsets";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Diagnostics.ImplicitComposableOutsideScope);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(static startContext =>
        {
            var index = new FlowIndex();

            startContext.RegisterOperationAction(index.AddInvocation, OperationKind.Invocation);
            startContext.RegisterOperationAction(index.AddMethodReference, OperationKind.MethodReference);
            startContext.RegisterOperationAction(index.AddLocalReference, OperationKind.LocalReference);
            startContext.RegisterCompilationEndAction(index.Analyze);
        });
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
            || IsImplicitCompositionLocalRead(method)
            || IsImplicitComposableNodeRender(method)
            || IsImplicitWindowInsetsRead(method);
    }

    static bool IsImplicitWindowInsetsRead(IMethodSymbol method) =>
        method.Name == "AsPaddingValues"
        && !method.IsStatic
        && method.Parameters.Length == 0
        && method.ContainingType.ToDisplayString() == WindowInsetsTypeName;

    static bool IsImplicitComposableNodeRender(IMethodSymbol method) =>
        method.Name == "Render"
        && !method.IsStatic
        && method.Parameters.Length == 0
        && method.ContainingType.ToDisplayString() == ComposableNodeTypeName;

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

    static bool IsComposableMethod(ISymbol symbol) =>
        symbol is IMethodSymbol method
        && (HasAttribute(method, ComposableAttributeName)
            || RequiresImplicitComposer(method));

    static bool HasAttribute(ISymbol symbol, string metadataName) =>
        symbol.GetAttributes().Any(a =>
            a.AttributeClass?.ToDisplayString() == metadataName);

    sealed class FlowIndex
    {
        const int MaximumTraceDepth = 32;
        const int MaximumTraceSteps = 256;

        readonly object gate = new();
        readonly List<(IInvocationOperation Operation, ISymbol Owner)> invocations = [];
        readonly List<(IMethodReferenceOperation Operation, ISymbol Owner)> methodReferences = [];
        readonly Dictionary<ISymbol, List<IOperation>> symbolReferences =
            new(SymbolEqualityComparer.Default);
        readonly Dictionary<IMethodSymbol, List<IInvocationOperation>> methodInvocations =
            new(SymbolEqualityComparer.Default);
        readonly Dictionary<IInvocationOperation, ISymbol> invocationOwners = [];

        public void AddInvocation(OperationAnalysisContext context)
        {
            var invocation = (IInvocationOperation)context.Operation;
            var target = Canonical(invocation.TargetMethod);
            lock (gate)
            {
                invocations.Add((invocation, context.ContainingSymbol));
                invocationOwners[invocation] = context.ContainingSymbol;
                Add(methodInvocations, target, invocation);
            }
        }

        public void AddMethodReference(OperationAnalysisContext context)
        {
            var reference = (IMethodReferenceOperation)context.Operation;
            lock (gate)
            {
                methodReferences.Add((reference, context.ContainingSymbol));
                Add(symbolReferences, Canonical(reference.Method), reference);
            }
        }

        public void AddLocalReference(OperationAnalysisContext context)
        {
            var reference = (ILocalReferenceOperation)context.Operation;
            if (IsAssignmentTarget(reference))
                return;
            lock (gate)
                Add(symbolReferences, reference.Local, reference);
        }

        public void Analyze(CompilationAnalysisContext context)
        {
            var diagnostics = new Dictionary<(SyntaxTree? Tree, int Start), Diagnostic>();
            var flowCache = new Dictionary<IOperation, ImmutableArray<Location>>();

            foreach (var (invocation, owner) in invocations)
            {
                var method = Canonical(invocation.TargetMethod);
                if (!RequiresImplicitComposer(method))
                    continue;

                var source = EnclosingDelegate(invocation);
                if (source is null)
                {
                    if (!IsComposableMethod(owner))
                    {
                        var failures = new List<Location>();
                        TraceMethodExecution(owner, invocation.Syntax.GetLocation(),
                            new TraceState(), failures, 0);
                        foreach (var location in failures)
                            AddDiagnostic(diagnostics, location, method.Name);
                    }
                    continue;
                }

                foreach (var location in TraceCached(source, flowCache))
                    AddDiagnostic(diagnostics, location, method.Name);
            }

            foreach (var (reference, _) in methodReferences)
            {
                var method = Canonical(reference.Method);
                if (!RequiresImplicitComposer(method))
                    continue;

                foreach (var location in TraceCached(reference, flowCache))
                    AddDiagnostic(diagnostics, location, method.Name);
            }

            foreach (var diagnostic in diagnostics.Values)
                context.ReportDiagnostic(diagnostic);
        }

        ImmutableArray<Location> TraceCached(
            IOperation source,
            Dictionary<IOperation, ImmutableArray<Location>> cache)
        {
            if (cache.TryGetValue(source, out var cached))
                return cached;

            var failures = new List<Location>();
            var state = new TraceState();
            TraceOperation(source, source.Syntax.GetLocation(), state, failures, 0);
            var result = failures
                .GroupBy(static location =>
                    (location.SourceTree, location.SourceSpan.Start))
                .Select(static group => group.First())
                .ToImmutableArray();
            cache[source] = result;
            return result;
        }

        void TraceOperation(
            IOperation operation,
            Location fallback,
            TraceState state,
            List<Location> failures,
            int depth)
        {
            if (!state.TryStep(depth))
            {
                failures.Add(fallback);
                return;
            }

            if (operation is ILocalFunctionOperation { Symbol.IsAsync: true } asyncLocal)
            {
                failures.Add(asyncLocal.Syntax.GetLocation());
                return;
            }

            if (operation is ILocalFunctionOperation localFunction)
            {
                TraceSymbol(localFunction.Symbol, fallback,
                    state, failures, depth + 1);
                return;
            }

            if (operation is IAnonymousFunctionOperation anonymous
                && anonymous.Symbol.IsAsync)
            {
                failures.Add(anonymous.Syntax.GetLocation());
                return;
            }

            IOperation current = operation;
            while (current.Parent is IDelegateCreationOperation
                or IConversionOperation
                or IParenthesizedOperation
                or IConditionalOperation
                or ICoalesceOperation)
            {
                if (current.Parent is IConversionOperation
                    {
                        OperatorMethod: { Parameters.Length: 1 } conversion
                    } userDefined)
                {
                    if (HasAttribute(
                            conversion.Parameters[0],
                            ComposableContentAttributeName))
                    {
                        return;
                    }

                    failures.Add(userDefined.Syntax.GetLocation());
                    return;
                }

                current = current.Parent;
            }

            switch (current.Parent)
            {
                case IArgumentOperation argument:
                    TraceArgument(argument, failures);
                    return;
                case IVariableInitializerOperation
                {
                    Parent: IVariableDeclaratorOperation declarator
                }:
                    if (declarator.Symbol is ILocalSymbol)
                    {
                        TraceSymbol(declarator.Symbol, declarator.Syntax.GetLocation(),
                            state, failures, depth + 1);
                    }
                    else
                    {
                        failures.Add(declarator.Syntax.GetLocation());
                    }
                    return;
                case IFieldInitializerOperation fieldInitializer:
                    failures.Add(fieldInitializer.InitializedFields
                        .SelectMany(static field => field.Locations)
                        .FirstOrDefault(static location => location.IsInSource)
                        ?? fieldInitializer.Syntax.GetLocation());
                    return;
                case ISimpleAssignmentOperation assignment
                    when ReferenceEquals(assignment.Value, current):
                    TraceAssignment(assignment, state, failures, depth + 1);
                    return;
                case IReturnOperation returned:
                    TraceReturn(returned, failures);
                    return;
                case IInvocationOperation invocation
                    when invocation.TargetMethod.MethodKind == MethodKind.DelegateInvoke:
                    TraceExecution(invocation, fallback,
                        state, failures, depth + 1);
                    return;
                default:
                    failures.Add(current.Syntax.GetLocation());
                    return;
            }
        }

        void TraceArgument(
            IArgumentOperation argument,
            List<Location> failures)
        {
            var parameter = argument.Parameter;
            if (parameter is null)
            {
                failures.Add(argument.Syntax.GetLocation());
                return;
            }

            if (HasAttribute(parameter, ComposableContentAttributeName))
                return;

            failures.Add(argument.Syntax.GetLocation());
        }

        void TraceAssignment(
            ISimpleAssignmentOperation assignment,
            TraceState state,
            List<Location> failures,
            int depth)
        {
            switch (assignment.Target)
            {
                case ILocalReferenceOperation local:
                    TraceSymbol(local.Local, assignment.Target.Syntax.GetLocation(),
                        state, failures, depth);
                    break;
                case IParameterReferenceOperation parameter:
                    TraceSymbol(parameter.Parameter, assignment.Target.Syntax.GetLocation(),
                        state, failures, depth);
                    break;
                default:
                    failures.Add(assignment.Target.Syntax.GetLocation());
                    break;
            }
        }

        void TraceReturn(
            IReturnOperation returned,
            List<Location> failures)
        {
            failures.Add(returned.Syntax.GetLocation());
        }

        void TraceSymbol(
            ISymbol symbol,
            Location fallback,
            TraceState state,
            List<Location> failures,
            int depth)
        {
            symbol = symbol is IMethodSymbol method ? Canonical(method) : symbol;
            if (!state.EnterSymbol(symbol, depth))
            {
                failures.Add(fallback);
                return;
            }

            try
            {
                bool foundUse = false;

                if (symbolReferences.TryGetValue(symbol, out var references))
                {
                    foreach (var reference in references)
                    {
                        foundUse = true;
                        TraceOperation(reference, reference.Syntax.GetLocation(),
                            state, failures, depth + 1);
                    }
                }

                if (symbol is IMethodSymbol usedMethod
                    && methodInvocations.TryGetValue(usedMethod, out var calls))
                {
                    foreach (var call in calls)
                    {
                        foundUse = true;
                        TraceExecution(call, fallback, state, failures, depth + 1);
                    }
                }

                if (!foundUse)
                    failures.Add(fallback);
            }
            finally
            {
                state.ExitSymbol(symbol);
            }
        }

        void TraceExecution(
            IInvocationOperation invocation,
            Location fallback,
            TraceState state,
            List<Location> failures,
            int depth)
        {
            var enclosing = EnclosingDelegate(invocation);
            if (enclosing is not null)
            {
                TraceOperation(enclosing, fallback, state, failures, depth);
                return;
            }

            if (!invocationOwners.TryGetValue(invocation, out var owner)
                || !IsComposableMethod(owner))
            {
                TraceMethodExecution(owner, invocation.Syntax.GetLocation(),
                    state, failures, depth + 1);
            }
        }

        void TraceMethodExecution(
            ISymbol? owner,
            Location fallback,
            TraceState state,
            List<Location> failures,
            int depth)
        {
            if (owner is not IMethodSymbol method
                || method.IsAsync
                || method.MethodKind != MethodKind.LocalFunction
                    && method.DeclaredAccessibility != Accessibility.Private
                || method.DeclaringSyntaxReferences.Length == 0)
            {
                failures.Add(fallback);
                return;
            }

            TraceSymbol(Canonical(method), fallback,
                state, failures, depth + 1);
        }

        static IOperation? EnclosingDelegate(IOperation operation)
        {
            for (var parent = operation.Parent; parent is not null; parent = parent.Parent)
            {
                if (parent is IAnonymousFunctionOperation
                    or ILocalFunctionOperation)
                {
                    return parent;
                }
            }

            return null;
        }

        static IMethodSymbol Canonical(IMethodSymbol method) =>
            method.ReducedFrom?.OriginalDefinition
            ?? method.OriginalDefinition;

        static bool IsAssignmentTarget(IOperation operation) =>
            operation.Parent is ISimpleAssignmentOperation assignment
            && ReferenceEquals(assignment.Target, operation);

        static void Add<TKey, TValue>(
            Dictionary<TKey, List<TValue>> dictionary,
            TKey key,
            TValue value)
            where TKey : notnull
        {
            if (!dictionary.TryGetValue(key, out var values))
                dictionary[key] = values = [];
            values.Add(value);
        }

        static void AddDiagnostic(
            Dictionary<(SyntaxTree? Tree, int Start), Diagnostic> diagnostics,
            Location location,
            string methodName)
        {
            var key = (location.SourceTree, location.SourceSpan.Start);
            if (!diagnostics.ContainsKey(key))
            {
                diagnostics[key] = Diagnostic.Create(
                    Diagnostics.ImplicitComposableOutsideScope,
                    location,
                    methodName);
            }
        }

        sealed class TraceState
        {
            readonly HashSet<ISymbol> symbols =
                new(SymbolEqualityComparer.Default);
            int steps;

            public bool TryStep(int depth) =>
                depth <= MaximumTraceDepth
                && ++steps <= MaximumTraceSteps;

            public bool EnterSymbol(ISymbol symbol, int depth) =>
                depth <= MaximumTraceDepth
                && ++steps <= MaximumTraceSteps
                && symbols.Add(symbol);

            public void ExitSymbol(ISymbol symbol) => symbols.Remove(symbol);
        }
    }
}
