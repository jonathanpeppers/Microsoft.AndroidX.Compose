using System;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
        if (method.IsGenericMethod)
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.ComposableGenericUnsupported, loc, method.ToDisplayString()));
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

        return new CallSite(
            target,
            path ?? string.Empty,
            loc.Version,
            loc.Data);
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

    static bool CanEmitInterceptor(IMethodSymbol method) =>
        method.IsStatic &&
        method.ReturnType.SpecialType == SpecialType.System_Void &&
        (method.Parameters.Count(static p => IsComposer(p.Type)) == 0 ||
            (method.Parameters.Count(static p => IsComposer(p.Type)) == 1 &&
                IsComposer(method.Parameters[0].Type))) &&
        IsAccessibleFromGeneratedType(method) &&
        !method.IsAsync &&
        !method.IsExtensionMethod &&
        !method.IsGenericMethod &&
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
        var method = site.Target;
        var containingType = method.ContainingType
            ?? throw new InvalidOperationException(
                $"Composable method '{method.Name}' has no containing type.");
        var methodFqn = containingType.ToDisplayString() + "." + method.Name;
        int key = FnvHash(methodFqn);
        bool hasExplicitComposer = method.Parameters.Length > 0
            && IsComposer(method.Parameters[0].Type);

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
        sb.Append("        public static void ").Append(wrapperName).Append('(');
        for (int i = 0; i < method.Parameters.Length; i++)
        {
            if (i > 0) sb.Append(", ");
            var p = method.Parameters[i];
            sb.Append(p.Type.ToDisplayString(ParameterTypeFormat))
              .Append(' ').Append(EscapeIdentifier(p.Name));
        }
        sb.AppendLine(")");
        sb.AppendLine("        {");
        sb.Append("            ").Append(coreName).Append('(');
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

        sb.Append("        static void ").Append(coreName).Append('(');
        if (!hasExplicitComposer)
            sb.Append("global::AndroidX.Compose.Runtime.IComposer __composer");
        for (int i = 0; i < method.Parameters.Length; i++)
        {
            if (i > 0 || !hasExplicitComposer) sb.Append(", ");
            var p = method.Parameters[i];
            sb.Append(p.Type.ToDisplayString(ParameterTypeFormat))
              .Append(' ').Append(EscapeIdentifier(p.Name));
        }
        sb.Append(", ");
        sb.AppendLine("int __changed)");
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
            sb.Append("            __dirty |= __c.DiffSlot<")
              .Append(p.Type.ToDisplayString(ParameterTypeFormat))
              .Append(">(").Append(EscapeIdentifier(p.Name)).Append(", ")
              .Append(bitOffset.ToString(CultureInfo.InvariantCulture))
              .AppendLine(");");
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
        // Call the user's original method by fully-qualified name. This
        // call site itself doesn't have [InterceptsLocation] pointing at
        // it, so it passes through to the user method's body (which is
        // what we want — that's the actual composable's body running).
        sb.Append("                ")
          .Append(containingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
          .Append('.')
          .Append(EscapeIdentifier(method.Name))
          .Append('(');
        if (hasExplicitComposer)
            sb.Append("__c");
        bool hasCallArgument = hasExplicitComposer;
        foreach (var p in userParams)
        {
            if (hasCallArgument)
                sb.Append(", ");
            sb.Append(EscapeIdentifier(p.Name));
            hasCallArgument = true;
        }
        sb.AppendLine(");");
        sb.AppendLine("            }");
        sb.AppendLine("            else");
        sb.AppendLine("            {");
        sb.AppendLine("                __c.SkipToGroupEnd();");
        sb.AppendLine("            }");

        // Recompose path. The lambda re-enters THIS wrapper (not the
        // user method) so the restart group re-opens, args re-diff,
        // skip-or-call fires the same way.
        sb.Append("            __c.EndRestartGroup()?.UpdateScope(new global::AndroidX.Compose.ComposableLambda2((__c2, __force) => ")
          .Append(coreName).Append("(__c2");
        foreach (var p in userParams)
            sb.Append(", ").Append(EscapeIdentifier(p.Name));
        sb.AppendLine(", __force | 0b1)));");

        sb.AppendLine("        }");
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
        public CallSite(IMethodSymbol target, string filePath, int locationVersion, string locationData)
        {
            Target = target;
            FilePath = filePath;
            LocationVersion = locationVersion;
            LocationData = locationData;
        }

        public IMethodSymbol Target { get; }
        public string FilePath { get; }
        public int LocationVersion { get; }
        public string LocationData { get; }
    }
}
