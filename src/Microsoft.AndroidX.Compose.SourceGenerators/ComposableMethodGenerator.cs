using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace AndroidX.Compose.SourceGenerators;

/// <summary>
/// Tier 2 source generator. Picks up methods marked with
/// <c>[AndroidX.Compose.Composable]</c> and emits a partial implementation
/// that wraps the user-written body (the sibling <c>&lt;Name&gt;Impl</c>
/// static method) in a Compose restart group with per-parameter
/// <c>$changed</c> diffing and a skip-when-unchanged path.
/// </summary>
/// <remarks>
/// <para>
/// User pattern:
/// </para>
/// <code>
/// public static partial class Screens
/// {
///     [Composable]
///     public static partial void Greeting(IComposer composer, string name);
///
///     static void GreetingImpl(IComposer composer, string name)
///     {
///         Text(composer, $"Hello {name}");
///     }
/// }
/// </code>
/// <para>
/// The generator emits a second <c>partial</c> declaration that fills
/// in <c>Greeting</c>'s body with the canonical restart-group +
/// skip prelude. See <see cref="AndroidX.Compose.ComposableAttribute"/>
/// for the user-facing docs.
/// </para>
/// </remarks>
[Generator(LanguageNames.CSharp)]
public sealed class ComposableMethodGenerator : IIncrementalGenerator
{
    const string ComposableAttributeMetadataName = "AndroidX.Compose.ComposableAttribute";
    const string ComposerFullName = "AndroidX.Compose.Runtime.IComposer";
    const string ImplSuffix = "Impl";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var methods = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                ComposableAttributeMetadataName,
                predicate: static (node, _) => node is MethodDeclarationSyntax,
                transform: static (ctx, _) => (IMethodSymbol)ctx.TargetSymbol)
            .Where(static m => m is not null)
            .Collect();

        context.RegisterSourceOutput(methods, static (spc, methods) =>
        {
            foreach (var method in methods)
            {
                var result = Build(method);
                foreach (var diag in result.Diagnostics)
                    spc.ReportDiagnostic(diag);
                if (result.Source is { } src && result.HintName is { } hint)
                    spc.AddSource(hint, SourceText.From(src, Encoding.UTF8));
            }
        });
    }

    static GenerationResult Build(IMethodSymbol method)
    {
        var loc = method.Locations.FirstOrDefault() ?? Location.None;

        if (!method.IsStatic || !IsPartial(method))
        {
            return new GenerationResult(null, null,
                new[] { Diagnostic.Create(Diagnostics.ComposableNotPartial, loc, method.ToDisplayString()) });
        }

        if (method.Parameters.Length == 0 || !IsComposer(method.Parameters[0].Type))
        {
            return new GenerationResult(null, null,
                new[] { Diagnostic.Create(Diagnostics.ComposableMissingComposer, loc, method.ToDisplayString()) });
        }

        var container = method.ContainingType;
        if (container is null || !IsContainerPartial(container))
        {
            return new GenerationResult(null, null,
                new[] { Diagnostic.Create(Diagnostics.ComposableContainerNotPartial, loc, method.ToDisplayString(), container?.ToDisplayString() ?? "<unknown>") });
        }

        var implName = method.Name + ImplSuffix;
        var implCandidates = container.GetMembers(implName)
            .OfType<IMethodSymbol>()
            .Where(m => m.IsStatic)
            .ToList();

        if (implCandidates.Count == 0)
        {
            return new GenerationResult(null, null,
                new[] { Diagnostic.Create(Diagnostics.ComposableMissingImpl, loc, method.ToDisplayString(), implName) });
        }

        var impl = implCandidates.FirstOrDefault(c => SignatureMatches(method, c));
        if (impl is null)
        {
            return new GenerationResult(null, null,
                new[] { Diagnostic.Create(Diagnostics.ComposableImplSignatureMismatch, loc, method.ToDisplayString(), implName) });
        }

        var src = Emit(method);
        // Disambiguate overloads by hashing the parameter type list into
        // the hint suffix — two [Composable] overloads with the same
        // name (e.g. `Counter(IComposer, int)` and `Counter(IComposer,
        // int, string)`) would otherwise collide on the same `.g.cs`
        // file name.
        string overloadSuffix = string.Empty;
        if (method.Parameters.Length > 0)
        {
            var sig = new StringBuilder();
            foreach (var p in method.Parameters)
            {
                sig.Append(p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                sig.Append('|');
            }
            overloadSuffix = "_" + FnvHash(sig.ToString()).ToString("X8", System.Globalization.CultureInfo.InvariantCulture);
        }
        var hint = $"AndroidX.Compose.Composable.{container.ToDisplayString().Replace('.', '_').Replace('<', '_').Replace('>', '_')}.{method.Name}{overloadSuffix}.g.cs";
        return new GenerationResult(src, hint, []);
    }

    static bool IsPartial(IMethodSymbol method)
    {
        foreach (var syntaxRef in method.DeclaringSyntaxReferences)
        {
            if (syntaxRef.GetSyntax() is MethodDeclarationSyntax decl)
            {
                foreach (var mod in decl.Modifiers)
                {
                    if (mod.ValueText == "partial")
                        return true;
                }
            }
        }
        return false;
    }

    static bool IsContainerPartial(INamedTypeSymbol container)
    {
        foreach (var syntaxRef in container.DeclaringSyntaxReferences)
        {
            if (syntaxRef.GetSyntax() is TypeDeclarationSyntax decl)
            {
                foreach (var mod in decl.Modifiers)
                {
                    if (mod.ValueText == "partial")
                        return true;
                }
            }
        }
        return false;
    }

    static bool IsComposer(ITypeSymbol type) =>
        type.ToDisplayString() == ComposerFullName;

    static bool SignatureMatches(IMethodSymbol decl, IMethodSymbol impl)
    {
        if (decl.Parameters.Length != impl.Parameters.Length)
            return false;
        for (int i = 0; i < decl.Parameters.Length; i++)
        {
            if (!SymbolEqualityComparer.Default.Equals(
                    decl.Parameters[i].Type, impl.Parameters[i].Type))
                return false;
        }
        return true;
    }

    static string Emit(IMethodSymbol method)
    {
        var container = method.ContainingType!;
        var ns = container.ContainingNamespace?.IsGlobalNamespace == false
            ? container.ContainingNamespace.ToDisplayString()
            : null;
        var containerKeyword = container.IsRecord
            ? "record"
            : container.TypeKind switch
            {
                TypeKind.Struct => "struct",
                _ => "class",
            };

        // Build the key for this method site. Stable across processes —
        // FNV-1a over the fully-qualified method name. Matches the
        // SourceLocationKey contract used elsewhere in the repo.
        var methodFqn = container.ToDisplayString() + "." + method.Name;
        int key = FnvHash(methodFqn);

        var composer = method.Parameters[0].Name;

        // Optional trailing `int _changed = 0` parameter — the Kotlin
        // `$changed` analogue. When present, the wrapper threads
        // caller-provided change bits into __dirty, skips DiffSlot
        // fallback for slots the caller already populated, and uses
        // the Kotlin-correct skip mask/expected pair. When absent
        // (back-compat shape), the wrapper falls back to "skip iff
        // every param read Same AND composer is in a skip-eligible
        // state" with no force-bit awareness.
        bool hasChangedParam =
            method.Parameters.Length >= 2 &&
            method.Parameters[method.Parameters.Length - 1].Name == "_changed" &&
            method.Parameters[method.Parameters.Length - 1].Type.SpecialType == SpecialType.System_Int32;

        var userParams = hasChangedParam
            ? method.Parameters.Skip(1).Take(method.Parameters.Length - 2).ToList()
            : method.Parameters.Skip(1).ToList();
        var changedParamName = hasChangedParam
            ? method.Parameters[method.Parameters.Length - 1].Name
            : null;

        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using global::AndroidX.Compose;");
        sb.AppendLine("using global::AndroidX.Compose.Runtime;");
        sb.AppendLine();
        if (ns is not null)
        {
            sb.Append("namespace ").Append(ns).AppendLine(";");
            sb.AppendLine();
        }

        // Walk the containing type chain so we can re-open every
        // partial wrapper. For nested types we currently support one
        // level (the common case); deeply nested would need recursion.
        var typeChain = new System.Collections.Generic.List<INamedTypeSymbol>();
        for (var t = container; t is not null && t.TypeKind != TypeKind.Error && !t.IsImplicitlyDeclared; t = t.ContainingType)
        {
            typeChain.Insert(0, t);
            if (t.ContainingType is null) break;
        }

        int indentLevel = 0;
        foreach (var t in typeChain)
        {
            AppendIndent(sb, indentLevel);
            sb.Append(t.IsStatic ? "static " : string.Empty);
            sb.Append("partial ").Append(containerKeyword).Append(' ').Append(t.Name);
            if (t.TypeParameters.Length > 0)
            {
                sb.Append('<');
                for (int i = 0; i < t.TypeParameters.Length; i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.Append(t.TypeParameters[i].Name);
                }
                sb.Append('>');
            }
            sb.AppendLine();
            AppendIndent(sb, indentLevel);
            sb.AppendLine("{");
            indentLevel++;
        }

        // Method signature, repeated verbatim from the user's
        // partial declaration so the compiler sees a matching
        // implementing part.
        AppendIndent(sb, indentLevel);
        sb.Append(AccessibilityKeyword(method.DeclaredAccessibility))
          .Append("static partial ")
          .Append(method.ReturnType.ToDisplayString())
          .Append(' ')
          .Append(method.Name)
          .Append('(');
        for (int i = 0; i < method.Parameters.Length; i++)
        {
            if (i > 0) sb.Append(", ");
            var p = method.Parameters[i];
            sb.Append(p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
              .Append(' ').Append(p.Name);
            // Note: default values live on the partial declaration only —
            // emitting them on the implementing part trips CS1066.
        }
        sb.AppendLine(")");
        AppendIndent(sb, indentLevel);
        sb.AppendLine("{");

        var bodyIndent = indentLevel + 1;

        // var __c = composer.StartRestartGroup(KEY);
        AppendIndent(sb, bodyIndent);
        sb.Append("var __c = ").Append(composer).Append(".StartRestartGroup(unchecked((int)0x")
          .Append(key.ToString("X8", System.Globalization.CultureInfo.InvariantCulture))
          .AppendLine("));");

        // int __dirty = _changed;   (or 0 when no _changed param)
        AppendIndent(sb, bodyIndent);
        if (hasChangedParam)
            sb.Append("int __dirty = ").Append(changedParamName).AppendLine(";");
        else
            sb.AppendLine("int __dirty = 0;");

        // Per-param: when caller didn't tell us about this slot
        // (`($changed and 0b110<<shift) == 0`), fall back to DiffSlot.
        // The `0b110 << shift` mask captures the Same+Different bits
        // of the slot — if either is set the caller is informing us.
        for (int i = 0; i < userParams.Count; i++)
        {
            var p = userParams[i];
            int bitOffset = 1 + i * 3;
            AppendIndent(sb, bodyIndent);
            if (hasChangedParam)
            {
                // `if ((_changed & 0bMASK) == 0) __dirty |= __c.DiffSlot(p, bitOffset);`
                int sameDiffMask = 0b110 << i * 3 + 1 >> 1; // == 0b011 << (1+3*i) hm let me just compute
                int slotMask = (0b001 | 0b010) << bitOffset; // bits Same+Different of this slot
                sb.Append("if ((").Append(changedParamName)
                  .Append(" & 0x")
                  .Append(slotMask.ToString("X", System.Globalization.CultureInfo.InvariantCulture))
                  .Append(") == 0) __dirty |= __c.DiffSlot<")
                  .Append(p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                  .Append(">(").Append(p.Name).Append(", ")
                  .Append(bitOffset.ToString(System.Globalization.CultureInfo.InvariantCulture))
                  .AppendLine(");");
            }
            else
            {
                sb.Append("__dirty |= __c.DiffSlot<")
                  .Append(p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                  .Append(">(").Append(p.Name).Append(", ")
                  .Append(bitOffset.ToString(System.Globalization.CultureInfo.InvariantCulture))
                  .AppendLine(");");
            }
        }

        // Skip check.
        //
        // With `_changed`: Kotlin-correct mask/expected.
        //   mask     = 0b001 (force) | for each param: 0b101 << (1+3*i)
        //   expected = for each param: 0b001 << (1+3*i)
        //   `if ((__dirty & mask) != expected || !__c.Skipping)`
        //
        // Without `_changed` (back-compat): "skip iff every param Same".
        //   `if ((__dirty & differentMask) != 0 || !__c.Skipping)`
        AppendIndent(sb, bodyIndent);
        if (hasChangedParam)
        {
            long mask = 0b001;
            long expected = 0;
            for (int i = 0; i < userParams.Count; i++)
            {
                int shift = 1 + i * 3;
                mask |= 0b101L << shift;
                expected |= 0b001L << shift;
            }
            if (userParams.Count == 0)
            {
                // mask = 1, expected = 0 → `(__dirty & 1) != 0` ≡ force bit set.
                sb.AppendLine("if ((__dirty & 0x1) != 0 || !__c.Skipping)");
            }
            else
            {
                sb.Append("if ((__dirty & 0x")
                  .Append(mask.ToString("X", System.Globalization.CultureInfo.InvariantCulture))
                  .Append(") != 0x")
                  .Append(expected.ToString("X", System.Globalization.CultureInfo.InvariantCulture))
                  .AppendLine(" || !__c.Skipping)");
            }
        }
        else
        {
            long differentMask = 0;
            for (int i = 0; i < userParams.Count; i++)
            {
                differentMask |= (long)2 << (1 + i * 3);
            }
            if (userParams.Count == 0)
                sb.AppendLine("if (!__c.Skipping)");
            else
                sb.Append("if ((__dirty & 0x")
                  .Append(differentMask.ToString("X", System.Globalization.CultureInfo.InvariantCulture))
                  .AppendLine(") != 0 || !__c.Skipping)");
        }

        AppendIndent(sb, bodyIndent);
        sb.AppendLine("{");
        AppendIndent(sb, bodyIndent + 1);
        sb.Append(method.Name).Append(ImplSuffix).Append("(__c");
        foreach (var p in userParams)
        {
            sb.Append(", ").Append(p.Name);
        }
        if (hasChangedParam)
        {
            // Pass __dirty through to the body so it can in turn
            // forward to nested [Composable] calls. Until the recursive
            // call rewriter lands the body just ignores it.
            sb.Append(", __dirty");
        }
        sb.AppendLine(");");
        AppendIndent(sb, bodyIndent);
        sb.AppendLine("}");
        AppendIndent(sb, bodyIndent);
        sb.AppendLine("else");
        AppendIndent(sb, bodyIndent);
        sb.AppendLine("{");
        AppendIndent(sb, bodyIndent + 1);
        sb.AppendLine("__c.SkipToGroupEnd();");
        AppendIndent(sb, bodyIndent);
        sb.AppendLine("}");

        // EndRestartGroup + UpdateScope. With `_changed`, the second
        // lambda param carries the runtime's "force" hint; we OR-in
        // bit 0 so the recompose pass is never elided.
        AppendIndent(sb, bodyIndent);
        if (hasChangedParam)
        {
            sb.Append("__c.EndRestartGroup()?.UpdateScope(new global::AndroidX.Compose.ComposableLambda2((__c2, __force) => ")
              .Append(method.Name).Append("(__c2");
            foreach (var p in userParams)
            {
                sb.Append(", ").Append(p.Name);
            }
            sb.AppendLine(", __force | 1)));");
        }
        else
        {
            sb.Append("__c.EndRestartGroup()?.UpdateScope(new global::AndroidX.Compose.ComposableLambda2(__c2 => ")
              .Append(method.Name).Append("(__c2");
            foreach (var p in userParams)
            {
                sb.Append(", ").Append(p.Name);
            }
            sb.AppendLine(")));");
        }

        AppendIndent(sb, indentLevel);
        sb.AppendLine("}");

        for (int i = indentLevel - 1; i >= 0; i--)
        {
            AppendIndent(sb, i);
            sb.AppendLine("}");
        }

        return sb.ToString();
    }

    static void AppendIndent(StringBuilder sb, int level)
    {
        for (int i = 0; i < level; i++) sb.Append("    ");
    }

    static string AccessibilityKeyword(Accessibility a) => a switch
    {
        Accessibility.Public => "public ",
        Accessibility.Internal => "internal ",
        Accessibility.Private => "private ",
        Accessibility.Protected => "protected ",
        Accessibility.ProtectedAndInternal => "private protected ",
        Accessibility.ProtectedOrInternal => "protected internal ",
        _ => string.Empty,
    };

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
}
