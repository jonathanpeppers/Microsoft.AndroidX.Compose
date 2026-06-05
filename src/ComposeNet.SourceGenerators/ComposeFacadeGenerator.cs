using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace ComposeNet.SourceGenerators;

/// <summary>
/// Emits user-facing facade classes from <c>[ComposeFacade]</c>-decorated
/// bridge methods on <c>ComposeNet.ComposeBridges</c>. The bridge already
/// owns the JNI plumbing; the facade is a thin
/// <see cref="ComposableNode"/> / <see cref="ComposableContainer"/>
/// wrapper that builds the bridge's user-controlled args (Action →
/// <c>ComposableLambda0</c>, modifier → <c>BuildModifier()</c>, content
/// → <c>ComposableLambdas.Wrap2/3</c>) and forwards through.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class ComposeFacadeGenerator : IIncrementalGenerator
{
    const string FacadeAttributeMetadataName = "ComposeNet.ComposeFacadeAttribute";
    const string BridgeAttributeMetadataName = "ComposeNet.ComposeBridgeAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Attributes.cs (shared) emits ComposeFacadeAttribute via
        // ComposeDefaultsGenerator.RegisterPostInitializationOutput.

        var methods = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) =>
                    node is MethodDeclarationSyntax m &&
                    m.Modifiers.Any(t => t.IsKind(SyntaxKind.PartialKeyword)) &&
                    m.AttributeLists.Count > 0,
                transform: static (ctx, _) => ctx)
            .Where(static ctx => ctx.SemanticModel.GetDeclaredSymbol((MethodDeclarationSyntax)ctx.Node) is IMethodSymbol);

        var combined = methods.Combine(context.CompilationProvider);

        context.RegisterSourceOutput(combined, static (spc, pair) =>
        {
            var (ctx, compilation) = pair;
            var facadeAttr = compilation.GetTypeByMetadataName(FacadeAttributeMetadataName);
            var bridgeAttr = compilation.GetTypeByMetadataName(BridgeAttributeMetadataName);
            if (facadeAttr is null) return;

            var method = (IMethodSymbol)ctx.SemanticModel.GetDeclaredSymbol((MethodDeclarationSyntax)ctx.Node)!;
            var attr = method.GetAttributes()
                .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, facadeAttr));
            if (attr is null) return;

            var result = Build(method, attr, bridgeAttr);
            foreach (var diag in result.Diagnostics)
                spc.ReportDiagnostic(diag);
            if (result.Source is { } source && result.HintName is { } hint)
                spc.AddSource(hint, SourceText.From(source, Encoding.UTF8));
        });
    }

    static GenerationResult Build(IMethodSymbol method, AttributeData attr, INamedTypeSymbol? bridgeAttr)
    {
        var loc = attr.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? Location.None;

        // CN3001 — facade can only attach to ComposeBridges methods. The
        // emitter assumes the bridge call target is
        // global::ComposeNet.ComposeBridges.<name>.
        var container = method.ContainingType;
        if (container.Name != "ComposeBridges" ||
            container.ContainingNamespace?.ToDisplayString() != "ComposeNet")
        {
            return Fail(Diagnostics.FacadeWrongContainingType, loc, method.Name, container.ToDisplayString());
        }

        // CN3004 — facade is a thin wrapper around the bridge body; the
        // bridge attribute must be present or the bridge call won't compile.
        if (bridgeAttr is not null &&
            !method.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, bridgeAttr)))
        {
            return Fail(Diagnostics.FacadeMissingBridge, loc, method.Name);
        }

        string className = ReadString(attr, "ClassName") ?? method.Name;
        string? scope = ReadString(attr, "Scope");
        string? summary = ReadString(attr, "Summary");

        // Identify the composer slot (must be the trailing parameter for
        // @Composable bridges — the only shape facade generation supports).
        var csParams = method.Parameters;
        if (csParams.Length == 0 || !ComposeDefaultsGenerator.IsComposer(csParams[csParams.Length - 1].Type))
        {
            return Fail(Diagnostics.FacadeUnsupportedParameter, loc, method.Name, "<composer>",
                "bridge must be @Composable (trailing IComposer parameter) for facade generation");
        }
        var composerParam = csParams[csParams.Length - 1];
        var userParams = csParams.Take(csParams.Length - 1).ToArray();

        // Walk user params and classify each. The classifier rejects
        // anything outside the documented Phase 1 shapes (e.g. IFunction1
        // callbacks, value-class handles, manual `int defaults` hatch).
        var slots = new List<FacadeSlot>(userParams.Length);
        IParameterSymbol? modifierParam = null;
        IParameterSymbol? contentParam = null;
        int contentArity = 0; // 2 = Wrap2, 3 = Wrap3
        var diags = new List<Diagnostic>();

        foreach (var p in userParams)
        {
            // The bridge generator treats a trailing `int defaults` (just
            // before composer) as a caller-controlled $default mask. Surface
            // would leak that low-level knob into the public ctor, so we
            // refuse to generate a facade for it.
            if (p.Name == "defaults" && p.Type.SpecialType == SpecialType.System_Int32)
            {
                diags.Add(Diagnostic.Create(Diagnostics.FacadeUnsupportedParameter, loc,
                    method.Name, p.Name, "int (caller-controlled $default mask)"));
                continue;
            }

            if (IsModifier(p.Type))
            {
                if (modifierParam is not null)
                {
                    diags.Add(Diagnostic.Create(Diagnostics.FacadeUnsupportedParameter, loc,
                        method.Name, p.Name, "duplicate IModifier? parameter"));
                    continue;
                }
                modifierParam = p;
                slots.Add(new FacadeSlot(p, FacadeSlotKind.Modifier));
                continue;
            }

            var funcArity = KotlinFunctionArity(p.Type);
            if (funcArity == 0)
            {
                slots.Add(new FacadeSlot(p, FacadeSlotKind.OnClick));
                continue;
            }
            if (funcArity == 2 || funcArity == 3)
            {
                if (contentParam is not null)
                {
                    diags.Add(Diagnostic.Create(Diagnostics.FacadeUnsupportedParameter, loc,
                        method.Name, p.Name, $"multiple content lambdas (IFunction{funcArity}) — only a single content slot is supported"));
                    continue;
                }
                contentParam = p;
                contentArity = funcArity;
                slots.Add(new FacadeSlot(p, funcArity == 2 ? FacadeSlotKind.Content2 : FacadeSlotKind.Content3));
                continue;
            }
            if (funcArity > 0)
            {
                diags.Add(Diagnostic.Create(Diagnostics.FacadeUnsupportedParameter, loc,
                    method.Name, p.Name, $"IFunction{funcArity} (callback / non-content lambda)"));
                continue;
            }

            if (IsPrimitiveCtorType(p.Type))
            {
                slots.Add(new FacadeSlot(p, FacadeSlotKind.Primitive));
                continue;
            }

            diags.Add(Diagnostic.Create(Diagnostics.FacadeUnsupportedParameter, loc,
                method.Name, p.Name, p.Type.ToDisplayString()));
        }

        if (!string.IsNullOrEmpty(scope) && contentArity != 3)
        {
            diags.Add(Diagnostic.Create(Diagnostics.FacadeScopeMisuse, loc, method.Name, scope ?? "?"));
        }

        if (!string.IsNullOrEmpty(scope) && scope != "Row" && scope != "Column")
        {
            diags.Add(Diagnostic.Create(Diagnostics.FacadeScopeMisuse, loc, method.Name, scope!));
        }

        if (diags.Count > 0)
            return new GenerationResult(null, null, diags);

        var source = Emit(className, method.Name, summary, scope, composerParam, slots, contentArity, modifierParam is not null);
        var hint = $"ComposeNet.Facade.{className}.g.cs";
        return new GenerationResult(source, hint, Array.Empty<Diagnostic>());
    }

    static string Emit(string className, string bridgeMethodName, string? summary, string? scope,
        IParameterSymbol composerParam, IReadOnlyList<FacadeSlot> slots,
        int contentArity, bool hasModifier)
    {
        bool isContainer = contentArity != 0;
        string baseClass = isContainer ? "global::ComposeNet.ComposableContainer" : "global::ComposeNet.ComposableNode";

        var ctorSlots = slots.Where(s => s.Kind is FacadeSlotKind.OnClick or FacadeSlotKind.Primitive).ToArray();

        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("// Generated by ComposeNet.SourceGenerators.ComposeFacadeGenerator.");
        sb.AppendLine("#nullable enable");
        sb.AppendLine("namespace ComposeNet");
        sb.AppendLine("{");

        sb.Append("    /// <summary>");
        if (!string.IsNullOrEmpty(summary))
            sb.Append(EscapeXml(summary!));
        else
            sb.Append("Generated facade for <c>ComposeBridges.").Append(className).Append("</c>.");
        sb.AppendLine("</summary>");

        sb.Append("    public sealed partial class ").Append(className).Append(" : ").AppendLine(baseClass);
        sb.AppendLine("    {");

        // Backing fields for ctor-supplied slots.
        foreach (var s in ctorSlots)
        {
            var typeRef = CtorFieldType(s);
            sb.Append("        readonly ").Append(typeRef).Append(" _").Append(s.Param.Name).AppendLine(";");
        }

        if (ctorSlots.Length > 0)
        {
            sb.Append("        public ").Append(className).Append('(');
            for (int i = 0; i < ctorSlots.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(CtorFieldType(ctorSlots[i])).Append(' ').Append(EscapeIdent(ctorSlots[i].Param.Name));
            }
            sb.AppendLine(")");
            sb.AppendLine("        {");
            foreach (var s in ctorSlots)
                sb.Append("            _").Append(s.Param.Name).Append(" = ").Append(EscapeIdent(s.Param.Name)).AppendLine(";");
            sb.AppendLine("        }");
        }

        // Render
        var composerName = EscapeIdent(composerParam.Name);
        sb.Append("        internal override void Render(global::AndroidX.Compose.Runtime.IComposer ")
          .Append(composerName).AppendLine(")");
        sb.AppendLine("        {");

        // OnClick wrappers — one line each so per-bridge generated files
        // give each Wrap*/ComposableLambda* call a unique line (slot-table
        // keys are derived from CallerLineNumber + CallerFilePath).
        foreach (var s in slots.Where(s => s.Kind == FacadeSlotKind.OnClick))
        {
            sb.Append("            var __").Append(s.Param.Name)
              .Append(" = new global::ComposeNet.ComposableLambda0(_").Append(s.Param.Name).AppendLine(");");
        }

        // Content wrapper.
        if (isContainer)
        {
            var contentSlot = slots.First(s => s.Kind is FacadeSlotKind.Content2 or FacadeSlotKind.Content3);
            sb.Append("            var __").Append(contentSlot.Param.Name).Append(" = global::ComposeNet.ComposableLambdas.");
            if (contentArity == 2)
            {
                sb.Append("Wrap2(").Append(composerName).AppendLine(", c => RenderChildren(c));");
            }
            else if (!string.IsNullOrEmpty(scope))
            {
                sb.Append("Wrap3(").Append(composerName).AppendLine(", (__scope, c) =>");
                sb.AppendLine("            {");
                sb.Append("                using var __scopeFrame = global::ComposeNet.RenderContext.PushScope(__scope, global::ComposeNet.ScopeKind.")
                  .Append(scope).AppendLine(");");
                sb.AppendLine("                RenderChildren(c);");
                sb.AppendLine("            });");
            }
            else
            {
                sb.Append("Wrap3(").Append(composerName).AppendLine(", c => RenderChildren(c));");
            }
        }

        // Bridge call — preserve bridge param order. Always use the
        // bridge method's real name, not the user-chosen facade name
        // (ClassName attribute may override the latter).
        sb.Append("            global::ComposeNet.ComposeBridges.").Append(bridgeMethodName).Append('(');
        bool first = true;
        foreach (var s in slots)
        {
            if (!first) sb.Append(", ");
            first = false;
            switch (s.Kind)
            {
                case FacadeSlotKind.Modifier:
                    sb.Append("BuildModifier()");
                    break;
                case FacadeSlotKind.OnClick:
                case FacadeSlotKind.Content2:
                case FacadeSlotKind.Content3:
                    sb.Append("__").Append(s.Param.Name);
                    break;
                case FacadeSlotKind.Primitive:
                    sb.Append('_').Append(s.Param.Name);
                    break;
            }
        }
        if (!first) sb.Append(", ");
        sb.Append(composerName).AppendLine(");");

        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    static string CtorFieldType(FacadeSlot slot) =>
        slot.Kind switch
        {
            FacadeSlotKind.OnClick   => "global::System.Action",
            FacadeSlotKind.Primitive => slot.Param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat
                .WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.UseSpecialTypes
                    | SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers)),
            _ => slot.Param.Type.ToDisplayString(),
        };

    static bool IsModifier(ITypeSymbol type) =>
        type is INamedTypeSymbol n &&
        n.Name == "IModifier" &&
        n.ContainingNamespace?.ToDisplayString() == "AndroidX.Compose.UI";

    /// <summary>
    /// Returns the arity of a Kotlin <c>IFunction*</c> parameter type
    /// (<c>0</c> for IFunction0, <c>2</c> for IFunction2, etc.) or
    /// <c>-1</c> if the type is not a Kotlin function interface.
    /// </summary>
    static int KotlinFunctionArity(ITypeSymbol type)
    {
        if (type is not INamedTypeSymbol n) return -1;
        if (n.ContainingNamespace?.ToDisplayString() != "Kotlin.Jvm.Functions") return -1;
        var name = n.Name;
        if (!name.StartsWith("IFunction", StringComparison.Ordinal)) return -1;
        var tail = name.Substring("IFunction".Length);
        return int.TryParse(tail, out var arity) ? arity : -1;
    }

    static bool IsPrimitiveCtorType(ITypeSymbol type) =>
        type.SpecialType is SpecialType.System_String
            or SpecialType.System_Int32
            or SpecialType.System_Int64
            or SpecialType.System_Boolean
            or SpecialType.System_Single
            or SpecialType.System_Double;

    static GenerationResult Fail(DiagnosticDescriptor desc, Location loc, params object?[] args) =>
        new(null, null, new[] { Diagnostic.Create(desc, loc, args) });

    static string? ReadString(AttributeData attr, string name)
    {
        foreach (var na in attr.NamedArguments)
            if (na.Key == name && na.Value.Value is string s) return s;
        return null;
    }

    static string EscapeIdent(string name) =>
        SyntaxFacts.GetKeywordKind(name) == SyntaxKind.None ? name : "@" + name;

    static string EscapeXml(string s) =>
        s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    enum FacadeSlotKind
    {
        Modifier,
        OnClick,
        Content2,
        Content3,
        Primitive,
    }

    readonly struct FacadeSlot
    {
        public FacadeSlot(IParameterSymbol param, FacadeSlotKind kind)
        {
            Param = param;
            Kind = kind;
        }
        public IParameterSymbol Param { get; }
        public FacadeSlotKind Kind { get; }
    }
}
