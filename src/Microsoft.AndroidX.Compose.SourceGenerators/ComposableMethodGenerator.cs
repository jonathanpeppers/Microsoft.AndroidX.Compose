using System;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;

#pragma warning disable RSEXPERIMENTAL002 // GetInterceptableLocation — also used by dotnet/maui's BindingSourceGen

namespace AndroidX.Compose.SourceGenerators;

/// <summary>
/// Tier 2 source generator. Discovers every call site of a method
/// marked <c>[AndroidX.Compose.Composable]</c> and emits an
/// <c>[InterceptsLocation]</c>-decorated wrapper that opens a Compose
/// restart group, diffs each argument via <c>DiffSlot</c>, skips the
/// underlying call when nothing changed, and registers an
/// <c>UpdateScope</c> recompose lambda.
/// </summary>
/// <remarks>
/// <para>
/// The user-facing surface is a single plain C# method — no <c>partial</c>
/// modifier, no <c>Impl</c> companion. The same shape as a Kotlin
/// <c>@Composable</c> function:
/// </para>
/// <code>
/// [Composable]
/// public static void Greeting(string name)
/// {
///     Composables.Text($"Hello, {name}");
/// }
///
/// Greeting("world"); // ← intercepted; wrapped in a restart group
/// </code>
/// <para>
/// Mirrors the pattern used by <c>dotnet/maui</c>'s
/// <c>BindingSourceGen</c> — generator emits a <c>file</c>-scoped
/// <c>InterceptsLocationAttribute</c> definition once per generated
/// file, then one wrapper method per intercepted call site under
/// <c>Microsoft.AndroidX.Compose.Generated.ComposableInterceptors</c>.
/// </para>
/// </remarks>
[Generator(LanguageNames.CSharp)]
public sealed class ComposableMethodGenerator : IIncrementalGenerator
{
    const string ComposableAttributeMetadataName = "AndroidX.Compose.ComposableAttribute";
    const string ComposableDirectTargetAttributeMetadataName = "AndroidX.Compose.ComposableDirectTargetAttribute";
    const string ComposerFullName = "AndroidX.Compose.Runtime.IComposer";
    static readonly SymbolDisplayFormat ParameterTypeFormat =
        SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(
            SymbolDisplayFormat.FullyQualifiedFormat.MiscellaneousOptions
            | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier
            | SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers);
    const string GeneratedNamespace = "Microsoft.AndroidX.Compose.Generated";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // ---- Pipeline 1: validate every [Composable] method up front. ----
        var composableMethods = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                ComposableAttributeMetadataName,
                predicate: static (node, _) => node is MethodDeclarationSyntax,
                transform: static (ctx, _) => (IMethodSymbol)ctx.TargetSymbol)
            .Where(static m => m is not null)
            .Collect();

        context.RegisterSourceOutput(composableMethods, static (spc, methods) =>
        {
            foreach (var method in methods)
                ValidateComposable(method, spc);
        });

        // ---- Pipeline 2: every invocation in user code. ----
        // We don't filter by attribute presence in the syntax predicate
        // (the syntax tree alone can't tell us whether the target has
        // [Composable]) — that's a SemanticModel question handled in
        // the transform. The predicate is the cheap up-front filter.
        var callSites = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is InvocationExpressionSyntax,
                transform: static (ctx, ct) => TryBuildCallSite(ctx, ct))
            .Where(static cs => cs is not null)
            .Select(static (cs, _) => cs
                ?? throw new InvalidOperationException(
                    "Composable call-site filtering produced a null result."))
            .Collect();

        context.RegisterSourceOutput(callSites, static (spc, sites) =>
        {
            if (sites.IsDefaultOrEmpty)
                return;

            var sb = new StringBuilder();
            EmitPreamble(sb);
            // Stable ordering — by (FilePath, Version, Data) — so the
            // generator output is byte-identical between rebuilds when
            // nothing changed.
            var ordered = sites.OrderBy(s => s.FilePath, System.StringComparer.Ordinal)
                .ThenBy(s => s.LocationVersion)
                .ThenBy(s => s.LocationData, System.StringComparer.Ordinal)
                .ToList();

            int index = 0;
            foreach (var site in ordered)
                EmitInterceptor(sb, site, index++);

            EmitPostamble(sb);
            spc.AddSource("Microsoft.AndroidX.Compose.Composable.Interceptors.g.cs",
                SourceText.From(sb.ToString(), Encoding.UTF8));
        });
    }

    static void ValidateComposable(IMethodSymbol method, SourceProductionContext spc)
    {
        var loc = method.Locations.FirstOrDefault() ?? Location.None;

        if (!method.IsStatic)
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.ComposableNotStatic, loc, method.ToDisplayString()));
            return;
        }
        if (method.ReturnType.SpecialType != SpecialType.System_Void)
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.ComposableReturnsNotVoid, loc, method.ToDisplayString()));
            return;
        }
        int composerCount = method.Parameters.Count(static p => IsComposer(p.Type));
        if (composerCount > 0
            && (composerCount != 1 || !IsComposer(method.Parameters[0].Type)))
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.ComposableMissingComposer, loc, method.ToDisplayString()));
            return;
        }
        if (!IsAccessibleFromGeneratedType(method))
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.ComposableNotAccessible, loc, method.ToDisplayString()));
            return;
        }
        if (method.IsAsync)
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.ComposableAsyncUnsupported, loc, method.ToDisplayString()));
            return;
        }
        if (method.IsExtensionMethod)
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.ComposableExtensionUnsupported, loc, method.ToDisplayString()));
            return;
        }
        var byRefParameter = method.Parameters.FirstOrDefault(
            static p => p.RefKind != RefKind.None);
        if (byRefParameter is not null)
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.ComposableByRefUnsupported,
                loc,
                method.ToDisplayString(),
                byRefParameter.Name,
                byRefParameter.RefKind.ToString().ToLowerInvariant()));
        }
    }

    /// <summary>
    /// Pull the bits we need out of a candidate invocation: the
    /// resolved target symbol (so we can spell its containing type
    /// and parameter list verbatim in the emitted call), and the
    /// <see cref="InterceptableLocation"/> Roslyn computes for the
    /// call site. Returns <c>null</c> when the invocation doesn't
    /// resolve to a <c>[Composable]</c> target.
    /// </summary>
    static CallSite? TryBuildCallSite(GeneratorSyntaxContext ctx, System.Threading.CancellationToken ct)
    {
        var invocation = (InvocationExpressionSyntax)ctx.Node;

        // Skip invocations inside generator-emitted files so we don't
        // wrap our own wrapper's call to the user method (which would
        // cause infinite recursion). Roslyn doesn't expose this as a
        // first-class flag on syntax trees, but the file-path convention
        // ".g.cs" is universal.
        var path = invocation.SyntaxTree.FilePath;
        if (path is { Length: > 0 } && path.EndsWith(".g.cs", System.StringComparison.Ordinal))
            return null;

        var symbolInfo = ctx.SemanticModel.GetSymbolInfo(invocation, ct);
        if (symbolInfo.Symbol is not IMethodSymbol target)
            return null;

        // Lookup [Composable] by metadata name on the resolved method.
        // Reduced (extension-method-style) calls resolve to the reduced
        // form; the attribute lives on the original definition.
        var actual = target.ReducedFrom ?? target.OriginalDefinition;
        if (!HasComposableAttribute(actual))
            return null;

        // ValidateComposable reports the diagnostic on the declaration;
        // this guard keeps unsupported targets out of generated code.
        if (!CanEmitInterceptor(actual))
            return null;

        var loc = ctx.SemanticModel.GetInterceptableLocation(invocation, ct);
        if (loc is null)
            return null;

        var directTarget = TryReadDirectTarget(actual);
        ulong omittedArguments = directTarget is null
            ? 0
            : ReadOmittedArguments(ctx.SemanticModel, invocation, target, ct);

        return new CallSite(
            target,
            path ?? string.Empty,
            loc.Version,
            loc.Data,
            directTarget,
            omittedArguments);
    }

    static DirectTarget? TryReadDirectTarget(IMethodSymbol method)
    {
        var attr = method.GetAttributes().FirstOrDefault(static a =>
            a.AttributeClass?.ToDisplayString() == ComposableDirectTargetAttributeMetadataName);
        if (attr is null || attr.ConstructorArguments.Length != 2)
            return null;
        if (attr.ConstructorArguments[0].Value is not INamedTypeSymbol containingType)
            return null;
        if (attr.ConstructorArguments[1].Value is not string methodName
            || string.IsNullOrWhiteSpace(methodName))
            return null;
        return new DirectTarget(containingType, methodName);
    }

    static ulong ReadOmittedArguments(
        SemanticModel semanticModel,
        InvocationExpressionSyntax invocation,
        IMethodSymbol target,
        System.Threading.CancellationToken ct)
    {
        if (semanticModel.GetOperation(invocation, ct) is not IInvocationOperation operation)
            return 0;

        bool hasExplicitComposer = target.Parameters.Length > 0
            && IsComposer(target.Parameters[0].Type);
        int composerOffset = hasExplicitComposer ? 1 : 0;
        ulong omitted = 0;
        foreach (var argument in operation.Arguments)
        {
            if (argument.ArgumentKind != ArgumentKind.DefaultValue
                || argument.Parameter is not { } parameter)
                continue;
            int userIndex = parameter.Ordinal - composerOffset;
            if ((uint)userIndex < 64)
                omitted |= 1UL << userIndex;
        }
        return omitted;
    }

    static bool HasComposableAttribute(IMethodSymbol method)
    {
        foreach (var attr in method.GetAttributes())
        {
            if (attr.AttributeClass?.ToDisplayString() == ComposableAttributeMetadataName)
                return true;
        }
        return false;
    }

    static bool IsComposer(ITypeSymbol type) =>
        type.ToDisplayString() == ComposerFullName;

    static string? GetObsoleteDiagnosticId(ISymbol symbol)
    {
        for (ISymbol? current = symbol; current is not null;
             current = current.ContainingType)
        {
            var obsolete = current.GetAttributes().FirstOrDefault(
                static attribute =>
                    attribute.AttributeClass?.ToDisplayString()
                        == "System.ObsoleteAttribute");
            if (obsolete is not null)
            {
                foreach (var named in obsolete.NamedArguments)
                {
                    if (named.Key == "DiagnosticId"
                        && named.Value.Value is string diagnosticId
                        && diagnosticId.Length > 0)
                    {
                        return diagnosticId;
                    }
                }
                return "CS0618";
            }
        }
        return null;
    }

    static bool CanEmitInterceptor(IMethodSymbol method) =>
        method.IsStatic &&
        method.ReturnType.SpecialType == SpecialType.System_Void &&
        (method.Parameters.Count(static p => IsComposer(p.Type)) == 0 ||
            (method.Parameters.Count(static p => IsComposer(p.Type)) == 1 &&
                IsComposer(method.Parameters[0].Type))) &&
        IsAccessibleFromGeneratedType(method) &&
        !method.IsAsync &&
        !method.IsExtensionMethod &&
        !method.Parameters.Any(static p => p.RefKind != RefKind.None);

    static bool IsAccessibleFromGeneratedType(IMethodSymbol method)
    {
        if (!IsAssemblyAccessible(method.DeclaredAccessibility))
            return false;

        for (var type = method.ContainingType; type is not null; type = type.ContainingType)
        {
            if (type.IsFileLocal || !IsAssemblyAccessible(type.DeclaredAccessibility))
                return false;
        }
        return true;
    }

    static bool IsAssemblyAccessible(Accessibility accessibility) =>
        accessibility is Accessibility.Public
            or Accessibility.Internal
            or Accessibility.ProtectedOrInternal;

    static void EmitPreamble(StringBuilder sb)
    {
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("namespace System.Runtime.CompilerServices");
        sb.AppendLine("{");
        sb.AppendLine("    using System;");
        sb.AppendLine("    using System.Diagnostics;");
        sb.AppendLine();
        sb.AppendLine("    [Conditional(\"DEBUG\")]");
        sb.AppendLine("    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]");
        sb.AppendLine("    file sealed class InterceptsLocationAttribute : Attribute");
        sb.AppendLine("    {");
        sb.AppendLine("        public InterceptsLocationAttribute(int version, string data)");
        sb.AppendLine("        {");
        sb.AppendLine("            _ = version;");
        sb.AppendLine("            _ = data;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.Append("namespace ").Append(GeneratedNamespace).AppendLine();
        sb.AppendLine("{");
        sb.AppendLine("    using global::AndroidX.Compose;");
        sb.AppendLine("    using global::AndroidX.Compose.Runtime;");
        sb.AppendLine();
        sb.AppendLine("    /// <summary>Generated wrappers for [Composable] call sites.</summary>");
        sb.AppendLine("    internal static class ComposableInterceptors");
        sb.AppendLine("    {");
    }

    static void EmitPostamble(StringBuilder sb)
    {
        sb.AppendLine("    }");
        sb.AppendLine("}");
    }

    /// <summary>
    /// Emit one wrapper per intercepted call site. The wrapper opens a
    /// restart group keyed by the target method's FQN, runs per-param
    /// <c>DiffSlot</c> into <c>__dirty</c>, branches on the canonical
    /// skip check, and registers the recompose lambda.
    /// </summary>
    static void EmitInterceptor(StringBuilder sb, CallSite site, int index)
    {
        // Keep substitutions from a constructed containing type
        // (e.g. Screens<string>) while reopening only this method's own
        // generic parameters for the interceptor signature.
        var method = site.Target.ConstructedFrom;
        var interceptorTypeParameters = GetInterceptorTypeParameters(method);
        var typeParameterNames = BuildTypeParameterNames(interceptorTypeParameters);
        var containingType = method.ContainingType
            ?? throw new InvalidOperationException(
                $"Composable method '{method.Name}' has no containing type.");
        var methodFqn = containingType.ToDisplayString() + "." + method.Name;
        int key = FnvHash(methodFqn);
        bool hasExplicitComposer = method.Parameters.Length > 0
            && IsComposer(method.Parameters[0].Type);
        string? obsoleteDiagnosticId = GetObsoleteDiagnosticId(method);

        // Same Kotlin-shape mask/expected pair used by the previous
        // generator revision — `0b001` is the runtime "force" bit, each
        // user param contributes `0b101 << (1+3*i)` to the mask and
        // `0b001 << (1+3*i)` to the expected value. Skip when
        // (__dirty & mask) == expected && composer.Skipping.
        var userParams = method.Parameters.Skip(hasExplicitComposer ? 1 : 0).ToList();
        int trackedParamCount = Math.Min(userParams.Count, 10);
        long mask = 0b001;
        long expected = 0;
        for (int i = 0; i < trackedParamCount; i++)
        {
            int shift = 1 + i * 3;
            mask |= 0b101L << shift;
            expected |= 0b001L << shift;
        }

        string wrapperName = "Composable_" + index.ToString(CultureInfo.InvariantCulture)
            + "_" + FnvHash(site.LocationData + "|" + site.LocationVersion)
                .ToString("X8", CultureInfo.InvariantCulture);
        string coreName = wrapperName + "_Core";

        sb.AppendLine();
        sb.Append("        // ").Append(method.ToDisplayString()).AppendLine();
        sb.Append("        // site: ").Append(site.FilePath).AppendLine();
        sb.Append("        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute(")
          .Append(site.LocationVersion.ToString(CultureInfo.InvariantCulture))
          .Append(", @\"")
          .Append(site.LocationData.Replace("\"", "\"\""))
          .AppendLine("\")]");

        // Signature mirrors the target verbatim. Use FullyQualifiedFormat
        // so a parameter type from any namespace lands as `global::...`.
        sb.Append("        public static void ").Append(wrapperName);
        AppendTypeParameters(sb, interceptorTypeParameters, typeParameterNames);
        sb.Append('(');
        for (int i = 0; i < method.Parameters.Length; i++)
        {
            if (i > 0) sb.Append(", ");
            var p = method.Parameters[i];
            sb.Append(DisplayType(p.Type, typeParameterNames))
              .Append(' ').Append(EscapeIdentifier(p.Name));
        }
        sb.AppendLine(")");
        AppendTypeParameterConstraints(
            sb, interceptorTypeParameters, typeParameterNames, "        ");
        sb.AppendLine("        {");
        sb.Append("            ").Append(coreName);
        AppendTypeArguments(sb, interceptorTypeParameters, typeParameterNames);
        sb.Append('(');
        if (!hasExplicitComposer)
            sb.Append("global::AndroidX.Compose.ComposableContext.Current");
        for (int i = 0; i < method.Parameters.Length; i++)
        {
            if (i > 0 || !hasExplicitComposer) sb.Append(", ");
            sb.Append(EscapeIdentifier(method.Parameters[i].Name));
        }
        sb.AppendLine(", 0);");
        sb.AppendLine("        }");
        sb.AppendLine();

        sb.Append("        static void ").Append(coreName);
        AppendTypeParameters(sb, interceptorTypeParameters, typeParameterNames);
        sb.Append('(');
        if (!hasExplicitComposer)
            sb.Append("global::AndroidX.Compose.Runtime.IComposer __composer");
        for (int i = 0; i < method.Parameters.Length; i++)
        {
            if (i > 0 || !hasExplicitComposer) sb.Append(", ");
            var p = method.Parameters[i];
            sb.Append(DisplayType(p.Type, typeParameterNames))
              .Append(' ').Append(EscapeIdentifier(p.Name));
        }
        sb.Append(", ");
        sb.AppendLine("int __changed)");
        AppendTypeParameterConstraints(
            sb, interceptorTypeParameters, typeParameterNames, "        ");
        sb.AppendLine("        {");

        var composerName = hasExplicitComposer
            ? EscapeIdentifier(method.Parameters[0].Name)
            : "__composer";
        sb.Append("            var __c = ").Append(composerName)
          .Append(".StartRestartGroup(unchecked((int)0x")
          .Append(key.ToString("X8", CultureInfo.InvariantCulture))
          .AppendLine("));");
        sb.AppendLine("            using var __composerScope = global::AndroidX.Compose.ComposableContext.Enter(__c);");

        sb.AppendLine("            int __dirty = __changed;");
        for (int i = 0; i < trackedParamCount; i++)
        {
            int bitOffset = 1 + i * 3;
            var p = userParams[i];
            if (IsUnstableCollection(p.Type))
            {
                // Mutable collections commonly preserve reference identity
                // across in-place edits. Never skip solely because the list
                // object compares equal to its previous reference.
                sb.AppendLine("            __dirty |= 0b1;");
            }
            else
            {
                sb.Append("            __dirty |= __c.DiffSlot<")
                  .Append(DisplayType(p.Type, typeParameterNames))
                  .Append(">(").Append(EscapeIdentifier(p.Name)).Append(", ")
                  .Append(bitOffset.ToString(CultureInfo.InvariantCulture))
                  .AppendLine(");");
            }
        }
        if (userParams.Count > trackedParamCount)
            sb.AppendLine("            __dirty |= 0b1;");

        if (userParams.Count == 0)
        {
            sb.AppendLine("            if ((__dirty & 0x1) != 0 || !__c.Skipping)");
        }
        else
        {
            sb.Append("            if ((__dirty & 0x")
              .Append(mask.ToString("X", CultureInfo.InvariantCulture))
              .Append(") != 0x")
              .Append(expected.ToString("X", CultureInfo.InvariantCulture))
              .AppendLine(" || !__c.Skipping)");
        }
        sb.AppendLine("            {");
        // Call the user's original method by fully-qualified name. This call
        // site has no [InterceptsLocation], so it reaches the method body.
        if (obsoleteDiagnosticId is not null)
            sb.Append("                #pragma warning disable ")
              .AppendLine(obsoleteDiagnosticId);
        sb.Append("                ");
        if (site.DirectTarget is not null)
        {
            sb.Append(site.DirectTarget.ContainingType.ToDisplayString(
                    SymbolDisplayFormat.FullyQualifiedFormat))
              .Append('.')
              .Append(EscapeIdentifier(site.DirectTarget.MethodName));
        }
        else
        {
            sb.Append(DisplayType(containingType, typeParameterNames))
              .Append('.')
              .Append(EscapeIdentifier(method.Name));
            AppendTypeArguments(sb, method.TypeParameters, typeParameterNames);
        }
        sb.Append('(');
        bool hasCallArgument = false;
        if (site.DirectTarget is not null || hasExplicitComposer)
        {
            sb.Append("__c");
            hasCallArgument = true;
        }
        foreach (var p in userParams)
        {
            if (hasCallArgument)
                sb.Append(", ");
            sb.Append(EscapeIdentifier(p.Name));
            hasCallArgument = true;
        }
        if (site.DirectTarget is not null)
        {
            if (hasCallArgument)
                sb.Append(", ");
            sb.Append("0x")
              .Append(site.OmittedArguments.ToString("X", CultureInfo.InvariantCulture))
              .Append("UL, __dirty");
        }
        sb.AppendLine(");");
        if (obsoleteDiagnosticId is not null)
            sb.Append("                #pragma warning restore ")
              .AppendLine(obsoleteDiagnosticId);
        sb.AppendLine("            }");
        sb.AppendLine("            else");
        sb.AppendLine("            {");
        sb.AppendLine("                __c.SkipToGroupEnd();");
        sb.AppendLine("            }");

        // Recompose path. The lambda re-enters THIS wrapper (not the
        // user method) so the restart group re-opens, args re-diff,
        // skip-or-call fires the same way.
        sb.Append("            __c.EndRestartGroup()?.UpdateScope(new global::AndroidX.Compose.ComposableLambda2((__c2, __force) => ")
          .Append(coreName);
        AppendTypeArguments(sb, interceptorTypeParameters, typeParameterNames);
        sb.Append("(__c2");
        foreach (var p in userParams)
            sb.Append(", ").Append(EscapeIdentifier(p.Name));
        sb.AppendLine(", __force | 0b1)));");

        sb.AppendLine("        }");
    }

    static System.Collections.Generic.IReadOnlyList<ITypeParameterSymbol>
        GetInterceptorTypeParameters(IMethodSymbol method)
    {
        var result = new System.Collections.Generic.List<ITypeParameterSymbol>();
        var containingTypes = new System.Collections.Generic.Stack<INamedTypeSymbol>();
        for (var type = method.ContainingType; type is not null; type = type.ContainingType)
            containingTypes.Push(type);
        foreach (var containingType in containingTypes)
        {
            foreach (var typeArgument in containingType.TypeArguments)
                CollectTypeParameters(typeArgument, result);
        }
        foreach (var typeParameter in method.TypeParameters)
            AddTypeParameter(typeParameter, result);
        for (int i = 0; i < result.Count; i++)
        {
            foreach (var constraintType in result[i].ConstraintTypes)
                CollectTypeParameters(constraintType, result);
        }
        return result;
    }

    static void CollectTypeParameters(
        ITypeSymbol type,
        System.Collections.Generic.List<ITypeParameterSymbol> result)
    {
        if (type is ITypeParameterSymbol typeParameter)
        {
            AddTypeParameter(typeParameter, result);
            return;
        }

        if (type is IArrayTypeSymbol array)
        {
            CollectTypeParameters(array.ElementType, result);
            return;
        }

        if (type is INamedTypeSymbol named)
        {
            foreach (var typeArgument in named.TypeArguments)
                CollectTypeParameters(typeArgument, result);
        }
    }

    static void AddTypeParameter(
        ITypeParameterSymbol typeParameter,
        System.Collections.Generic.List<ITypeParameterSymbol> result)
    {
        if (!result.Any(existing =>
                SymbolEqualityComparer.Default.Equals(existing, typeParameter)))
        {
            result.Add(typeParameter);
        }
    }

    static bool IsUnstableCollection(ITypeSymbol type)
    {
        if (type.SpecialType == SpecialType.System_String)
            return false;
        if (type is ITypeParameterSymbol typeParameter)
        {
            // An unconstrained/reference-constrained T can be instantiated
            // with a mutable collection even when its declaration does not
            // expose collection interfaces. Value-constrained T is copied
            // and remains safe for EqualityComparer<T>.Default diffing.
            return !typeParameter.HasValueTypeConstraint
                && !typeParameter.HasUnmanagedTypeConstraint;
        }
        if (type.SpecialType == SpecialType.System_Collections_IEnumerable)
            return true;
        if (type is IArrayTypeSymbol)
            return true;
        return type.AllInterfaces.Any(static i =>
            i.SpecialType == SpecialType.System_Collections_IEnumerable);
    }

    static System.Collections.Generic.Dictionary<ITypeParameterSymbol, string>
        BuildTypeParameterNames(
            System.Collections.Generic.IReadOnlyList<ITypeParameterSymbol> typeParameters)
    {
        var result =
            new System.Collections.Generic.Dictionary<ITypeParameterSymbol, string>(
                SymbolEqualityComparer.Default);
        var used = new System.Collections.Generic.HashSet<string>(
            System.StringComparer.Ordinal);
        foreach (var typeParameter in typeParameters)
        {
            string name = typeParameter.Name;
            int suffix = 1;
            while (!used.Add(name))
            {
                name = typeParameter.Name + "_"
                    + suffix.ToString(CultureInfo.InvariantCulture);
                suffix++;
            }
            result.Add(typeParameter, name);
        }
        return result;
    }

    static string DisplayType(
        ITypeSymbol type,
        System.Collections.Generic.IReadOnlyDictionary<ITypeParameterSymbol, string>
            typeParameterNames)
    {
        var sb = new StringBuilder();
        foreach (var part in type.ToDisplayParts(ParameterTypeFormat))
        {
            if (part.Kind == SymbolDisplayPartKind.TypeParameterName
                && part.Symbol is ITypeParameterSymbol typeParameter
                && typeParameterNames.TryGetValue(typeParameter, out var generatedName))
            {
                sb.Append(EscapeIdentifier(generatedName));
            }
            else
            {
                sb.Append(part.ToString());
            }
        }
        return sb.ToString();
    }

    static void AppendTypeParameters(
        StringBuilder sb,
        System.Collections.Generic.IReadOnlyList<ITypeParameterSymbol> typeParameters,
        System.Collections.Generic.IReadOnlyDictionary<ITypeParameterSymbol, string>
            typeParameterNames)
    {
        if (typeParameters.Count == 0)
            return;

        sb.Append('<');
        for (int i = 0; i < typeParameters.Count; i++)
        {
            if (i > 0)
                sb.Append(", ");
            sb.Append(EscapeIdentifier(typeParameterNames[typeParameters[i]]));
        }
        sb.Append('>');
    }

    static void AppendTypeArguments(
        StringBuilder sb,
        System.Collections.Generic.IReadOnlyList<ITypeParameterSymbol> typeParameters,
        System.Collections.Generic.IReadOnlyDictionary<ITypeParameterSymbol, string>
            typeParameterNames) =>
        AppendTypeParameters(sb, typeParameters, typeParameterNames);

    static void AppendTypeParameterConstraints(
        StringBuilder sb,
        System.Collections.Generic.IReadOnlyList<ITypeParameterSymbol> typeParameters,
        System.Collections.Generic.IReadOnlyDictionary<ITypeParameterSymbol, string>
            typeParameterNames,
        string indent)
    {
        foreach (var typeParameter in typeParameters)
        {
            var constraints = new System.Collections.Generic.List<string>();
            if (typeParameter.HasUnmanagedTypeConstraint)
            {
                constraints.Add("unmanaged");
            }
            else if (typeParameter.HasValueTypeConstraint)
            {
                constraints.Add("struct");
            }
            else if (typeParameter.HasReferenceTypeConstraint)
            {
                constraints.Add(
                    typeParameter.ReferenceTypeConstraintNullableAnnotation
                        == NullableAnnotation.Annotated
                            ? "class?"
                            : "class");
            }
            else if (typeParameter.HasNotNullConstraint)
            {
                constraints.Add("notnull");
            }

            foreach (var constraintType in typeParameter.ConstraintTypes)
                constraints.Add(DisplayType(constraintType, typeParameterNames));

            if (typeParameter.HasConstructorConstraint)
                constraints.Add("new()");

            if (constraints.Count == 0)
                continue;

            sb.Append(indent)
              .Append("    where ")
              .Append(EscapeIdentifier(typeParameterNames[typeParameter]))
              .Append(" : ")
              .AppendLine(string.Join(", ", constraints));
        }
    }

    static int FnvHash(string s)
    {
        const uint offset = 2166136261u;
        const uint prime = 16777619u;
        uint hash = offset;
        for (int i = 0; i < s.Length; i++)
        {
            hash ^= s[i];
            hash *= prime;
        }

        return unchecked((int)hash);
    }

    static string EscapeIdentifier(string name) =>
        SyntaxFacts.GetKeywordKind(name) != SyntaxKind.None
            || SyntaxFacts.GetContextualKeywordKind(name) != SyntaxKind.None
            ? "@" + name
            : name;

    /// <summary>
    /// One intercepted call site — the resolved target method plus the
    /// <see cref="InterceptableLocation"/> data the compiler needs to
    /// rewire that exact call.
    /// </summary>
    sealed class CallSite
    {
        public CallSite(IMethodSymbol target, string filePath, int locationVersion, string locationData,
            DirectTarget? directTarget, ulong omittedArguments)
        {
            Target = target;
            FilePath = filePath;
            LocationVersion = locationVersion;
            LocationData = locationData;
            DirectTarget = directTarget;
            OmittedArguments = omittedArguments;
        }

        public IMethodSymbol Target { get; }
        public string FilePath { get; }
        public int LocationVersion { get; }
        public string LocationData { get; }
        public DirectTarget? DirectTarget { get; }
        public ulong OmittedArguments { get; }
    }

    sealed class DirectTarget
    {
        public DirectTarget(INamedTypeSymbol containingType, string methodName)
        {
            ContainingType = containingType;
            MethodName = methodName;
        }

        public INamedTypeSymbol ContainingType { get; }
        public string MethodName { get; }
    }
}
